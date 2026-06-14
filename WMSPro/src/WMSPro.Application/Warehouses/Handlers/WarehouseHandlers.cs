using MediatR;
using WMSPro.Application.Abstractions;
using WMSPro.Application.Abstractions.Repositories;
using WMSPro.Application.Warehouses.Commands;
using WMSPro.Domain.Common;
using WMSPro.Domain.Entities;

namespace WMSPro.Application.Warehouses.Handlers;

// --- Helper mapper ---
internal static class WarehouseMapper
{
    public static WarehouseDto ToDto(Warehouse w) => new(
        w.Id, w.Name, w.Capacity, w.CurrentStock,
        w.CapacityUsagePercent, w.Location, w.ManagerName, w.Phone, w.Email);
}

// ─── Get All ────────────────────────────────────────────────────────────────
public class GetWarehousesHandler : IRequestHandler<GetWarehousesQuery, Result<IReadOnlyList<WarehouseDto>>>
{
    private readonly IWarehouseRepository _repo;
    public GetWarehousesHandler(IWarehouseRepository repo) => _repo = repo;

    public async Task<Result<IReadOnlyList<WarehouseDto>>> Handle(
        GetWarehousesQuery request, CancellationToken cancellationToken)
    {
        var warehouses = await _repo.GetAllAsync(cancellationToken);
        var dtos = warehouses.Select(WarehouseMapper.ToDto).ToList().AsReadOnly();
        return Result.Success<IReadOnlyList<WarehouseDto>>(dtos);
    }
}

// ─── Get By Id ──────────────────────────────────────────────────────────────
public class GetWarehouseByIdHandler : IRequestHandler<GetWarehouseByIdQuery, Result<WarehouseDto>>
{
    private readonly IWarehouseRepository _repo;
    public GetWarehouseByIdHandler(IWarehouseRepository repo) => _repo = repo;

    public async Task<Result<WarehouseDto>> Handle(
        GetWarehouseByIdQuery request, CancellationToken cancellationToken)
    {
        var warehouse = await _repo.GetByIdAsync(request.Id, cancellationToken);
        if (warehouse is null)
            return Result.Failure<WarehouseDto>(Error.NotFound(nameof(Warehouse), request.Id));

        return Result.Success(WarehouseMapper.ToDto(warehouse));
    }
}

// ─── Create ─────────────────────────────────────────────────────────────────
public class CreateWarehouseHandler : IRequestHandler<CreateWarehouseCommand, Result<WarehouseDto>>
{
    private readonly IWarehouseRepository _repo;
    private readonly IUnitOfWork _unitOfWork;

    public CreateWarehouseHandler(IWarehouseRepository repo, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<WarehouseDto>> Handle(
        CreateWarehouseCommand request, CancellationToken cancellationToken)
    {
        if (await _repo.ExistsByNameAsync(request.Name, cancellationToken))
            return Result.Failure<WarehouseDto>(Error.Conflict($"A warehouse named '{request.Name}' already exists."));

        var result = Warehouse.Create(
            request.Name, request.Capacity, request.Location,
            request.ManagerName, request.Phone, request.Email);

        if (result.IsFailure)
            return Result.Failure<WarehouseDto>(result.Error);

        await _repo.AddAsync(result.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(WarehouseMapper.ToDto(result.Value));
    }
}

// ─── Update ─────────────────────────────────────────────────────────────────
public class UpdateWarehouseHandler : IRequestHandler<UpdateWarehouseCommand, Result>
{
    private readonly IWarehouseRepository _repo;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateWarehouseHandler(IWarehouseRepository repo, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await _repo.GetByIdAsync(request.Id, cancellationToken);
        if (warehouse is null)
            return Result.Failure(Error.NotFound(nameof(Warehouse), request.Id));

        if (await _repo.ExistsByNameExceptAsync(request.Name, request.Id, cancellationToken))
            return Result.Failure(Error.Conflict($"A warehouse named '{request.Name}' already exists."));

        var updateResult = warehouse.Update(
            request.Name, request.Capacity, request.Location,
            request.ManagerName, request.Phone, request.Email);

        if (updateResult.IsFailure)
            return updateResult;

        _repo.Update(warehouse);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

// ─── Delete ─────────────────────────────────────────────────────────────────
public class DeleteWarehouseHandler : IRequestHandler<DeleteWarehouseCommand, Result>
{
    private readonly IWarehouseRepository _repo;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteWarehouseHandler(IWarehouseRepository repo, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await _repo.GetByIdAsync(request.Id, cancellationToken);
        if (warehouse is null)
            return Result.Failure(Error.NotFound(nameof(Warehouse), request.Id));

        _repo.Delete(warehouse);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
