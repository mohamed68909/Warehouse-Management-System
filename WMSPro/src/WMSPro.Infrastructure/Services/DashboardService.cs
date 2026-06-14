using Microsoft.EntityFrameworkCore;
using WMSPro.Application.Abstractions;
using WMSPro.Application.Dashboard.Queries;
using WMSPro.Domain.Common;
using WMSPro.Domain.Enums;
using WMSPro.Infrastructure.Persistence;

namespace WMSPro.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context) => _context = context;

    public async Task<Result<DashboardStatsDto>> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        // ── KPI Cards ───────────────────────────────────────────────────────────
        var totalProducts = await _context.Products.CountAsync(cancellationToken);
        var totalStock = await _context.Products.SumAsync(p => p.Quantity, cancellationToken);
        var lowStockCount = await _context.Products.CountAsync(p => p.Quantity <= 20, cancellationToken);
        var totalWarehouses = await _context.Warehouses.CountAsync(cancellationToken);

        // ── Stock Movement Trend (last 6 months) ────────────────────────────────
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-5);
        var cutoff = new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1);

        var transactions = await _context.InventoryTransactions
            .Where(t => t.Date >= cutoff)
            .Select(t => new { t.Type, t.Quantity, t.Date })
            .ToListAsync(cancellationToken);

        // Build trend grouped by month
        var months = Enumerable.Range(0, 6)
            .Select(i => DateTime.UtcNow.AddMonths(-5 + i))
            .ToList();

        var trend = months.Select(m =>
        {
            var monthTxs = transactions
                .Where(t => t.Date.Year == m.Year && t.Date.Month == m.Month)
                .ToList();

            return new StockMovementTrendDto(
                m.ToString("MMM"),
                monthTxs.Where(t => t.Type == TransactionType.In).Sum(t => t.Quantity),
                monthTxs.Where(t => t.Type == TransactionType.Out).Sum(t => t.Quantity));
        }).ToList().AsReadOnly();

        // ── Recent Activities (last 10) ─────────────────────────────────────────
        var recentTxs = await _context.InventoryTransactions
            .Include(t => t.Product)
            .Include(t => t.SourceWarehouse)
            .Include(t => t.DestinationWarehouse)
            .OrderByDescending(t => t.Date)
            .Take(10)
            .ToListAsync(cancellationToken);

        var recentActivities = recentTxs.Select(t =>
        {
            var action = t.Type switch
            {
                TransactionType.In => "Stock IN",
                TransactionType.Out => "Stock OUT",
                TransactionType.Transfer => "Transfer",
                _ => t.Type.ToString()
            };

            var warehouse = t.Type switch
            {
                TransactionType.In => t.DestinationWarehouse?.Name ?? "—",
                TransactionType.Out => t.SourceWarehouse?.Name ?? "—",
                TransactionType.Transfer =>
                    $"{t.SourceWarehouse?.Name} → {t.DestinationWarehouse?.Name}",
                _ => "—"
            };

            return new RecentActivityDto(
                action,
                t.Product?.Name ?? "—",
                t.Quantity,
                warehouse,
                t.Date);
        }).ToList().AsReadOnly();

        var stats = new DashboardStatsDto(
            totalProducts, totalStock, lowStockCount, totalWarehouses,
            trend, recentActivities);

        return Result.Success(stats);
    }
}
