using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMSPro.Mvc.Extensions;

namespace WMSPro.Mvc.Controllers;

[Authorize]
public class LocationsController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly WMSPro.Mvc.Services.IWmsNotifier _notifier;

    public LocationsController(IHttpClientFactory httpClientFactory, WMSPro.Mvc.Services.IWmsNotifier notifier)
    {
        _httpClient = httpClientFactory.CreateClient("WmsApi");
        _notifier = notifier;
    }

    public async Task<IActionResult> Index([FromQuery] Guid? warehouseId, [FromQuery] string? zone)
    {
        var vm = new WarehouseMapViewModel
        {
            SelectedZone = zone
        };

        // 1. Load warehouses
        var whRes = await _httpClient.GetAsync("api/warehouses");
        if (whRes.IsSuccessStatusCode)
        {
            vm.Warehouses = await whRes.Content.ReadFromJsonAsync<List<WarehouseViewModel>>() ?? new();
        }

        if (!vm.Warehouses.Any())
        {
            TempData["ErrorMessage"] = "No warehouses found. Please create a warehouse first.";
            return View(vm);
        }

        // Set default selected warehouse if null
        vm.SelectedWarehouseId = warehouseId ?? vm.Warehouses.First().Id;
        vm.SelectedWarehouse = vm.Warehouses.FirstOrDefault(w => w.Id == vm.SelectedWarehouseId);

        // 2. Load locations for selected warehouse
        var locUrl = string.IsNullOrWhiteSpace(zone)
            ? $"api/storagelocations?warehouseId={vm.SelectedWarehouseId}"
            : $"api/storagelocations?warehouseId={vm.SelectedWarehouseId}&zone={Uri.EscapeDataString(zone)}";

        var locRes = await _httpClient.GetAsync(locUrl);
        if (locRes.IsSuccessStatusCode)
        {
            vm.Locations = await locRes.Content.ReadFromJsonAsync<List<StorageLocationViewModel>>() ?? new();
        }

        // 3. Load summary
        var sumRes = await _httpClient.GetAsync($"api/storagelocations/summary/{vm.SelectedWarehouseId}");
        if (sumRes.IsSuccessStatusCode)
        {
            vm.Summary = await sumRes.Content.ReadFromJsonAsync<SpatialSummaryViewModel>() ?? new();
        }

        // 4. Load products for assign modal
        var prodRes = await _httpClient.GetAsync("api/products");
        if (prodRes.IsSuccessStatusCode)
        {
            vm.Products = await prodRes.Content.ReadFromJsonAsync<List<ProductViewModel>>() ?? new();
        }

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Assign([FromBody] AssignBinRequest model)
    {
        if (model == null || model.LocationId == Guid.Empty || model.ProductId == Guid.Empty || model.Quantity < 0)
            return BadRequest(new { error = "Invalid parameters." });

        var res = await _httpClient.PostAsJsonAsync("api/storagelocations/assign", model);
        if (res.IsSuccessStatusCode)
        {
            await _notifier.NotifyBinAllocatedAsync("Bin Allocation", "Product", model.Quantity);
            return Ok(new { success = true, message = "Product successfully allocated to storage bin." });
        }

        var error = await res.GetErrorMessageAsync();
        return BadRequest(new { error });
    }

    [HttpPost]
    public async Task<IActionResult> EmptyLocation([FromRoute] Guid id)
    {
        if (id == Guid.Empty)
            return BadRequest(new { error = "Invalid location ID." });

        var res = await _httpClient.PostAsync($"api/storagelocations/{id}/empty", null);
        if (res.IsSuccessStatusCode)
        {
            return Ok(new { success = true, message = "Storage bin emptied successfully." });
        }

        var error = await res.GetErrorMessageAsync();
        return BadRequest(new { error });
    }
}

// ─── View Models ─────────────────────────────────────────────────────────────
public class WarehouseMapViewModel
{
    public Guid SelectedWarehouseId { get; set; }
    public WarehouseViewModel? SelectedWarehouse { get; set; }
    public string? SelectedZone { get; set; }

    public List<WarehouseViewModel> Warehouses { get; set; } = new();
    public List<StorageLocationViewModel> Locations { get; set; } = new();
    public List<ProductViewModel> Products { get; set; } = new();
    public SpatialSummaryViewModel Summary { get; set; } = new();
}

public class StorageLocationViewModel
{
    public Guid Id { get; set; }
    public Guid WarehouseId { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public string Aisle { get; set; } = string.Empty;
    public string Rack { get; set; } = string.Empty;
    public int ShelfLevel { get; set; }
    public string BinNumber { get; set; } = string.Empty;
    public int CapacityUnits { get; set; }
    public int CurrentStock { get; set; }
    public double UtilizationPercent { get; set; }
    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? ProductSku { get; set; }
    public bool IsEmpty { get; set; }
    public bool IsFull { get; set; }
    public bool IsActive { get; set; }
}

public class SpatialSummaryViewModel
{
    public Guid WarehouseId { get; set; }
    public int TotalBins { get; set; }
    public int OccupiedBins { get; set; }
    public int EmptyBins { get; set; }
    public double SpaceUtilizationPercent { get; set; }
    public List<string> Zones { get; set; } = new();
    public List<string> Aisles { get; set; } = new();
}

public class AssignBinRequest
{
    public Guid LocationId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
