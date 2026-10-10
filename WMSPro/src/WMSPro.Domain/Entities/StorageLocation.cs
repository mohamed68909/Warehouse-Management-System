using WMSPro.Domain.Common;

namespace WMSPro.Domain.Entities;

public sealed class StorageLocation : Entity
{
    private StorageLocation() { }

    private StorageLocation(
        Guid id,
        Guid warehouseId,
        string locationCode,
        string zone,
        string aisle,
        string rack,
        int shelfLevel,
        string binNumber,
        int capacityUnits,
        int currentStock,
        Guid? productId,
        bool isActive) : base(id)
    {
        WarehouseId = warehouseId;
        LocationCode = locationCode;
        Zone = zone;
        Aisle = aisle;
        Rack = rack;
        ShelfLevel = shelfLevel;
        BinNumber = binNumber;
        CapacityUnits = capacityUnits;
        CurrentStock = currentStock;
        ProductId = productId;
        IsActive = isActive;
    }

    public Guid WarehouseId { get; private set; }
    public string LocationCode { get; private set; } = string.Empty;
    public string Zone { get; private set; } = string.Empty;
    public string Aisle { get; private set; } = string.Empty;
    public string Rack { get; private set; } = string.Empty;
    public int ShelfLevel { get; private set; }
    public string BinNumber { get; private set; } = string.Empty;
    public int CapacityUnits { get; private set; }
    public int CurrentStock { get; private set; }
    public Guid? ProductId { get; private set; }
    public bool IsActive { get; private set; }

    // Navigation properties
    public Warehouse? Warehouse { get; private set; }
    public Product? Product { get; private set; }

    // Computed properties
    public double CapacityUsagePercent =>
        CapacityUnits > 0 ? Math.Round((double)CurrentStock / CapacityUnits * 100, 1) : 0;

    public bool IsEmpty => CurrentStock == 0;
    public bool IsFull => CurrentStock >= CapacityUnits;

    // Factory Method
    public static Result<StorageLocation> Create(
        Guid warehouseId,
        string locationCode,
        string zone,
        string aisle,
        string rack,
        int shelfLevel,
        string binNumber,
        int capacityUnits,
        int currentStock = 0,
        Guid? productId = null)
    {
        if (warehouseId == Guid.Empty)
            return Result.Failure<StorageLocation>(Error.Validation(nameof(warehouseId), "Warehouse is required."));

        if (string.IsNullOrWhiteSpace(locationCode))
            return Result.Failure<StorageLocation>(Error.Validation(nameof(locationCode), "Location code is required."));

        if (capacityUnits <= 0)
            return Result.Failure<StorageLocation>(Error.Validation(nameof(capacityUnits), "Capacity must be positive."));

        if (currentStock < 0)
            return Result.Failure<StorageLocation>(Error.Validation(nameof(currentStock), "Current stock cannot be negative."));

        if (currentStock > capacityUnits)
            return Result.Failure<StorageLocation>(Error.Conflict($"Current stock ({currentStock}) exceeds bin capacity ({capacityUnits})."));

        var loc = new StorageLocation(
            Guid.NewGuid(),
            warehouseId,
            locationCode.Trim().ToUpper(),
            string.IsNullOrWhiteSpace(zone) ? "Standard Zone" : zone.Trim(),
            string.IsNullOrWhiteSpace(aisle) ? "A01" : aisle.Trim().ToUpper(),
            string.IsNullOrWhiteSpace(rack) ? "R01" : rack.Trim().ToUpper(),
            shelfLevel <= 0 ? 1 : shelfLevel,
            string.IsNullOrWhiteSpace(binNumber) ? "B01" : binNumber.Trim().ToUpper(),
            capacityUnits,
            currentStock,
            productId,
            isActive: true);

        return Result.Success(loc);
    }

    // Domain Methods
    public Result AssignProduct(Guid productId, int quantity)
    {
        if (productId == Guid.Empty)
            return Result.Failure(Error.Validation(nameof(productId), "Product ID is required."));

        if (quantity < 0)
            return Result.Failure(Error.Validation(nameof(quantity), "Quantity cannot be negative."));

        if (quantity > CapacityUnits)
            return Result.Failure(Error.Conflict($"Quantity ({quantity}) exceeds bin capacity ({CapacityUnits})."));

        ProductId = productId;
        CurrentStock = quantity;
        return Result.Success();
    }

    public Result AdjustStock(int newQuantity)
    {
        if (newQuantity < 0)
            return Result.Failure(Error.Validation(nameof(newQuantity), "Quantity cannot be negative."));

        if (newQuantity > CapacityUnits)
            return Result.Failure(Error.Conflict($"Quantity ({newQuantity}) exceeds bin capacity ({CapacityUnits})."));

        CurrentStock = newQuantity;
        if (CurrentStock == 0)
        {
            ProductId = null;
        }

        return Result.Success();
    }

    public void EmptyBin()
    {
        CurrentStock = 0;
        ProductId = null;
    }

    public void SetActive(bool active) => IsActive = active;
}
