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

public record DashboardStatsDto(
    int TotalProducts,
    int TotalStock,
    int LowStockAlertsCount,
    int TotalWarehouses,
    IReadOnlyList<StockMovementTrendDto> StockMovementTrend,
    IReadOnlyList<RecentActivityDto> RecentActivities);

// ─── Query ────────────────────────────────────────────────────────────────────
public record GetDashboardStatsQuery : IRequest<Result<DashboardStatsDto>>;
