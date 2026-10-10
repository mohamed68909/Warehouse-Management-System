using MediatR;
using WMSPro.Domain.Common;

namespace WMSPro.Application.StorageLocations.Commands;

// ─── DTOs ────────────────────────────────────────────────────────────────────
public record StorageLocationDto(
    Guid Id,
    Guid WarehouseId,
    string LocationCode,
    string Zone,
    string Aisle,
    string Rack,
    int ShelfLevel,
    string BinNumber,
    int CapacityUnits,
    int CurrentStock,
    double UtilizationPercent,
    Guid? ProductId,
    string? ProductName,
    string? ProductSku,
    bool IsEmpty,
    bool IsFull,
    bool IsActive);

public record WarehouseSpatialSummaryDto(
    Guid WarehouseId,
    int TotalBins,
    int OccupiedBins,
    int EmptyBins,
    double SpaceUtilizationPercent,
    IReadOnlyList<string> Zones,
    IReadOnlyList<string> Aisles);

// ─── Queries ──────────────────────────────────────────────────────────────────
public record GetStorageLocationsQuery(
    Guid WarehouseId,
    string? Zone = null) : IRequest<Result<IReadOnlyList<StorageLocationDto>>>;

public record GetStorageLocationByIdQuery(Guid Id) : IRequest<Result<StorageLocationDto>>;

public record GetWarehouseSpatialSummaryQuery(Guid WarehouseId) : IRequest<Result<WarehouseSpatialSummaryDto>>;

// ─── Commands ─────────────────────────────────────────────────────────────────
public record AssignBinProductCommand(
    Guid LocationId,
    Guid ProductId,
    int Quantity) : IRequest<Result>;

public record EmptyBinCommand(Guid LocationId) : IRequest<Result>;

public record CreateStorageLocationCommand(
    Guid WarehouseId,
    string LocationCode,
    string Zone,
    string Aisle,
    string Rack,
    int ShelfLevel,
    string BinNumber,
    int CapacityUnits) : IRequest<Result<StorageLocationDto>>;
