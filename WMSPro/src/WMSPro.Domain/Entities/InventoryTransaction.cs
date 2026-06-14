using WMSPro.Domain.Common;
using WMSPro.Domain.Enums;

namespace WMSPro.Domain.Entities;

public sealed class InventoryTransaction : Entity
{
    private InventoryTransaction() { }

    private InventoryTransaction(
        Guid id,
        TransactionType type,
        Guid productId,
        Guid? sourceWarehouseId,
        Guid? destinationWarehouseId,
        int quantity,
        string reference,
        DateTime date) : base(id)
    {
        Type = type;
        ProductId = productId;
        SourceWarehouseId = sourceWarehouseId;
        DestinationWarehouseId = destinationWarehouseId;
        Quantity = quantity;
        Reference = reference;
        Date = date;
    }

    public TransactionType Type { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? SourceWarehouseId { get; private set; }
    public Guid? DestinationWarehouseId { get; private set; }
    public int Quantity { get; private set; }
    public string Reference { get; private set; } = string.Empty;
    public DateTime Date { get; private set; }

    // Navigation properties (set by EF Core)
    public Product? Product { get; private set; }
    public Warehouse? SourceWarehouse { get; private set; }
    public Warehouse? DestinationWarehouse { get; private set; }

    // ─── Factory Methods ─────────────────────────────────────────────────────

    /// <summary>Creates a Stock IN transaction (e.g., from purchase order or manual entry).</summary>
    public static Result<InventoryTransaction> CreateStockIn(
        Guid productId,
        Guid destinationWarehouseId,
        int quantity,
        string reference,
        DateTime? date = null)
    {
        if (quantity <= 0)
            return Result.Failure<InventoryTransaction>(
                Error.Validation(nameof(quantity), "Quantity must be greater than zero."));

        if (string.IsNullOrWhiteSpace(reference))
            return Result.Failure<InventoryTransaction>(
                Error.Validation(nameof(reference), "Reference is required."));

        var tx = new InventoryTransaction(
            Guid.NewGuid(), TransactionType.In, productId,
            sourceWarehouseId: null, destinationWarehouseId,
            quantity, reference.Trim(), date ?? DateTime.UtcNow);

        return Result.Success(tx);
    }

    /// <summary>Creates a Stock OUT transaction, validating the product has enough stock.</summary>
    public static Result<InventoryTransaction> CreateStockOut(
        Guid productId,
        Guid sourceWarehouseId,
        int quantity,
        int availableStock,
        string reference,
        string productName,
        DateTime? date = null)
    {
        if (quantity <= 0)
            return Result.Failure<InventoryTransaction>(
                Error.Validation(nameof(quantity), "Quantity must be greater than zero."));

        if (availableStock < quantity)
            return Result.Failure<InventoryTransaction>(
                Error.InsufficientStock(productName, availableStock, quantity));

        if (string.IsNullOrWhiteSpace(reference))
            return Result.Failure<InventoryTransaction>(
                Error.Validation(nameof(reference), "Reference is required."));

        var tx = new InventoryTransaction(
            Guid.NewGuid(), TransactionType.Out, productId,
            sourceWarehouseId, destinationWarehouseId: null,
            quantity, reference.Trim(), date ?? DateTime.UtcNow);

        return Result.Success(tx);
    }

    /// <summary>Creates a Transfer transaction between two warehouses, validating stock at source.</summary>
    public static Result<InventoryTransaction> CreateTransfer(
        Guid productId,
        Guid sourceWarehouseId,
        Guid destinationWarehouseId,
        int quantity,
        int availableStock,
        string reference,
        string productName,
        DateTime? date = null)
    {
        if (sourceWarehouseId == destinationWarehouseId)
            return Result.Failure<InventoryTransaction>(
                Error.Validation("warehouse", "Source and destination warehouses cannot be the same."));

        if (quantity <= 0)
            return Result.Failure<InventoryTransaction>(
                Error.Validation(nameof(quantity), "Quantity must be greater than zero."));

        if (availableStock < quantity)
            return Result.Failure<InventoryTransaction>(
                Error.InsufficientStock(productName, availableStock, quantity));

        if (string.IsNullOrWhiteSpace(reference))
            return Result.Failure<InventoryTransaction>(
                Error.Validation(nameof(reference), "Reference is required."));

        var tx = new InventoryTransaction(
            Guid.NewGuid(), TransactionType.Transfer, productId,
            sourceWarehouseId, destinationWarehouseId,
            quantity, reference.Trim(), date ?? DateTime.UtcNow);

        return Result.Success(tx);
    }
}
