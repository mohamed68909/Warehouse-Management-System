using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMSPro.Mvc.Extensions;

namespace WMSPro.Mvc.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly HttpClient _httpClient;

    public DashboardController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("WmsApi");
    }

    public async Task<IActionResult> Index()
    {
        var response = await _httpClient.GetAsync("api/dashboard/stats");
        
        if (response.IsSuccessStatusCode)
        {
            var stats = await response.Content.ReadFromJsonAsync<DashboardStatsViewModel>();
            return View(stats ?? new DashboardStatsViewModel());
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Account");
        }

        TempData["ErrorMessage"] = "Failed to load dashboard statistics.";
        return View(new DashboardStatsViewModel());
    }

    [HttpGet]
    public async Task<IActionResult> Export()
    {
        var response = await _httpClient.GetAsync("api/dashboard/stats");
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.GetErrorMessageAsync();
            TempData["ErrorMessage"] = $"Failed to generate report. {error}";
            return RedirectToAction(nameof(Index));
        }

        var stats = await response.Content.ReadFromJsonAsync<DashboardStatsViewModel>() ?? new DashboardStatsViewModel();

        var builder = new StringBuilder();
        builder.AppendLine("WMS Pro - Enterprise Executive Dashboard Report");
        builder.AppendLine($"Generated On,{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
        builder.AppendLine();
        
        builder.AppendLine("--- FINANCIAL & INVENTORY KPIs ---");
        builder.AppendLine($"Total Catalog Products,{stats.TotalProducts}");
        builder.AppendLine($"Total Stock Units In Hand,{stats.TotalStock}");
        builder.AppendLine($"Total Inventory Valuation,${stats.TotalInventoryValue:N2}");
        builder.AppendLine($"Active Warehouses Count,{stats.TotalWarehouses}");
        builder.AppendLine($"Inventory Turnover Ratio (Annualized),{stats.InventoryTurnoverRatio:N2}x");
        builder.AppendLine($"Days Sales of Inventory (DSI),{stats.DaysSalesOfInventory} days");
        builder.AppendLine($"Low Stock Warning Items,{stats.LowStockAlertsCount}");
        builder.AppendLine($"Dead / Slow-Moving Items Count,{stats.DeadStockCount}");
        builder.AppendLine($"Dead Stock Capital Tied Up,${stats.DeadStockValue:N2}");
        builder.AppendLine($"Order Fulfillment Rate,{stats.OrderFulfillmentRate}%");
        builder.AppendLine($"Total Sales Order Volume,${stats.TotalSalesRevenue:N2}");
        builder.AppendLine($"Total Purchase Commitments,${stats.TotalPurchaseExpense:N2}");
        builder.AppendLine();
        
        builder.AppendLine("--- WAREHOUSE CAPACITY UTILIZATION ---");
        builder.AppendLine("Warehouse,Location,Capacity (Units),Current Stock,Utilization %");
        foreach (var wh in stats.WarehouseCapacities)
        {
            builder.AppendLine($"\"{wh.Name}\",\"{wh.Location}\",{wh.Capacity},{wh.CurrentStock},{wh.UtilizationPercent}%");
        }
        builder.AppendLine();

        builder.AppendLine("--- ABC INVENTORY VALUATION ---");
        builder.AppendLine("Class,Products Count,Total Valuation,Share of Capital %");
        foreach (var abc in stats.AbcAnalysis)
        {
            builder.AppendLine($"\"{abc.Category}\",{abc.ProductCount},${abc.TotalValue:N2},{abc.ValuePercentage}%");
        }
        builder.AppendLine();

        builder.AppendLine("--- RECENT AUDIT ACTIVITIES ---");
        builder.AppendLine("Time,Action,Product,Quantity,Warehouse");
        foreach (var act in stats.RecentActivities)
        {
            builder.AppendLine($"{act.Time.ToLocalTime():yyyy-MM-dd HH:mm},{act.Action},\"{act.ProductName}\",{act.Quantity},\"{act.Warehouse}\"");
        }

        return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", $"WMS_Executive_Report_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPdfReport()
    {
        var response = await _httpClient.GetAsync("api/dashboard/report-pdf");
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.GetErrorMessageAsync();
            TempData["ErrorMessage"] = $"Failed to generate executive PDF report: {error}";
            return RedirectToAction(nameof(Index));
        }

        var pdfBytes = await response.Content.ReadAsByteArrayAsync();
        return File(pdfBytes, "application/pdf", $"WMS_Executive_Report_{DateTime.UtcNow:yyyyMMdd_HHmm}.pdf");
    }
}

// ─── View Models ─────────────────────────────────────────────────────────────
public class DashboardStatsViewModel
{
    public int TotalProducts { get; set; }
    public int TotalStock { get; set; }
    public int LowStockAlertsCount { get; set; }
    public int TotalWarehouses { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public double InventoryTurnoverRatio { get; set; }
    public int DaysSalesOfInventory { get; set; }
    public decimal DeadStockValue { get; set; }
    public int DeadStockCount { get; set; }
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public double OrderFulfillmentRate { get; set; }
    public decimal TotalSalesRevenue { get; set; }
    public decimal TotalPurchaseExpense { get; set; }

    public List<StockMovementTrendViewModel> StockMovementTrend { get; set; } = new();
    public List<RecentActivityViewModel> RecentActivities { get; set; } = new();
    public List<WarehouseCapacityViewModel> WarehouseCapacities { get; set; } = new();
    public List<AbcCategoryViewModel> AbcAnalysis { get; set; } = new();
    public List<DeadStockItemViewModel> DeadStockItems { get; set; } = new();
    public List<CriticalAlertViewModel> CriticalAlerts { get; set; } = new();
}

public class StockMovementTrendViewModel
{
    public string Month { get; set; } = string.Empty;
    public int StockIn { get; set; }
    public int StockOut { get; set; }
}

public class RecentActivityViewModel
{
    public string Action { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Warehouse { get; set; } = string.Empty;
    public DateTime Time { get; set; }
}

public class WarehouseCapacityViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int CurrentStock { get; set; }
    public double UtilizationPercent { get; set; }
}

public class AbcCategoryViewModel
{
    public string Category { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public decimal TotalValue { get; set; }
    public double ValuePercentage { get; set; }
}

public class DeadStockItemViewModel
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal TotalTiedValue { get; set; }
    public int DaysInactive { get; set; }
}

public class CriticalAlertViewModel
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "info";
    public DateTime Timestamp { get; set; }
}
