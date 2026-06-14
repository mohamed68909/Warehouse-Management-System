using MediatR;
using WMSPro.Application.Abstractions;
using WMSPro.Application.Abstractions.Repositories;
using WMSPro.Application.Transactions.Commands;
using WMSPro.Domain.Common;
using WMSPro.Domain.Entities;
using WMSPro.Domain.Enums;

namespace WMSPro.Application.Transactions.Handlers;

// ─── Helper mapper ───────────────────────────────────────────────────────────
internal static class TransactionMapper
{
    public static TransactionDto ToDto(InventoryTransaction t) => new(
        t.Id,
        t.Type.ToString(),
        t.ProductId,
        t.Product?.Name ?? string.Empty,
        t.Product?.SKU ?? string.Empty,
        t.SourceWarehouseId,
        t.SourceWarehouse?.Name,
        t.DestinationWarehouseId,
        t.DestinationWarehouse?.Name,
        t.Quantity,
        t.Reference,
        t.Date);
}

// ─── Get All Transactions ────────────────────────────────────────────────────
public class GetTransactionsHandler : IRequestHandler<GetTransactionsQuery, Result<IReadOnlyList<TransactionDto>>>
{
    private readonly IInventoryTransactionRepository _repo;
    public GetTransactionsHandler(IInventoryTransactionRepository repo) => _repo = repo;

    public async Task<Result<IReadOnlyList<TransactionDto>>> Handle(
        GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        TransactionType? typeFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Type) && Enum.TryParse<TransactionType>(request.Type, true, out var parsed))
            typeFilter = parsed;

        var transactions = await _repo.GetAllAsync(request.ProductSearch, typeFilter, cancellationToken);

        return Result.Success<IReadOnlyList<TransactionDto>>(
            transactions.Select(TransactionMapper.ToDto).ToList().AsReadOnly());
    }
}

