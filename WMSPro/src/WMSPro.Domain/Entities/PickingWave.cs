using WMSPro.Domain.Common;
using WMSPro.Domain.Enums;

namespace WMSPro.Domain.Entities;

public sealed class PickingWave : AggregateRoot
{
    private readonly List<PickItem> _items = [];

    private PickingWave() { }

    private PickingWave(
        Guid id,
        string waveNumber,
        Guid warehouseId,
        string? assignedPicker) : base(id)
    {
        WaveNumber = waveNumber;
        WarehouseId = warehouseId;
        AssignedPicker = assignedPicker;
        Status = PickingWaveStatus.Generated;
        CreatedAt = DateTime.UtcNow;
    }

    public string WaveNumber { get; private set; } = string.Empty;
    public Guid WarehouseId { get; private set; }
    public PickingWaveStatus Status { get; private set; }
    public string? AssignedPicker { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public IReadOnlyList<PickItem> Items => _items.AsReadOnly();

    public int TotalItems => _items.Count;
    public int PickedItems => _items.Count(i => i.IsPicked);
    public double ProgressPercentage => TotalItems > 0 ? Math.Round((double)PickedItems / TotalItems * 100, 1) : 0;
    public bool IsFullyPicked => TotalItems > 0 && _items.All(i => i.IsPicked);

    // Navigation property
    public Warehouse? Warehouse { get; private set; }

    // Factory Method
    public static Result<PickingWave> Create(string waveNumber, Guid warehouseId, string? assignedPicker = null)
    {
        if (string.IsNullOrWhiteSpace(waveNumber))
            return Result.Failure<PickingWave>(Error.Validation(nameof(waveNumber), "Wave number is required."));

        if (warehouseId == Guid.Empty)
            return Result.Failure<PickingWave>(Error.Validation(nameof(warehouseId), "Warehouse ID is required."));

        var wave = new PickingWave(Guid.NewGuid(), waveNumber.Trim().ToUpper(), warehouseId, assignedPicker?.Trim());
        return Result.Success(wave);
    }

    // Domain Methods
    public Result AddPickItem(
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
        int quantityRequested)
    {
        if (Status is PickingWaveStatus.Completed or PickingWaveStatus.Cancelled)
            return Result.Failure(Error.Conflict($"Cannot add items to a {Status} picking wave."));

        if (quantityRequested <= 0)
            return Result.Failure(Error.Validation(nameof(quantityRequested), "Quantity must be greater than zero."));

        var item = new PickItem(
            Guid.NewGuid(),
            Id,
            orderId,
            orderNumber,
            productId,
            productName,
            productSku,
            storageLocationId,
            locationCode,
            zone,
            aisle,
            rack,
            shelfLevel,
            binNumber,
            sequenceOrder,
            quantityRequested);

        _items.Add(item);
        return Result.Success();
    }

    public Result ConfirmItemPick(Guid pickItemId, int quantityPicked)
    {
        if (Status == PickingWaveStatus.Cancelled)
            return Result.Failure(Error.Conflict("Cannot pick items from a cancelled wave."));

        var item = _items.FirstOrDefault(i => i.Id == pickItemId);
        if (item is null)
            return Result.Failure(Error.NotFound("PickItem.NotFound", "Pick item not found in this wave."));

        var result = item.ConfirmPick(quantityPicked);
        if (!result.IsSuccess)
            return result;

        if (Status == PickingWaveStatus.Generated)
        {
            Status = PickingWaveStatus.InProgress;
        }

        if (IsFullyPicked)
        {
            Status = PickingWaveStatus.Completed;
            CompletedAt = DateTime.UtcNow;
        }

        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status == PickingWaveStatus.Completed)
            return Result.Failure(Error.Conflict("Cannot cancel an already completed wave."));

        Status = PickingWaveStatus.Cancelled;
        return Result.Success();
    }

    public void AssignPicker(string pickerName)
    {
        if (!string.IsNullOrWhiteSpace(pickerName))
            AssignedPicker = pickerName.Trim();
    }
}
