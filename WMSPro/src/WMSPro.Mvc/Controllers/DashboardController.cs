using System.Net.Http.Headers;
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
            return View(stats);
        }

        // If unauthorized or error, could log and handle accordingly
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

        var builder = new System.Text.StringBuilder();
        builder.AppendLine("WMS Pro - General Dashboard Report");
        builder.AppendLine($"Generated On,{DateTime.Now:yyyy-MM-dd HH:mm}");
        builder.AppendLine();
        
        builder.AppendLine("--- KEY PERFORMANCE INDICATORS ---");
        builder.AppendLine($"Total Products,{stats.TotalProducts}");
        builder.AppendLine($"Total Stock (Units),{stats.TotalStock}");
        builder.AppendLine($"Low Stock Alerts,{stats.LowStockAlertsCount}");
        builder.AppendLine($"Active Warehouses,{stats.TotalWarehouses}");
        builder.AppendLine();
        
        builder.AppendLine("--- RECENT ACTIVITIES ---");
        builder.AppendLine("Time,Action,Product,Quantity,Warehouse");
        
        foreach (var act in stats.RecentActivities)
        {
            builder.AppendLine($"{act.Time.ToLocalTime():yyyy-MM-dd HH:mm},{act.Action},\"{act.ProductName}\",{act.Quantity},\"{act.Warehouse}\"");
        }

        return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "General_Report.csv");
    }
}

// ─── View Models ─────────────────────────────────────────────────────────────
public class DashboardStatsViewModel
{
    public int TotalProducts { get; set; }
    public int TotalStock { get; set; }
    public int LowStockAlertsCount { get; set; }
    public int TotalWarehouses { get; set; }
    public List<StockMovementTrendViewModel> StockMovementTrend { get; set; } = new();
    public List<RecentActivityViewModel> RecentActivities { get; set; } = new();
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
