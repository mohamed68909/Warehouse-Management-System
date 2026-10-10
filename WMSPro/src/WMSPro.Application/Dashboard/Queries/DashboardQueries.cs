using MediatR;
using WMSPro.Domain.Common;

namespace WMSPro.Application.Dashboard.Queries;

// ─── DTOs ─────────────────────────────────────────────────────────────────────
public record StockMovementTrendDto(string Month, int StockIn, int StockOut);

public record RecentActivityDto(
    string Action,      // "Stock IN", "Stock OUT", "Transfer"
    string ProductName,
    int Quantity,
    string Warehouse,
    DateTime Time);

public record WarehouseCapacityDto(
    Guid Id,
    string Name,
    string Location,
    int Capacity,
    int CurrentStock,
    double UtilizationPercent);

public record AbcCategorySummaryDto(
    string Category,        // "A" (Top ~80% Value), "B" (~15% Value), "C" (~5% Value)
    int ProductCount,
    decimal TotalValue,
    double ValuePercentage);

public record DeadStockItemDto(
    Guid ProductId,
    string ProductName,
    string SKU,
    int Quantity,
    decimal Price,
    decimal TotalTiedValue,
    int DaysInactive);

public record CriticalAlertDto(
    string Type,            // "LowStock", "CapacityWarning", "PendingOrder"
    string Title,
    string Message,
    string Severity,        // "danger", "warning", "info"
    DateTime Timestamp);

public record DashboardStatsDto(
    int TotalProducts,
    int TotalStock,
    int LowStockAlertsCount,
    int TotalWarehouses,
    decimal TotalInventoryValue,
    double InventoryTurnoverRatio,
    int DaysSalesOfInventory,
    decimal DeadStockValue,
    int DeadStockCount,
    int TotalOrders,
    int CompletedOrders,
    double OrderFulfillmentRate,
    decimal TotalSalesRevenue,
    decimal TotalPurchaseExpense,
    IReadOnlyList<StockMovementTrendDto> StockMovementTrend,
    IReadOnlyList<RecentActivityDto> RecentActivities,
    IReadOnlyList<WarehouseCapacityDto> WarehouseCapacities,
    IReadOnlyList<AbcCategorySummaryDto> AbcAnalysis,
    IReadOnlyList<DeadStockItemDto> DeadStockItems,
    IReadOnlyList<CriticalAlertDto> CriticalAlerts);

// ─── Query ────────────────────────────────────────────────────────────────────
public record GetDashboardStatsQuery : IRequest<Result<DashboardStatsDto>>;
