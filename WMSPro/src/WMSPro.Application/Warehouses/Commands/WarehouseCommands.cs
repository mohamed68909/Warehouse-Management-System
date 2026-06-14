using MediatR;
using WMSPro.Domain.Common;

namespace WMSPro.Application.Warehouses.Commands;

// --- DTOs ---
public record WarehouseDto(
    Guid Id,
    string Name,
    int Capacity,
    int CurrentStock,
    double CapacityUsagePercent,
    string Location,
    string ManagerName,
    string Phone,
    string Email);

// --- Commands ---
public record CreateWarehouseCommand(
    string Name,
    int Capacity,
    string Location,
    string ManagerName,
    string Phone,
    string Email) : IRequest<Result<WarehouseDto>>;

public record UpdateWarehouseCommand(
    Guid Id,
    string Name,
    int Capacity,
    string Location,
    string ManagerName,
    string Phone,
    string Email) : IRequest<Result>;

public record DeleteWarehouseCommand(Guid Id) : IRequest<Result>;

// --- Queries ---
public record GetWarehousesQuery : IRequest<Result<IReadOnlyList<WarehouseDto>>>;

public record GetWarehouseByIdQuery(Guid Id) : IRequest<Result<WarehouseDto>>;
