using WMSPro.Domain.Common;

namespace WMSPro.Domain.Entities;

public sealed class PickItem : Entity
{
    private PickItem() { }

    internal PickItem(
        Guid id,
        Guid pickingWaveId,
        Guid orderId,
        string orderNumber,
        Guid productId,
        string productName,
        string productSku,
        Guid? storageLocationId,
        string locationCode,
        string zone,
        string aisle,
        string rack,
        int shelfLevel,
        string binNumber,
        int sequenceOrder,
        int quantityRequested) : base(id)
    {
        PickingWaveId = pickingWaveId;
        OrderId = orderId;
        OrderNumber = orderNumber;
        ProductId = productId;
        ProductName = productName;
        ProductSku = productSku;
        StorageLocationId = storageLocationId;
        LocationCode = locationCode;
        Zone = zone;
        Aisle = aisle;
        Rack = rack;
        ShelfLevel = shelfLevel;
        BinNumber = binNumber;
        SequenceOrder = sequenceOrder;
        QuantityRequested = quantityRequested;
        QuantityPicked = 0;
        IsPicked = false;
    }

    public Guid PickingWaveId { get; private set; }
    public Guid OrderId { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string ProductSku { get; private set; } = string.Empty;
    public Guid? StorageLocationId { get; private set; }
    public string LocationCode { get; private set; } = string.Empty;
    public string Zone { get; private set; } = string.Empty;
    public string Aisle { get; private set; } = string.Empty;
    public string Rack { get; private set; } = string.Empty;
    public int ShelfLevel { get; private set; }
    public string BinNumber { get; private set; } = string.Empty;
    public int SequenceOrder { get; internal set; }
    public int QuantityRequested { get; private set; }
    public int QuantityPicked { get; private set; }
    public bool IsPicked { get; private set; }
    public DateTime? PickedAt { get; private set; }

    public Result ConfirmPick(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure(Error.Validation(nameof(quantity), "Quantity picked must be greater than zero."));

        if (quantity > QuantityRequested)
            return Result.Failure(Error.Conflict($"Quantity picked ({quantity}) cannot exceed requested quantity ({QuantityRequested})."));

        QuantityPicked = quantity;
        IsPicked = (QuantityPicked >= QuantityRequested);
        PickedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
