using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMSPro.Mvc.Extensions;

namespace WMSPro.Mvc.Controllers;

[Authorize]
public class ScannerController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly WMSPro.Mvc.Services.IWmsNotifier _notifier;

    public ScannerController(IHttpClientFactory httpClientFactory, WMSPro.Mvc.Services.IWmsNotifier notifier)
    {
        _httpClient = httpClientFactory.CreateClient("WmsApi");
        _notifier = notifier;
    }

    public async Task<IActionResult> Index()
    {
        var vm = new ScannerViewModel();
        
        // Load warehouses for quick Stock IN/OUT actions
        var whRes = await _httpClient.GetAsync("api/warehouses");
        if (whRes.IsSuccessStatusCode)
        {
            vm.Warehouses = await whRes.Content.ReadFromJsonAsync<List<WarehouseViewModel>>() ?? new();
        }

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Lookup([FromQuery] string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return BadRequest(new { error = "SKU is required." });

        var trimmedSku = sku.Trim();

        // 1. Fetch Product by SKU
        var prodRes = await _httpClient.GetAsync($"api/products/by-sku/{trimmedSku}");
        if (!prodRes.IsSuccessStatusCode)
        {
            return NotFound(new { error = $"Product with SKU '{trimmedSku}' not found." });
        }

        var product = await prodRes.Content.ReadFromJsonAsync<ProductViewModel>();
        if (product is null)
            return NotFound(new { error = "Product data invalid." });

        // 2. Fetch inventory locations breakdown for this product
        var invRes = await _httpClient.GetAsync("api/transactions/inventory");
        var stocks = new List<InventoryBreakdownItem>();
        if (invRes.IsSuccessStatusCode)
        {
            var allInventory = await invRes.Content.ReadFromJsonAsync<List<InventoryItemDto>>() ?? new();
            stocks = allInventory
                .Where(i => i.ProductId == product.Id)
                .Select(i => new InventoryBreakdownItem(i.WarehouseName, i.WarehouseId, i.Quantity))
                .ToList();
        }

        return Ok(new
        {
            product = product,
            stocks = stocks
        });
    }

    [HttpPost]
    public async Task<IActionResult> QuickTransaction([FromBody] QuickTransactionRequest request)
    {
        if (request == null || request.ProductId == Guid.Empty || request.Quantity <= 0)
            return BadRequest(new { error = "Invalid transaction parameters." });

        var reference = string.IsNullOrWhiteSpace(request.Reference)
            ? $"SCAN-{request.Type.ToUpper()}-{DateTime.UtcNow:yyyyMMdd-HHmm}"
            : request.Reference;

        var command = new
        {
            Type = request.Type, // "In" or "Out"
            ProductId = request.ProductId,
            SourceWarehouseId = request.Type.Equals("Out", StringComparison.OrdinalIgnoreCase) ? request.WarehouseId : (Guid?)null,
            DestinationWarehouseId = request.Type.Equals("In", StringComparison.OrdinalIgnoreCase) ? request.WarehouseId : (Guid?)null,
            Quantity = request.Quantity,
            Reference = reference
        };

        var response = await _httpClient.PostAsJsonAsync("api/transactions", command);
        if (response.IsSuccessStatusCode)
        {
            // Real-time broadcast to all connected clients
            await _notifier.NotifyTransactionAsync(request.Type, request.ProductName ?? "Item", request.Quantity, request.WarehouseName ?? "Facility", reference);
            return Ok(new { success = true, message = $"Stock {request.Type.ToUpper()} completed successfully!" });
        }

        var error = await response.GetErrorMessageAsync();
        return BadRequest(new { error = error });
    }
}

// ─── View Models & DTOs ─────────────────────────────────────────────────────
public class ScannerViewModel
{
    public List<WarehouseViewModel> Warehouses { get; set; } = new();
}

public record InventoryBreakdownItem(string WarehouseName, Guid WarehouseId, int Quantity);

public class QuickTransactionRequest
{
    public string Type { get; set; } = "In"; // "In" or "Out"
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public Guid WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public int Quantity { get; set; }
    public string? Reference { get; set; }
}

public class InventoryItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
