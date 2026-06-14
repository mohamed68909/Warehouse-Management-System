using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using WMSPro.Mvc.Extensions;

namespace WMSPro.Mvc.Controllers;

[Authorize]
public class ProductsController : Controller
{
    private readonly HttpClient _httpClient;

    public ProductsController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("WmsApi");
    }

    public async Task<IActionResult> Index([FromQuery] string? search)
    {
        var url = string.IsNullOrWhiteSpace(search) ? "api/products" : $"api/products?search={search}";
        var response = await _httpClient.GetAsync(url);
        
        if (response.IsSuccessStatusCode)
        {
            var products = await response.Content.ReadFromJsonAsync<List<ProductViewModel>>();
            ViewData["SearchQuery"] = search;
            return View(products ?? new List<ProductViewModel>());
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            return RedirectToAction("Login", "Account");

        TempData["ErrorMessage"] = "Failed to load products.";
        return View(new List<ProductViewModel>());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateProductViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Invalid product data.";
            return RedirectToAction(nameof(Index));
        }

        var response = await _httpClient.PostAsJsonAsync("api/products", model);
        if (response.IsSuccessStatusCode)
        {
            TempData["SuccessMessage"] = "Product created successfully!";
        }
        else
        {
            var error = await response.GetErrorMessageAsync();
            TempData["ErrorMessage"] = $"Failed to create product. {error}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Edit([FromForm] UpdateProductViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Invalid product data.";
            return RedirectToAction(nameof(Index));
        }

        var response = await _httpClient.PutAsJsonAsync($"api/products/{model.Id}", model);
        if (response.IsSuccessStatusCode)
        {
            TempData["SuccessMessage"] = "Product updated successfully!";
        }
        else
        {
            var error = await response.GetErrorMessageAsync();
            TempData["ErrorMessage"] = $"Failed to update product. {error}";
        }

        return RedirectToAction(nameof(Index));
    }
}

// ─── View Models ─────────────────────────────────────────────────────────────
public class ProductViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class CreateProductViewModel
{
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int InitialQuantity { get; set; }
}

public class UpdateProductViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
