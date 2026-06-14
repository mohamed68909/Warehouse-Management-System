using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMSPro.Mvc.Extensions;

namespace WMSPro.Mvc.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly HttpClient _httpClient;

    public OrdersController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("WmsApi");
    }

    public async Task<IActionResult> Index([FromQuery] string? type)
    {
        var url = string.IsNullOrWhiteSpace(type) ? "api/orders" : $"api/orders?type={type}";
        var response = await _httpClient.GetAsync(url);
        
        var vm = new OrdersViewModel { FilterType = type };

        if (response.IsSuccessStatusCode)
        {
            vm.Orders = await response.Content.ReadFromJsonAsync<List<OrderViewModel>>() ?? new List<OrderViewModel>();
            
            // Load warehouses for the Create Order Modal
            var whRes = await _httpClient.GetAsync("api/warehouses");
            if (whRes.IsSuccessStatusCode)
                vm.Warehouses = await whRes.Content.ReadFromJsonAsync<List<WarehouseViewModel>>() ?? new List<WarehouseViewModel>();

            // Load products for the items selection
            var prodRes = await _httpClient.GetAsync("api/products");
            if (prodRes.IsSuccessStatusCode)
                vm.Products = await prodRes.Content.ReadFromJsonAsync<List<ProductViewModel>>() ?? new List<ProductViewModel>();

            return View(vm);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            return RedirectToAction("Login", "Account");

        TempData["ErrorMessage"] = "Failed to load orders.";
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateOrderRequest model)
    {
        // Auto-generate OrderNumber
        var prefix = model.Type == "Purchase" ? "PO-" : "SO-";
        model.OrderNumber = prefix + DateTime.UtcNow.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();

        if (!ModelState.IsValid || model.Items == null || !model.Items.Any())
        {
            TempData["ErrorMessage"] = "Invalid order data or no items added.";
            return RedirectToAction(nameof(Index));
        }

        var response = await _httpClient.PostAsJsonAsync("api/orders", model);
        if (response.IsSuccessStatusCode)
        {
            TempData["SuccessMessage"] = "Order created successfully!";
        }
        else
        {
            var error = await response.GetErrorMessageAsync();
            TempData["ErrorMessage"] = $"Failed to create order. {error}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus([FromForm] Guid id, [FromForm] string status)
    {
        var request = new { Id = id, Status = status };
        var response = await _httpClient.PatchAsJsonAsync($"api/orders/{id}/status", request);
        
        if (response.IsSuccessStatusCode)
        {
            TempData["SuccessMessage"] = "Order status updated successfully!";
        }
        else
        {
            var error = await response.GetErrorMessageAsync();
            TempData["ErrorMessage"] = $"Failed to update order status. {error}";
        }

        return RedirectToAction(nameof(Index));
    }
}

// ─── View Models ─────────────────────────────────────────────────────────────
public class OrdersViewModel
{
    public string? FilterType { get; set; }
    public List<OrderViewModel> Orders { get; set; } = new();
    public List<WarehouseViewModel> Warehouses { get; set; } = new();
    public List<ProductViewModel> Products { get; set; } = new();

    public int TotalOrders => Orders.Count;
    public decimal PurchaseOrdersTotal => Orders.Where(o => o.Type == "Purchase").Sum(o => o.TotalAmount);
    public decimal SalesOrdersTotal => Orders.Where(o => o.Type == "Sales").Sum(o => o.TotalAmount);
}

public class OrderViewModel
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SupplierCustomerName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime Date { get; set; }
    public List<OrderItemViewModel> Items { get; set; } = new();
}

public class OrderItemViewModel
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class CreateOrderRequest
{
    public string OrderNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string SupplierCustomerName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}

public class CreateOrderItemRequest
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
