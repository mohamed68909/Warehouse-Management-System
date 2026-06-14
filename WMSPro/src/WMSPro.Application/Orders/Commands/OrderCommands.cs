using MediatR;
using WMSPro.Domain.Common;
using WMSPro.Domain.Enums;

namespace WMSPro.Application.Orders.Commands;

// ─── DTOs ─────────────────────────────────────────────────────────────────────
public record OrderItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice);

public record OrderDto(
    Guid Id,
    string OrderNumber,
    string Type,
    string Status,
    string SupplierCustomerName,
    Guid WarehouseId,
    string? WarehouseName,
    decimal TotalAmount,
    DateTime Date,
    IReadOnlyList<OrderItemDto> Items);

// ─── Commands ─────────────────────────────────────────────────────────────────
public record CreateOrderItemRequest(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public record CreateOrderCommand(
    string OrderNumber,
    string Type,                  // "Purchase" or "Sales"
    string SupplierCustomerName,
    Guid WarehouseId,
    IReadOnlyList<CreateOrderItemRequest> Items) : IRequest<Result<OrderDto>>;

public record UpdateOrderStatusCommand(
    Guid Id,
    string Status) : IRequest<Result>;    // "Pending", "Processing", "Completed", "Cancelled"

// ─── Queries ──────────────────────────────────────────────────────────────────
public record GetOrdersQuery(string? Type = null) : IRequest<Result<IReadOnlyList<OrderDto>>>;

public record GetOrderByIdQuery(Guid Id) : IRequest<Result<OrderDto>>;
