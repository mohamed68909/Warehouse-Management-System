using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMSPro.Mvc.Extensions;
using WMSPro.Mvc.Services;

namespace WMSPro.Mvc.Controllers;

[Authorize]
public class PickingController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly IWmsNotifier _notifier;

    public PickingController(IHttpClientFactory httpClientFactory, IWmsNotifier notifier)
    {
        _httpClient = httpClientFactory.CreateClient("WmsApi");
        _notifier = notifier;
    }

    public async Task<IActionResult> Index([FromQuery] Guid? warehouseId, [FromQuery] string? status)
    {
        var vm = new PickingWavesViewModel();

        // 1. Load Waves
        var url = "api/picking-waves?";
        if (warehouseId.HasValue) url += $"warehouseId={warehouseId.Value}&";
        if (!string.IsNullOrWhiteSpace(status)) url += $"status={status}&";

        var wavesRes = await _httpClient.GetAsync(url);
        if (wavesRes.IsSuccessStatusCode)
        {
            vm.Waves = await wavesRes.Content.ReadFromJsonAsync<List<PickingWaveViewModel>>() ?? new List<PickingWaveViewModel>();
        }

        // 2. Load Warehouses
        var whRes = await _httpClient.GetAsync("api/warehouses");
        if (whRes.IsSuccessStatusCode)
        {
            vm.Warehouses = await whRes.Content.ReadFromJsonAsync<List<WarehouseViewModel>>() ?? new List<WarehouseViewModel>();
        }

        // 3. Load Pending Sales Orders for wave generation
        var ordersRes = await _httpClient.GetAsync("api/orders?type=Sales");
        if (ordersRes.IsSuccessStatusCode)
        {
            var allSalesOrders = await ordersRes.Content.ReadFromJsonAsync<List<OrderViewModel>>() ?? new List<OrderViewModel>();
            vm.PendingSalesOrders = allSalesOrders
                .Where(o => o.Status == "Pending" || o.Status == "Processing")
                .ToList();
        }

        return View(vm);
    }

    public async Task<IActionResult> Active(Guid id)
    {
        var response = await _httpClient.GetAsync($"api/picking-waves/{id}");
        if (!response.IsSuccessStatusCode)
        {
            TempData["ErrorMessage"] = "Picking wave not found.";
            return RedirectToAction(nameof(Index));
        }

        var wave = await response.Content.ReadFromJsonAsync<PickingWaveViewModel>();
        if (wave is null)
        {
            TempData["ErrorMessage"] = "Failed to deserialize picking wave.";
            return RedirectToAction(nameof(Index));
        }

        return View(wave);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateWaveFormModel model)
    {
        if (model.OrderIds == null || !model.OrderIds.Any())
        {
            TempData["ErrorMessage"] = "Please select at least one sales order for the wave.";
            return RedirectToAction(nameof(Index));
        }

        var payload = new
        {
            OrderIds = model.OrderIds,
            WarehouseId = model.WarehouseId,
            AssignedPicker = string.IsNullOrWhiteSpace(model.AssignedPicker) ? User.Identity?.Name ?? "Floor Operator" : model.AssignedPicker
        };

        var response = await _httpClient.PostAsJsonAsync("api/picking-waves", payload);
        if (response.IsSuccessStatusCode)
        {
            var created = await response.Content.ReadFromJsonAsync<PickingWaveViewModel>();
            await _notifier.NotifyOrderStatusAsync(created?.WaveNumber ?? "Wave", "Generated", "Logistics", created?.TotalItems ?? 0);
            TempData["SuccessMessage"] = $"Picking wave '{created?.WaveNumber}' generated with optimized S-Shape route!";
            return RedirectToAction(nameof(Active), new { id = created?.Id });
        }

        var error = await response.GetErrorMessageAsync();
        TempData["ErrorMessage"] = $"Failed to create picking wave. {error}";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmItem([FromForm] Guid waveId, [FromForm] Guid itemId, [FromForm] int quantityPicked)
    {
        var payload = new { QuantityPicked = quantityPicked };
        var response = await _httpClient.PostAsJsonAsync($"api/picking-waves/{waveId}/items/{itemId}/confirm", payload);

        if (response.IsSuccessStatusCode)
        {
            var updatedWave = await response.Content.ReadFromJsonAsync<PickingWaveViewModel>();
            var item = updatedWave?.Items.FirstOrDefault(i => i.Id == itemId);
            await _notifier.NotifyTransactionAsync("PICKED", item?.ProductName ?? "Item", quantityPicked, item?.LocationCode ?? "Bin", updatedWave?.WaveNumber ?? "Wave");

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, wave = updatedWave });
            }

            TempData["SuccessMessage"] = "Item pick confirmed successfully!";
        }
        else
        {
            var error = await response.GetErrorMessageAsync();
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, error });
            }
            TempData["ErrorMessage"] = $"Failed to confirm pick: {error}";
        }

        return RedirectToAction(nameof(Active), new { id = waveId });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPdf(Guid id)
    {
        var response = await _httpClient.GetAsync($"api/picking-waves/{id}/pdf");
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.GetErrorMessageAsync();
            TempData["ErrorMessage"] = $"Failed to generate pick list PDF: {error}";
            return RedirectToAction(nameof(Active), new { id });
        }

        var pdfBytes = await response.Content.ReadAsByteArrayAsync();
        return File(pdfBytes, "application/pdf", $"PickList_{id.ToString().Substring(0, 8)}.pdf");
    }
}

// ─── View Models ─────────────────────────────────────────────────────────────
public class PickingWavesViewModel
{
    public List<PickingWaveViewModel> Waves { get; set; } = new();
    public List<WarehouseViewModel> Warehouses { get; set; } = new();
    public List<OrderViewModel> PendingSalesOrders { get; set; } = new();
}

public class PickingWaveViewModel
{
    public Guid Id { get; set; }
    public string WaveNumber { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AssignedPicker { get; set; }
    public int TotalItems { get; set; }
    public int PickedItems { get; set; }
    public double ProgressPercentage { get; set; }
    public bool IsFullyPicked { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<PickItemViewModel> Items { get; set; } = new();
}

public class PickItemViewModel
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public Guid? StorageLocationId { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public string Aisle { get; set; } = string.Empty;
    public string Rack { get; set; } = string.Empty;
    public int ShelfLevel { get; set; }
    public string BinNumber { get; set; } = string.Empty;
    public int SequenceOrder { get; set; }
    public int QuantityRequested { get; set; }
    public int QuantityPicked { get; set; }
    public bool IsPicked { get; set; }
    public DateTime? PickedAt { get; set; }
}

public class CreateWaveFormModel
{
    public Guid WarehouseId { get; set; }
    public List<Guid> OrderIds { get; set; } = new();
    public string? AssignedPicker { get; set; }
}
