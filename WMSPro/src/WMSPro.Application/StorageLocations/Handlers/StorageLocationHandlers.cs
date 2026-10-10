using MediatR;
using WMSPro.Application.Abstractions;
using WMSPro.Application.Abstractions.Repositories;
using WMSPro.Application.StorageLocations.Commands;
using WMSPro.Domain.Common;
using WMSPro.Domain.Entities;

namespace WMSPro.Application.StorageLocations.Handlers;

internal static class StorageLocationMapper
{
    public static StorageLocationDto ToDto(StorageLocation sl) => new(
        sl.Id,
        sl.WarehouseId,
        sl.LocationCode,
        sl.Zone,
        sl.Aisle,
        sl.Rack,
        sl.ShelfLevel,
        sl.BinNumber,
        sl.CapacityUnits,
        sl.CurrentStock,
        sl.CapacityUsagePercent,
        sl.ProductId,
        sl.Product?.Name,
        sl.Product?.SKU,
        sl.IsEmpty,
        sl.IsFull,
        sl.IsActive);
}

// ─── Get Storage Locations for a Warehouse ────────────────────────────────────
public class GetStorageLocationsHandler : IRequestHandler<GetStorageLocationsQuery, Result<IReadOnlyList<StorageLocationDto>>>
{
    private readonly IStorageLocationRepository _repo;

    public GetStorageLocationsHandler(IStorageLocationRepository repo) => _repo = repo;

    public async Task<Result<IReadOnlyList<StorageLocationDto>>> Handle(
        GetStorageLocationsQuery request, CancellationToken cancellationToken)
    {
        var locations = await _repo.GetByWarehouseAsync(request.WarehouseId, request.Zone, cancellationToken);
        return Result.Success<IReadOnlyList<StorageLocationDto>>(
            locations.Select(StorageLocationMapper.ToDto).ToList().AsReadOnly());
    }
}

// ─── Get By Id ────────────────────────────────────────────────────────────────
public class GetStorageLocationByIdHandler : IRequestHandler<GetStorageLocationByIdQuery, Result<StorageLocationDto>>
{
    private readonly IStorageLocationRepository _repo;

    public GetStorageLocationByIdHandler(IStorageLocationRepository repo) => _repo = repo;

    public async Task<Result<StorageLocationDto>> Handle(
        GetStorageLocationByIdQuery request, CancellationToken cancellationToken)
    {
        var location = await _repo.GetByIdAsync(request.Id, cancellationToken);
        if (location is null)
            return Result.Failure<StorageLocationDto>(Error.NotFound(nameof(StorageLocation), request.Id));

        return Result.Success(StorageLocationMapper.ToDto(location));
    }
}

// ─── Get Spatial Summary ──────────────────────────────────────────────────────
public class GetWarehouseSpatialSummaryHandler : IRequestHandler<GetWarehouseSpatialSummaryQuery, Result<WarehouseSpatialSummaryDto>>
{
    private readonly IStorageLocationRepository _repo;

    public GetWarehouseSpatialSummaryHandler(IStorageLocationRepository repo) => _repo = repo;

    public async Task<Result<WarehouseSpatialSummaryDto>> Handle(
        GetWarehouseSpatialSummaryQuery request, CancellationToken cancellationToken)
    {
        var locations = await _repo.GetByWarehouseAsync(request.WarehouseId, null, cancellationToken);

        var totalBins = locations.Count;
        var occupiedBins = locations.Count(l => !l.IsEmpty);
        var emptyBins = locations.Count(l => l.IsEmpty);
        var totalCapacity = locations.Sum(l => l.CapacityUnits);
        var totalStock = locations.Sum(l => l.CurrentStock);
        var utilization = totalCapacity > 0 ? Math.Round((double)totalStock / totalCapacity * 100, 1) : 0;

        var zones = locations.Select(l => l.Zone).Distinct().ToList().AsReadOnly();
        var aisles = locations.Select(l => l.Aisle).Distinct().OrderBy(a => a).ToList().AsReadOnly();

        var summary = new WarehouseSpatialSummaryDto(
            request.WarehouseId,
            totalBins,
            occupiedBins,
            emptyBins,
            utilization,
            zones,
            aisles);

        return Result.Success(summary);
    }
}

// ─── Assign Product to Bin ────────────────────────────────────────────────────
public class AssignBinProductHandler : IRequestHandler<AssignBinProductCommand, Result>
{
    private readonly IStorageLocationRepository _repo;
    private readonly IUnitOfWork _unitOfWork;

    public AssignBinProductHandler(IStorageLocationRepository repo, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(AssignBinProductCommand request, CancellationToken cancellationToken)
    {
        var location = await _repo.GetByIdAsync(request.LocationId, cancellationToken);
        if (location is null)
            return Result.Failure(Error.NotFound(nameof(StorageLocation), request.LocationId));

        var assignResult = location.AssignProduct(request.ProductId, request.Quantity);
        if (assignResult.IsFailure)
            return assignResult;

        _repo.Update(location);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

// ─── Empty Bin ────────────────────────────────────────────────────────────────
public class EmptyBinHandler : IRequestHandler<EmptyBinCommand, Result>
{
    private readonly IStorageLocationRepository _repo;
    private readonly IUnitOfWork _unitOfWork;

    public EmptyBinHandler(IStorageLocationRepository repo, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(EmptyBinCommand request, CancellationToken cancellationToken)
    {
        var location = await _repo.GetByIdAsync(request.LocationId, cancellationToken);
        if (location is null)
            return Result.Failure(Error.NotFound(nameof(StorageLocation), request.LocationId));

        location.EmptyBin();
        _repo.Update(location);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

// ─── Create Location ──────────────────────────────────────────────────────────
public class CreateStorageLocationHandler : IRequestHandler<CreateStorageLocationCommand, Result<StorageLocationDto>>
{
    private readonly IStorageLocationRepository _repo;
    private readonly IUnitOfWork _unitOfWork;

    public CreateStorageLocationHandler(IStorageLocationRepository repo, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<StorageLocationDto>> Handle(
        CreateStorageLocationCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repo.GetByCodeAsync(request.WarehouseId, request.LocationCode, cancellationToken);
        if (existing is not null)
            return Result.Failure<StorageLocationDto>(Error.Conflict($"Location code '{request.LocationCode}' already exists in this warehouse."));

        var locResult = StorageLocation.Create(
            request.WarehouseId,
            request.LocationCode,
            request.Zone,
            request.Aisle,
            request.Rack,
            request.ShelfLevel,
            request.BinNumber,
            request.CapacityUnits);

        if (locResult.IsFailure)
            return Result.Failure<StorageLocationDto>(locResult.Error);

        await _repo.AddAsync(locResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(StorageLocationMapper.ToDto(locResult.Value));
    }
}
