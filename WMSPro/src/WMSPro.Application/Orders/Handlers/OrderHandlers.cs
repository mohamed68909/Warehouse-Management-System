using MediatR;
using WMSPro.Application.Abstractions;
using WMSPro.Application.Abstractions.Repositories;
using WMSPro.Application.Orders.Commands;
using WMSPro.Application.Orders.Events;
using WMSPro.Domain.Common;
using WMSPro.Domain.Entities;
using WMSPro.Domain.Enums;
using WMSPro.Domain.Events;

namespace WMSPro.Application.Orders.Handlers;

// ─── Helper mapper ────────────────────────────────────────────────────────────
internal static class OrderMapper
{
    public static OrderDto ToDto(Order o) => new(
        o.Id, o.OrderNumber, o.Type.ToString(), o.Status.ToString(),
        o.SupplierCustomerName, o.WarehouseId, o.Warehouse?.Name,
        o.TotalAmount, o.Date,
        o.Items.Select(i => new OrderItemDto(i.Id, i.ProductId, i.ProductName,
            i.Quantity, i.UnitPrice, i.TotalPrice)).ToList().AsReadOnly());
}

// ─── Get All Orders ────────────────────────────────────────────────────────────
public class GetOrdersHandler : IRequestHandler<GetOrdersQuery, Result<IReadOnlyList<OrderDto>>>
{
    private readonly IOrderRepository _repo;
    public GetOrdersHandler(IOrderRepository repo) => _repo = repo;

    public async Task<Result<IReadOnlyList<OrderDto>>> Handle(
        GetOrdersQuery request, CancellationToken cancellationToken)
    {
        OrderType? typeFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Type) && Enum.TryParse<OrderType>(request.Type, true, out var parsed))
            typeFilter = parsed;

        var orders = await _repo.GetAllAsync(typeFilter, cancellationToken);
        return Result.Success<IReadOnlyList<OrderDto>>(
            orders.Select(OrderMapper.ToDto).ToList().AsReadOnly());
    }
}

// ─── Get By Id ────────────────────────────────────────────────────────────────
public class GetOrderByIdHandler : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    private readonly IOrderRepository _repo;
    public GetOrderByIdHandler(IOrderRepository repo) => _repo = repo;

    public async Task<Result<OrderDto>> Handle(
        GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _repo.GetByIdAsync(request.Id, cancellationToken);
        if (order is null)
            return Result.Failure<OrderDto>(Error.NotFound(nameof(Order), request.Id));

        return Result.Success(OrderMapper.ToDto(order));
    }
}

// ─── Create Order ──────────────────────────────────────────────────────────────
public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Result<OrderDto>>
{
    private readonly IOrderRepository _repo;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderHandler(IOrderRepository repo, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<OrderDto>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OrderType>(request.Type, true, out var orderType))
            return Result.Failure<OrderDto>(Error.Validation("Type", "Invalid order type. Use: Purchase or Sales."));

        if (await _repo.ExistsByOrderNumberAsync(request.OrderNumber, cancellationToken))
            return Result.Failure<OrderDto>(Error.Conflict($"Order number '{request.OrderNumber}' already exists."));

        var orderResult = Order.Create(request.OrderNumber, orderType,
            request.SupplierCustomerName, request.WarehouseId);

        if (orderResult.IsFailure)
            return Result.Failure<OrderDto>(orderResult.Error);

        var order = orderResult.Value;

        foreach (var item in request.Items)
        {
            var addResult = order.AddOrderItem(
                item.ProductId, item.ProductName, item.Quantity, item.UnitPrice);

            if (addResult.IsFailure)
                return Result.Failure<OrderDto>(addResult.Error);
        }

        await _repo.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(OrderMapper.ToDto(order));
    }
}

// ─── Update Order Status ───────────────────────────────────────────────────────
public class UpdateOrderStatusHandler : IRequestHandler<UpdateOrderStatusCommand, Result>
{
    private readonly IOrderRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;   // MediatR publisher for Domain Events

    public UpdateOrderStatusHandler(IOrderRepository repo, IUnitOfWork unitOfWork, IPublisher publisher)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<Result> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var newStatus))
            return Result.Failure(Error.Validation("Status", "Invalid status. Use: Pending, Processing, Completed, or Cancelled."));

        var order = await _repo.GetByIdAsync(request.Id, cancellationToken);
        if (order is null)
            return Result.Failure(Error.NotFound(nameof(Order), request.Id));

        var updateResult = order.UpdateStatus(newStatus);
        if (updateResult.IsFailure)
            return updateResult;

        _repo.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // ── Dispatch Domain Events after persistence ───────────────────────────
        foreach (var domainEvent in order.DomainEvents)
        {
            if (domainEvent is OrderCompletedEvent completedEvent)
                await _publisher.Publish(new OrderCompletedEventNotification(completedEvent), cancellationToken);
        }

        order.ClearDomainEvents();

        return Result.Success();
    }
}

