using MediatR;
using WMSPro.Application.Abstractions;
using WMSPro.Application.Abstractions.Repositories;
using WMSPro.Application.Picking.Commands;
using WMSPro.Application.Picking.Services;
using WMSPro.Domain.Common;
using WMSPro.Domain.Entities;
using WMSPro.Domain.Enums;

namespace WMSPro.Application.Picking.Handlers;

public class PickingHandlers :
    IRequestHandler<CreatePickingWaveCommand, Result<PickingWaveDto>>,
    IRequestHandler<ConfirmPickItemCommand, Result<PickingWaveDto>>,
    IRequestHandler<CancelPickingWaveCommand, Result>,
    IRequestHandler<GetPickingWavesQuery, Result<IReadOnlyList<PickingWaveDto>>>,
    IRequestHandler<GetPickingWaveByIdQuery, Result<PickingWaveDto>>
{
    private readonly IPickingWaveRepository _waveRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly IStorageLocationRepository _locationRepo;
    private readonly IProductRepository _productRepo;
    private readonly IWarehouseRepository _warehouseRepo;
    private readonly IPickPathOptimizer _optimizer;
    private readonly IUnitOfWork _unitOfWork;

    public PickingHandlers(
        IPickingWaveRepository waveRepo,
        IOrderRepository orderRepo,
        IStorageLocationRepository locationRepo,
        IProductRepository productRepo,
        IWarehouseRepository warehouseRepo,
        IPickPathOptimizer optimizer,
        IUnitOfWork unitOfWork)
    {
        _waveRepo = waveRepo;
        _orderRepo = orderRepo;
        _locationRepo = locationRepo;
        _productRepo = productRepo;
        _warehouseRepo = warehouseRepo;
        _optimizer = optimizer;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PickingWaveDto>> Handle(CreatePickingWaveCommand request, CancellationToken cancellationToken)
    {
        if (request.OrderIds == null || !request.OrderIds.Any())
            return Result.Failure<PickingWaveDto>(Error.Validation("OrderIds", "At least one sales order is required to generate a wave."));

        var warehouse = await _warehouseRepo.GetByIdAsync(request.WarehouseId, cancellationToken);
        if (warehouse is null)
            return Result.Failure<PickingWaveDto>(Error.NotFound("Warehouse.NotFound", "Warehouse not found."));

        // Fetch orders
        var orders = new List<Order>();
        foreach (var orderId in request.OrderIds)
        {
            var order = await _orderRepo.GetByIdAsync(orderId, cancellationToken);
            if (order is null)
                return Result.Failure<PickingWaveDto>(Error.NotFound("Order.NotFound", $"Order {orderId} not found."));

            if (order.Type != OrderType.Sales)
                return Result.Failure<PickingWaveDto>(Error.Conflict($"Order {order.OrderNumber} is not a Sales Order. Picking waves are for outbound fulfillment."));

            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
                return Result.Failure<PickingWaveDto>(Error.Conflict($"Order {order.OrderNumber} is already {order.Status}."));

            orders.Add(order);
        }

        // Fetch products & storage locations
        var productIds = orders.SelectMany(o => o.Items).Select(i => i.ProductId).Distinct().ToList();
        var products = new Dictionary<Guid, Product>();
        foreach (var pid in productIds)
        {
            var prod = await _productRepo.GetByIdAsync(pid, cancellationToken);
            if (prod is not null)
                products[pid] = prod;
        }

        var warehouseLocations = await _locationRepo.GetByWarehouseAsync(request.WarehouseId, null, cancellationToken);
        var locationLookup = warehouseLocations
            .Where(l => l.ProductId.HasValue)
            .ToLookup(l => l.ProductId!.Value);

        // Build raw candidate pick stops
        var candidates = new List<PickLocationCandidate>();
        foreach (var order in orders)
        {
            foreach (var item in order.Items)
            {
                var prod = products.GetValueOrDefault(item.ProductId);
                var sku = prod?.SKU ?? "SKU-UNKNOWN";

                // Pick primary bin with stock if assigned, else first location
                var loc = locationLookup[item.ProductId].FirstOrDefault();

                candidates.Add(new PickLocationCandidate(
                    OrderId: order.Id,
                    OrderNumber: order.OrderNumber,
                    ProductId: item.ProductId,
                    ProductName: item.ProductName,
                    ProductSku: sku,
                    StorageLocationId: loc?.Id,
                    LocationCode: loc?.LocationCode ?? "FLOOR-UNASSIGNED",
                    Zone: loc?.Zone ?? "Floor",
                    Aisle: loc?.Aisle ?? "FL0",
                    Rack: loc?.Rack ?? "R0",
                    ShelfLevel: loc?.ShelfLevel ?? 0,
                    BinNumber: loc?.BinNumber ?? "B0",
                    QuantityRequested: item.Quantity));
            }
        }

        // Run S-Shape Optimization
        var optimizedStops = _optimizer.OptimizeRoute(candidates);

        // Generate wave
        var waveNumber = $"WAVE-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";
        var waveResult = PickingWave.Create(waveNumber, request.WarehouseId, request.AssignedPicker);
        if (!waveResult.IsSuccess)
            return Result.Failure<PickingWaveDto>(waveResult.Error);

        var wave = waveResult.Value;

        foreach (var stop in optimizedStops)
        {
            wave.AddPickItem(
                stop.OrderId,
                stop.OrderNumber,
                stop.ProductId,
                stop.ProductName,
                stop.ProductSku,
                stop.StorageLocationId,
                stop.LocationCode,
                stop.Zone,
                stop.Aisle,
                stop.Rack,
                stop.ShelfLevel,
                stop.BinNumber,
                stop.SequenceOrder,
                stop.QuantityRequested);
        }

        await _waveRepo.AddAsync(wave, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToDto(wave, warehouse.Name));
    }

    public async Task<Result<PickingWaveDto>> Handle(ConfirmPickItemCommand request, CancellationToken cancellationToken)
    {
        var wave = await _waveRepo.GetByIdWithItemsAsync(request.WaveId, cancellationToken);
        if (wave is null)
            return Result.Failure<PickingWaveDto>(Error.NotFound("PickingWave.NotFound", "Picking wave not found."));

        var pickResult = wave.ConfirmItemPick(request.PickItemId, request.QuantityPicked);
        if (!pickResult.IsSuccess)
            return Result.Failure<PickingWaveDto>(pickResult.Error);

        _waveRepo.Update(wave);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var warehouse = await _warehouseRepo.GetByIdAsync(wave.WarehouseId, cancellationToken);
        return Result.Success(MapToDto(wave, warehouse?.Name));
    }

    public async Task<Result> Handle(CancelPickingWaveCommand request, CancellationToken cancellationToken)
    {
        var wave = await _waveRepo.GetByIdAsync(request.WaveId, cancellationToken);
        if (wave is null)
            return Result.Failure(Error.NotFound("PickingWave.NotFound", "Picking wave not found."));

        var result = wave.Cancel();
        if (!result.IsSuccess)
            return result;

        _waveRepo.Update(wave);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<PickingWaveDto>>> Handle(GetPickingWavesQuery request, CancellationToken cancellationToken)
    {
        var waves = await _waveRepo.GetAllAsync(request.WarehouseId, request.Status, cancellationToken);
        var dtos = new List<PickingWaveDto>();

        foreach (var wave in waves)
        {
            var warehouse = await _warehouseRepo.GetByIdAsync(wave.WarehouseId, cancellationToken);
            dtos.Add(MapToDto(wave, warehouse?.Name));
        }

        return Result.Success<IReadOnlyList<PickingWaveDto>>(dtos);
    }

    public async Task<Result<PickingWaveDto>> Handle(GetPickingWaveByIdQuery request, CancellationToken cancellationToken)
    {
        var wave = await _waveRepo.GetByIdWithItemsAsync(request.Id, cancellationToken);
        if (wave is null)
            return Result.Failure<PickingWaveDto>(Error.NotFound("PickingWave.NotFound", "Picking wave not found."));

        var warehouse = await _warehouseRepo.GetByIdAsync(wave.WarehouseId, cancellationToken);
        return Result.Success(MapToDto(wave, warehouse?.Name));
    }

    private static PickingWaveDto MapToDto(PickingWave wave, string? warehouseName)
    {
        return new PickingWaveDto(
            wave.Id,
            wave.WaveNumber,
            wave.WarehouseId,
            warehouseName ?? "Warehouse Hub",
            wave.Status.ToString(),
            wave.AssignedPicker,
            wave.TotalItems,
            wave.PickedItems,
            wave.ProgressPercentage,
            wave.IsFullyPicked,
            wave.CreatedAt,
            wave.CompletedAt,
            wave.Items.OrderBy(i => i.SequenceOrder).Select(i => new PickItemDto(
                i.Id,
                i.OrderId,
                i.OrderNumber,
                i.ProductId,
                i.ProductName,
                i.ProductSku,
                i.StorageLocationId,
                i.LocationCode,
                i.Zone,
                i.Aisle,
                i.Rack,
                i.ShelfLevel,
                i.BinNumber,
                i.SequenceOrder,
                i.QuantityRequested,
                i.QuantityPicked,
                i.IsPicked,
                i.PickedAt)).ToList());
    }
}
