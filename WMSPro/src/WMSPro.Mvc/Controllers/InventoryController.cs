using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMSPro.Mvc.Extensions;

namespace WMSPro.Mvc.Controllers;

[Authorize]
public class InventoryController : Controller
{
    private readonly HttpClient _httpClient;

    public InventoryController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("WmsApi");
    }

    public async Task<IActionResult> Index()
    {
        var response = await _httpClient.GetAsync("api/transactions/inventory");
        
        if (response.IsSuccessStatusCode)
        {
            var inventory = await response.Content.ReadFromJsonAsync<List<InventoryItemViewModel>>();
            return View(inventory ?? new List<InventoryItemViewModel>());
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            return RedirectToAction("Login", "Account");

        TempData["ErrorMessage"] = "Failed to load inventory data.";
        return View(new List<InventoryItemViewModel>());
    }

    [HttpGet]
    public async Task<IActionResult> Export()
    {
        var response = await _httpClient.GetAsync("api/transactions/inventory");
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.GetErrorMessageAsync();
            TempData["ErrorMessage"] = $"Failed to export inventory. {error}";
            return RedirectToAction(nameof(Index));
        }

        var inventory = await response.Content.ReadFromJsonAsync<List<InventoryItemViewModel>>() ?? new List<InventoryItemViewModel>();

        var builder = new System.Text.StringBuilder();
        builder.AppendLine("Product,SKU,Warehouse,Quantity");

        foreach (var item in inventory)
        {
            builder.AppendLine($"\"{item.ProductName}\",{item.SKU},\"{item.WarehouseName}\",{item.Quantity}");
        }

        return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "Inventory_Report.csv");
    }
}

public class InventoryItemViewModel
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
