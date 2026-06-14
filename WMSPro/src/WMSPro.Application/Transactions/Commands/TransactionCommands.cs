using MediatR;
using WMSPro.Domain.Common;
using WMSPro.Domain.Enums;

namespace WMSPro.Application.Transactions.Commands;

// ─── DTOs ────────────────────────────────────────────────────────────────────
public record TransactionDto(
    Guid Id,
    string Type,
    Guid ProductId,
    string ProductName,
    string SKU,
    Guid? SourceWarehouseId,
    string? SourceWarehouseName,
    Guid? DestinationWarehouseId,
    string? DestinationWarehouseName,
    int Quantity,
    string Reference,
    DateTime Date);

// ─── Commands ─────────────────────────────────────────────────────────────────
public record CreateTransactionCommand(
    string Type,           // "In", "Out", "Transfer"
    Guid ProductId,
    Guid? SourceWarehouseId,
    Guid? DestinationWarehouseId,
    int Quantity,
    string Reference) : IRequest<Result<TransactionDto>>;

// ─── Queries ──────────────────────────────────────────────────────────────────
public record GetTransactionsQuery(
    string? ProductSearch = null,
    string? Type = null) : IRequest<Result<IReadOnlyList<TransactionDto>>>;

public record GetInventoryQuery : IRequest<Result<IReadOnlyList<InventoryItemDto>>>;

// ─── Inventory DTO (Product stock per warehouse) ──────────────────────────────
public record InventoryItemDto(
    Guid ProductId,
    string ProductName,
    string SKU,
    Guid WarehouseId,
    string WarehouseName,
    int Quantity);
