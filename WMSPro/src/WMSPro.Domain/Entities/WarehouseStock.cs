using WMSPro.Domain.Common;

namespace WMSPro.Domain.Entities;

public sealed class WarehouseStock : Entity
{
    private WarehouseStock() { }

    private WarehouseStock(Guid id, Guid warehouseId, Guid productId, int quantity) : base(id)
    {
        WarehouseId = warehouseId;
        ProductId = productId;
        Quantity = quantity;
    }

    public Guid WarehouseId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }

    // Navigation properties (set by EF Core)
    public Warehouse? Warehouse { get; private set; }
    public Product? Product { get; private set; }

    public static Result<WarehouseStock> Create(Guid warehouseId, Guid productId, int quantity)
    {
        if (warehouseId == Guid.Empty)
            return Result.Failure<WarehouseStock>(Error.Validation(nameof(warehouseId), "Warehouse ID is required."));

        if (productId == Guid.Empty)
            return Result.Failure<WarehouseStock>(Error.Validation(nameof(productId), "Product ID is required."));

        if (quantity < 0)
            return Result.Failure<WarehouseStock>(Error.Validation(nameof(quantity), "Quantity cannot be negative."));

        var ws = new WarehouseStock(Guid.NewGuid(), warehouseId, productId, quantity);
        return Result.Success(ws);
    }

    public Result AddStock(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure(Error.Validation(nameof(quantity), "Quantity to add must be positive."));

        Quantity += quantity;
        return Result.Success();
    }

    public Result RemoveStock(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure(Error.Validation(nameof(quantity), "Quantity to remove must be positive."));

        if (Quantity < quantity)
            return Result.Failure(Error.Validation(nameof(quantity), "Insufficient stock in the warehouse."));

        Quantity -= quantity;
        return Result.Success();
    }
}