// ─── OrderCompletedEvent Handler ─────────────────────────────────────────────
public class OrderCompletedEventHandler : INotificationHandler<OrderCompletedEventNotification>
{
    private readonly IInventoryTransactionRepository _txRepo;
    private readonly IProductRepository _productRepo;
    private readonly IWarehouseRepository _warehouseRepo;
    private readonly IWarehouseStockRepository _warehouseStockRepo;
    private readonly IUnitOfWork _unitOfWork;

    public OrderCompletedEventHandler(
        IInventoryTransactionRepository txRepo,
        IProductRepository productRepo,
        IWarehouseRepository warehouseRepo,
        IWarehouseStockRepository warehouseStockRepo,
        IUnitOfWork unitOfWork)
    {
        _txRepo = txRepo;
        _productRepo = productRepo;
        _warehouseRepo = warehouseRepo;
        _warehouseStockRepo = warehouseStockRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(OrderCompletedEventNotification notification, CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        // Load the warehouse once (shared across all items in the order)
        var warehouse = await _warehouseRepo.GetByIdAsync(evt.WarehouseId, cancellationToken);
        if (warehouse is null) return; // Warehouse was deleted — skip silently

        foreach (var item in evt.Items)
        {
            // Load the product to validate and update its global stock
            var product = await _productRepo.GetByIdAsync(item.ProductId, cancellationToken);
            if (product is null) continue; // Product was deleted — skip this item

            Result<InventoryTransaction> txResult;

            if (evt.OrderType == OrderType.Purchase)
            {
                // Purchase completed → Stock IN
                txResult = InventoryTransaction.CreateStockIn(
                    item.ProductId,
                    evt.WarehouseId,
                    item.Quantity,
                    reference: evt.OrderNumber);

                if (txResult.IsFailure) continue;

                // Update global product stock
                var addProductResult = product.AddStock(item.Quantity);
                if (addProductResult.IsFailure) continue;

                // Update warehouse current stock
                var addWarehouseResult = warehouse.AddStock(item.Quantity);
                if (addWarehouseResult.IsFailure) continue;

                // Update warehouse-specific stock
                await AdjustWarehouseStockAsync(evt.WarehouseId, item.ProductId, item.Quantity, cancellationToken);

                _productRepo.Update(product);
                _warehouseRepo.Update(warehouse);
            }
            else
            {
                // Sales completed → Stock OUT
                // Retrieve warehouse-specific available stock
                var stock = await _warehouseStockRepo.GetByWarehouseAndProductAsync(evt.WarehouseId, item.ProductId, cancellationToken);
                int availableStock = stock?.Quantity ?? 0;

                txResult = InventoryTransaction.CreateStockOut(
                    item.ProductId,
                    evt.WarehouseId,
                    item.Quantity,
                    availableStock: availableStock,
                    reference: evt.OrderNumber,
                    productName: item.ProductName);

                if (txResult.IsFailure) continue;

                // Update global product stock
                var removeProductResult = product.RemoveStock(item.Quantity);
                if (removeProductResult.IsFailure) continue;

                // Update warehouse current stock
                var removeWarehouseResult = warehouse.RemoveStock(item.Quantity);
                if (removeWarehouseResult.IsFailure) continue;

                // Update warehouse-specific stock
                await AdjustWarehouseStockAsync(evt.WarehouseId, item.ProductId, -item.Quantity, cancellationToken);

                _productRepo.Update(product);
                _warehouseRepo.Update(warehouse);
            }

            await _txRepo.AddAsync(txResult.Value, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task AdjustWarehouseStockAsync(
        Guid warehouseId,
        Guid productId,
        int quantityChange,
        CancellationToken cancellationToken)
    {
        var stock = await _warehouseStockRepo.GetByWarehouseAndProductAsync(warehouseId, productId, cancellationToken);
        if (stock is null)
        {
            if (quantityChange < 0)
            {
                throw new InvalidOperationException("Cannot reduce stock below zero because no inventory record exists.");
            }
            var newStock = WarehouseStock.Create(warehouseId, productId, quantityChange).Value;
            await _warehouseStockRepo.AddAsync(newStock, cancellationToken);
        }
        else
        {
            if (quantityChange > 0)
            {
                stock.AddStock(quantityChange);
            }
            else
            {
                stock.RemoveStock(-quantityChange);
            }
            _warehouseStockRepo.Update(stock);
        }
    }
}