// ─── Create Transaction ──────────────────────────────────────────────────────
public class CreateTransactionHandler : IRequestHandler<CreateTransactionCommand, Result<TransactionDto>>
{
    private readonly IInventoryTransactionRepository _txRepo;
    private readonly IProductRepository _productRepo;
    private readonly IWarehouseRepository _warehouseRepo;
    private readonly IWarehouseStockRepository _warehouseStockRepo;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTransactionHandler(
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

    public async Task<Result<TransactionDto>> Handle(
        CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        // Validate Product
        var product = await _productRepo.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
            return Result.Failure<TransactionDto>(Error.NotFound(nameof(Product), request.ProductId));

        Result<InventoryTransaction> txResult;

        if (request.Type.Equals("In", StringComparison.OrdinalIgnoreCase))
        {
            // ── Stock IN ──
            if (request.DestinationWarehouseId is null)
                return Result.Failure<TransactionDto>(Error.Validation("DestinationWarehouse", "Destination warehouse is required for Stock IN."));

            var destWarehouse = await _warehouseRepo.GetByIdAsync(request.DestinationWarehouseId.Value, cancellationToken);
            if (destWarehouse is null)
                return Result.Failure<TransactionDto>(Error.NotFound(nameof(Warehouse), request.DestinationWarehouseId));

            txResult = InventoryTransaction.CreateStockIn(
                product.Id, destWarehouse.Id, request.Quantity, request.Reference);

            if (txResult.IsFailure) return Result.Failure<TransactionDto>(txResult.Error);

            // Update stock
            var addResult = product.AddStock(request.Quantity);
            if (addResult.IsFailure) return Result.Failure<TransactionDto>(addResult.Error);

            var warehouseAddResult = destWarehouse.AddStock(request.Quantity);
            if (warehouseAddResult.IsFailure) return Result.Failure<TransactionDto>(warehouseAddResult.Error);

            // Update warehouse-specific stock
            await AdjustWarehouseStockAsync(destWarehouse.Id, product.Id, request.Quantity, cancellationToken);

            _productRepo.Update(product);
            _warehouseRepo.Update(destWarehouse);
        }
        else if (request.Type.Equals("Out", StringComparison.OrdinalIgnoreCase))
        {
            // ── Stock OUT ──
            if (request.SourceWarehouseId is null)
                return Result.Failure<TransactionDto>(Error.Validation("SourceWarehouse", "Source warehouse is required for Stock OUT."));

            var srcWarehouse = await _warehouseRepo.GetByIdAsync(request.SourceWarehouseId.Value, cancellationToken);
            if (srcWarehouse is null)
                return Result.Failure<TransactionDto>(Error.NotFound(nameof(Warehouse), request.SourceWarehouseId));

            // Retrieve warehouse-specific stock
            var stock = await _warehouseStockRepo.GetByWarehouseAndProductAsync(srcWarehouse.Id, product.Id, cancellationToken);
            int availableStock = stock?.Quantity ?? 0;

            txResult = InventoryTransaction.CreateStockOut(
                product.Id, srcWarehouse.Id, request.Quantity,
                availableStock, request.Reference, product.Name);

            if (txResult.IsFailure) return Result.Failure<TransactionDto>(txResult.Error);

            var removeProductResult = product.RemoveStock(request.Quantity);
            if (removeProductResult.IsFailure) return Result.Failure<TransactionDto>(removeProductResult.Error);

            var removeWarehouseResult = srcWarehouse.RemoveStock(request.Quantity);
            if (removeWarehouseResult.IsFailure) return Result.Failure<TransactionDto>(removeWarehouseResult.Error);

            // Update warehouse-specific stock
            await AdjustWarehouseStockAsync(srcWarehouse.Id, product.Id, -request.Quantity, cancellationToken);

            _productRepo.Update(product);
            _warehouseRepo.Update(srcWarehouse);
        }
        else if (request.Type.Equals("Transfer", StringComparison.OrdinalIgnoreCase))
        {
            // ── Transfer ──
            if (request.SourceWarehouseId is null || request.DestinationWarehouseId is null)
                return Result.Failure<TransactionDto>(Error.Validation("Warehouse", "Both source and destination warehouses are required for Transfer."));

            var srcWarehouse = await _warehouseRepo.GetByIdAsync(request.SourceWarehouseId.Value, cancellationToken);
            var destWarehouse = await _warehouseRepo.GetByIdAsync(request.DestinationWarehouseId.Value, cancellationToken);

            if (srcWarehouse is null) return Result.Failure<TransactionDto>(Error.NotFound(nameof(Warehouse), request.SourceWarehouseId));
            if (destWarehouse is null) return Result.Failure<TransactionDto>(Error.NotFound(nameof(Warehouse), request.DestinationWarehouseId));

            // Retrieve warehouse-specific stock
            var stock = await _warehouseStockRepo.GetByWarehouseAndProductAsync(srcWarehouse.Id, product.Id, cancellationToken);
            int availableStock = stock?.Quantity ?? 0;

            txResult = InventoryTransaction.CreateTransfer(
                product.Id, srcWarehouse.Id, destWarehouse.Id,
                request.Quantity, availableStock, request.Reference, product.Name);

            if (txResult.IsFailure) return Result.Failure<TransactionDto>(txResult.Error);

            var removeResult = srcWarehouse.RemoveStock(request.Quantity);
            if (removeResult.IsFailure) return Result.Failure<TransactionDto>(removeResult.Error);

            var addResult = destWarehouse.AddStock(request.Quantity);
            if (addResult.IsFailure) return Result.Failure<TransactionDto>(addResult.Error);

            // Update warehouse-specific stock
            await AdjustWarehouseStockAsync(srcWarehouse.Id, product.Id, -request.Quantity, cancellationToken);
            await AdjustWarehouseStockAsync(destWarehouse.Id, product.Id, request.Quantity, cancellationToken);

            _warehouseRepo.Update(srcWarehouse);
            _warehouseRepo.Update(destWarehouse);
        }
        else
        {
            return Result.Failure<TransactionDto>(Error.Validation("Type", "Invalid transaction type. Use: In, Out, or Transfer."));
        }

        // Save everything atomically
        await _txRepo.AddAsync(txResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(TransactionMapper.ToDto(txResult.Value));
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
