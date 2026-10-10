using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMSPro.Mvc.Extensions;

namespace WMSPro.Mvc.Controllers;

[Authorize]
public class TransactionsController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly WMSPro.Mvc.Services.IWmsNotifier _notifier;

    public TransactionsController(IHttpClientFactory httpClientFactory, WMSPro.Mvc.Services.IWmsNotifier notifier)
    {
        _httpClient = httpClientFactory.CreateClient("WmsApi");
        _notifier = notifier;
    }

    public async Task<IActionResult> Index([FromQuery] string? search, [FromQuery] string? type)
    {
        var url = "api/transactions";
        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) queryParams.Add($"search={search}");
        if (!string.IsNullOrWhiteSpace(type)) queryParams.Add($"type={type}");
        
        if (queryParams.Any()) url += "?" + string.Join("&", queryParams);

        var response = await _httpClient.GetAsync(url);
        
        var vm = new TransactionsViewModel
        {
            SearchQuery = search,
            FilterType = type
        };

        if (response.IsSuccessStatusCode)
        {
            vm.Transactions = await response.Content.ReadFromJsonAsync<List<TransactionViewModel>>() ?? new List<TransactionViewModel>();
            
            // Load products and warehouses for the Add Transaction Modal
            var prodRes = await _httpClient.GetAsync("api/products");
            if (prodRes.IsSuccessStatusCode)
                vm.Products = await prodRes.Content.ReadFromJsonAsync<List<ProductViewModel>>() ?? new List<ProductViewModel>();

            var whRes = await _httpClient.GetAsync("api/warehouses");
            if (whRes.IsSuccessStatusCode)
                vm.Warehouses = await whRes.Content.ReadFromJsonAsync<List<WarehouseViewModel>>() ?? new List<WarehouseViewModel>();

            return View(vm);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            return RedirectToAction("Login", "Account");

        TempData["ErrorMessage"] = "Failed to load transactions.";
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Export([FromQuery] string? search, [FromQuery] string? type)
    {
        var url = "api/transactions";
        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) queryParams.Add($"search={search}");
        if (!string.IsNullOrWhiteSpace(type)) queryParams.Add($"type={type}");
        
        if (queryParams.Any()) url += "?" + string.Join("&", queryParams);

        var response = await _httpClient.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.GetErrorMessageAsync();
            TempData["ErrorMessage"] = $"Failed to export transactions. {error}";
            return RedirectToAction(nameof(Index), new { search, type });
        }

        var transactions = await response.Content.ReadFromJsonAsync<List<TransactionViewModel>>() ?? new List<TransactionViewModel>();

        var builder = new System.Text.StringBuilder();
        builder.AppendLine("Date,Type,Product,SKU,Quantity,Source Warehouse,Destination Warehouse,Reference");

        foreach (var t in transactions)
        {
            var date = t.Date.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            var src = t.SourceWarehouseName ?? "-";
            var dst = t.DestinationWarehouseName ?? "-";
            builder.AppendLine($"{date},{t.Type},\"{t.ProductName}\",{t.SKU},{t.Quantity},\"{src}\",\"{dst}\",{t.Reference}");
        }

        return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "Transactions_Report.csv");
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateTransactionRequest model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Invalid transaction data.";
            return RedirectToAction(nameof(Index));
        }

        // Clean up unneeded warehouse IDs based on type
        if (model.Type == "In") model.SourceWarehouseId = null;
        if (model.Type == "Out") model.DestinationWarehouseId = null;

        var response = await _httpClient.PostAsJsonAsync("api/transactions", model);
        if (response.IsSuccessStatusCode)
        {
            var tx = await response.Content.ReadFromJsonAsync<TransactionViewModel>();
            if (tx != null)
            {
                var wh = tx.Type == "In" ? (tx.DestinationWarehouseName ?? "Facility") :
                         (tx.Type == "Out" ? (tx.SourceWarehouseName ?? "Facility") : $"{tx.SourceWarehouseName} -> {tx.DestinationWarehouseName}");
                await _notifier.NotifyTransactionAsync(tx.Type, tx.ProductName, tx.Quantity, wh, tx.Reference);
            }

            TempData["SuccessMessage"] = "Transaction created successfully!";
        }
        else
        {
            var error = await response.GetErrorMessageAsync();
            TempData["ErrorMessage"] = $"Failed to create transaction. {error}";
        }

        return RedirectToAction(nameof(Index));
    }
}

// ─── View Models ─────────────────────────────────────────────────────────────
public class TransactionsViewModel
{
    public string? SearchQuery { get; set; }
    public string? FilterType { get; set; }
    public List<TransactionViewModel> Transactions { get; set; } = new();
    public List<ProductViewModel> Products { get; set; } = new();
    public List<WarehouseViewModel> Warehouses { get; set; } = new();

    public int TotalStockIn => Transactions.Where(t => t.Type == "In").Sum(t => t.Quantity);
    public int TotalStockOut => Transactions.Where(t => t.Type == "Out").Sum(t => t.Quantity);
    public int TotalTransfers => Transactions.Where(t => t.Type == "Transfer").Sum(t => t.Quantity);
}

public class TransactionViewModel
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public Guid? SourceWarehouseId { get; set; }
    public string? SourceWarehouseName { get; set; }
    public Guid? DestinationWarehouseId { get; set; }
    public string? DestinationWarehouseName { get; set; }
    public int Quantity { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

public class CreateTransactionRequest
{
    public string Type { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public Guid? SourceWarehouseId { get; set; }
    public Guid? DestinationWarehouseId { get; set; }
    public int Quantity { get; set; }
    public string Reference { get; set; } = string.Empty;
}
