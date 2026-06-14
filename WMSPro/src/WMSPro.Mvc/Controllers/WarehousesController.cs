using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WMSPro.Mvc.Extensions;

namespace WMSPro.Mvc.Controllers;

[Authorize]
public class WarehousesController : Controller
{
    private readonly HttpClient _httpClient;

    public WarehousesController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("WmsApi");
    }

    public async Task<IActionResult> Index()
    {
        var response = await _httpClient.GetAsync("api/warehouses");
        
        if (response.IsSuccessStatusCode)
        {
            var warehouses = await response.Content.ReadFromJsonAsync<List<WarehouseViewModel>>();
            return View(warehouses ?? new List<WarehouseViewModel>());
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            return RedirectToAction("Login", "Account");

        TempData["ErrorMessage"] = "Failed to load warehouses.";
        return View(new List<WarehouseViewModel>());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateWarehouseViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Invalid warehouse data.";
            return RedirectToAction(nameof(Index));
        }

        var response = await _httpClient.PostAsJsonAsync("api/warehouses", model);
        if (response.IsSuccessStatusCode)
        {
            TempData["SuccessMessage"] = "Warehouse created successfully!";
        }
        else
        {
            var error = await response.GetErrorMessageAsync();
            TempData["ErrorMessage"] = $"Failed to create warehouse. {error}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Edit([FromForm] UpdateWarehouseViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Invalid warehouse data.";
            return RedirectToAction(nameof(Index));
        }

        var response = await _httpClient.PutAsJsonAsync($"api/warehouses/{model.Id}", model);
        if (response.IsSuccessStatusCode)
        {
            TempData["SuccessMessage"] = "Warehouse updated successfully!";
        }
        else
        {
            var error = await response.GetErrorMessageAsync();
            TempData["ErrorMessage"] = $"Failed to update warehouse. {error}";
        }

        return RedirectToAction(nameof(Index));
    }
}

// ─── View Models ─────────────────────────────────────────────────────────────
public class WarehouseViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string Location { get; set; } = string.Empty;
    public string ManagerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
}

public class CreateWarehouseViewModel
{
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string Location { get; set; } = string.Empty;
    public string ManagerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class UpdateWarehouseViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string Location { get; set; } = string.Empty;
    public string ManagerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
