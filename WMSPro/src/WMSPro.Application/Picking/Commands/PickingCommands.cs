using MediatR;
using WMSPro.Domain.Common;

namespace WMSPro.Application.Picking.Commands;

public record PickItemDto(
    Guid Id,
    Guid OrderId,
    string OrderNumber,
    Guid ProductId,
    string ProductName,
    string ProductSku,
    Guid? StorageLocationId,
    string LocationCode,
    string Zone,
    string Aisle,
    string Rack,
    int ShelfLevel,
    string BinNumber,
    int SequenceOrder,
    int QuantityRequested,
    int QuantityPicked,
    bool IsPicked,
    DateTime? PickedAt);

public record PickingWaveDto(
    Guid Id,
    string WaveNumber,
    Guid WarehouseId,
    string? WarehouseName,
    string Status,
    string? AssignedPicker,
    int TotalItems,
    int PickedItems,
    double ProgressPercentage,
    bool IsFullyPicked,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    IReadOnlyList<PickItemDto> Items);

// ─── Commands ─────────────────────────────────────────────────────────────────
public record CreatePickingWaveCommand(
    IReadOnlyList<Guid> OrderIds,
    Guid WarehouseId,
    string? AssignedPicker = null) : IRequest<Result<PickingWaveDto>>;

public record ConfirmPickItemCommand(
    Guid WaveId,
    Guid PickItemId,
    int QuantityPicked) : IRequest<Result<PickingWaveDto>>;

public record CancelPickingWaveCommand(
    Guid WaveId) : IRequest<Result>;

// ─── Queries ──────────────────────────────────────────────────────────────────
public record GetPickingWavesQuery(
    Guid? WarehouseId = null,
    string? Status = null) : IRequest<Result<IReadOnlyList<PickingWaveDto>>>;

public record GetPickingWaveByIdQuery(
    Guid Id) : IRequest<Result<PickingWaveDto>>;
