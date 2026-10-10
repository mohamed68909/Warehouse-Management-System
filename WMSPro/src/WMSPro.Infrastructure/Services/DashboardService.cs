using Microsoft.EntityFrameworkCore;
using WMSPro.Application.Abstractions;
using WMSPro.Application.Dashboard.Queries;
using WMSPro.Domain.Common;
using WMSPro.Domain.Entities;
using WMSPro.Domain.Enums;
using WMSPro.Infrastructure.Persistence;

namespace WMSPro.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context) => _context = context;

    public async Task<Result<DashboardStatsDto>> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        // ── Products & Valuation ────────────────────────────────────────────────
        var products = await _context.Products.AsNoTracking().ToListAsync(cancellationToken);
        var totalProducts = products.Count;
        var totalStock = products.Sum(p => p.Quantity);
        var lowStockCount = products.Count(p => p.Quantity <= 20);
        var totalInventoryValue = products.Sum(p => p.Price * p.Quantity);

        // ── Warehouses & Capacity ───────────────────────────────────────────────
        var warehouses = await _context.Warehouses.AsNoTracking().ToListAsync(cancellationToken);
        var totalWarehouses = warehouses.Count;

        var warehouseCapacities = warehouses.Select(w => new WarehouseCapacityDto(
            w.Id,
            w.Name,
            w.Location,
            w.Capacity,
            w.CurrentStock,
            w.CapacityUsagePercent
        )).ToList().AsReadOnly();

        // ── Stock Movement Trend (last 6 months) ────────────────────────────────
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-5);
        var cutoff = new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1);

        var transactions = await _context.InventoryTransactions
            .AsNoTracking()
            .Where(t => t.Date >= cutoff)
            .Select(t => new { t.Type, t.Quantity, t.Date, t.ProductId })
            .ToListAsync(cancellationToken);

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

        // ── ABC Inventory Classification (Pareto 80/15/5) ──────────────────────
        var sortedByVal = products
            .Where(p => p.Quantity > 0)
            .OrderByDescending(p => p.Price * p.Quantity)
            .ToList();

        var totalStockValuation = sortedByVal.Sum(p => p.Price * p.Quantity);
        var listA = new List<Product>();
        var listB = new List<Product>();
        var listC = new List<Product>();
        decimal runningTotal = 0;

        if (totalStockValuation > 0)
        {
            foreach (var prod in sortedByVal)
            {
                var val = prod.Price * prod.Quantity;
                runningTotal += val;
                var cumulativePct = (double)(runningTotal / totalStockValuation) * 100.0;

                if (cumulativePct <= 75.0 || listA.Count == 0)
                    listA.Add(prod);
                else if (cumulativePct <= 95.0 || listB.Count == 0)
                    listB.Add(prod);
                else
                    listC.Add(prod);
            }
        }

        var valA = listA.Sum(p => p.Price * p.Quantity);
        var valB = listB.Sum(p => p.Price * p.Quantity);
        var valC = listC.Sum(p => p.Price * p.Quantity);

        var abcAnalysis = new List<AbcCategorySummaryDto>
        {
            new("Class A (High Value)", listA.Count, valA, totalStockValuation > 0 ? Math.Round((double)(valA / totalStockValuation) * 100, 1) : 0),
            new("Class B (Moderate Value)", listB.Count, valB, totalStockValuation > 0 ? Math.Round((double)(valB / totalStockValuation) * 100, 1) : 0),
            new("Class C (Low Value / Bulk)", listC.Count, valC, totalStockValuation > 0 ? Math.Round((double)(valC / totalStockValuation) * 100, 1) : 0)
        }.AsReadOnly();

        // ── Dead Stock / Slow-Moving Detection (60+ days inactive) ──────────────
        var sixtyDaysAgo = DateTime.UtcNow.AddDays(-60);
        var recentOutProductIds = transactions
            .Where(t => t.Type == TransactionType.Out && t.Date >= sixtyDaysAgo)
            .Select(t => t.ProductId)
            .Distinct()
            .ToHashSet();

        var deadStockProducts = products
            .Where(p => p.Quantity > 0 && !recentOutProductIds.Contains(p.Id))
            .OrderByDescending(p => p.Price * p.Quantity)
            .ToList();

        var deadStockValue = deadStockProducts.Sum(p => p.Price * p.Quantity);
        var deadStockCount = deadStockProducts.Count;

        var deadStockItems = deadStockProducts
            .Take(8)
            .Select(p => new DeadStockItemDto(
                p.Id,
                p.Name,
                p.SKU,
                p.Quantity,
                p.Price,
                p.Price * p.Quantity,
                60))
            .ToList().AsReadOnly();

        // ── Inventory Turnover & DSI ───────────────────────────────────────────
        var totalUnitsOut = transactions
            .Where(t => t.Type == TransactionType.Out)
            .Sum(t => t.Quantity);

        double turnoverRatio = 0.0;
        int dsi = 0;
        if (totalStock > 0)
        {
            turnoverRatio = Math.Round((double)(totalUnitsOut * 2) / totalStock, 2);
            if (turnoverRatio > 0.05)
            {
                dsi = (int)Math.Round(365.0 / turnoverRatio);
            }
        }

        // ── Orders & Performance ────────────────────────────────────────────────
        var orders = await _context.Orders.AsNoTracking().ToListAsync(cancellationToken);
        var totalOrders = orders.Count;
        var completedOrders = orders.Count(o => o.Status == OrderStatus.Completed);
        var fulfillmentRate = totalOrders > 0 ? Math.Round((double)completedOrders / totalOrders * 100, 1) : 0.0;
        var totalSales = orders.Where(o => o.Type == OrderType.Sales && o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount);
        var totalPurchases = orders.Where(o => o.Type == OrderType.Purchase && o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount);

        // ── Critical Alerts ────────────────────────────────────────────────────
        var alerts = new List<CriticalAlertDto>();

        // 1. Low stock critical
        var criticalLow = products.Where(p => p.Quantity <= 10).OrderBy(p => p.Quantity).Take(3);
        foreach (var p in criticalLow)
        {
            alerts.Add(new CriticalAlertDto(
                "LowStock",
                $"Low Stock: {p.Name}",
                $"Only {p.Quantity} units remaining (SKU: {p.SKU}). Reorder required.",
                p.Quantity <= 5 ? "danger" : "warning",
                DateTime.UtcNow));
        }

        // 2. High capacity warehouses
        var highCap = warehouses.Where(w => w.CapacityUsagePercent >= 80);
        foreach (var w in highCap)
        {
            alerts.Add(new CriticalAlertDto(
                "CapacityWarning",
                $"Capacity Warning: {w.Name}",
                $"Utilization has reached {w.CapacityUsagePercent}% ({w.CurrentStock:N0} / {w.Capacity:N0} units).",
                w.CapacityUsagePercent >= 90 ? "danger" : "warning",
                DateTime.UtcNow));
        }

        // 3. Pending orders
        var pendingList = orders.Where(o => o.Status == OrderStatus.Pending).Take(2);
        foreach (var po in pendingList)
        {
            alerts.Add(new CriticalAlertDto(
                "PendingOrder",
                $"Pending {po.Type}: {po.OrderNumber}",
                $"Order for '{po.SupplierCustomerName}' with value ${po.TotalAmount:N2} requires review.",
                "info",
                po.Date));
        }

        // ── Recent Activities (last 10) ─────────────────────────────────────────
        var recentTxs = await _context.InventoryTransactions
            .AsNoTracking()
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
            totalProducts,
            totalStock,
            lowStockCount,
            totalWarehouses,
            totalInventoryValue,
            turnoverRatio,
            dsi,
            deadStockValue,
            deadStockCount,
            totalOrders,
            completedOrders,
            fulfillmentRate,
            totalSales,
            totalPurchases,
            trend,
            recentActivities,
            warehouseCapacities,
            abcAnalysis,
            deadStockItems,
            alerts.AsReadOnly());

        return Result.Success(stats);
    }
}
