using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using WMSPro.Application.Auth.DTOs;
using WMSPro.Mvc.Models.Account;

namespace WMSPro.Mvc.Controllers;

public class AccountController : Controller
{
    private readonly HttpClient _httpClient;

    public AccountController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("WmsApi");
    }

    // GET: /Account/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    // POST: /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return View(model);

        var request = new LoginRequest(model.Email, model.Password);
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            ModelState.AddModelError(string.Empty, error?.Error ?? "Invalid login attempt.");
            return View(model);
        }

        var authResult = await response.Content.ReadFromJsonAsync<AuthResponse>();
        if (authResult == null)
        {
            ModelState.AddModelError(string.Empty, "Unexpected error. Please try again.");
            return View(model);
        }

        // Standard Cookie Authentication SignIn
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.Name, authResult.FullName),
            new(System.Security.Claims.ClaimTypes.Email, authResult.Email),
            new(System.Security.Claims.ClaimTypes.Role, authResult.Role)
        };

        var claimsIdentity = new System.Security.Claims.ClaimsIdentity(claims, "Cookies");
        var authProperties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = authResult.ExpiresAt
        };

        await HttpContext.SignInAsync("Cookies", new System.Security.Claims.ClaimsPrincipal(claimsIdentity), authProperties);

        // Store JWT token in cookie for API calls
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = model.RememberMe
                ? DateTimeOffset.UtcNow.AddDays(7)
                : authResult.ExpiresAt
        };

        Response.Cookies.Append("jwt", authResult.Token, cookieOptions);
        Response.Cookies.Append("WmsUserName", authResult.FullName, new CookieOptions { HttpOnly = false });
        Response.Cookies.Append("WmsUserEmail", authResult.Email, new CookieOptions { HttpOnly = false });
        Response.Cookies.Append("WmsUserRole", authResult.Role, new CookieOptions { HttpOnly = false });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Dashboard");
    }

    // GET: /Account/Register
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        return View(new RegisterViewModel());
    }

    // POST: /Account/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (model.Password != model.ConfirmPassword)
        {
            ModelState.AddModelError(nameof(model.ConfirmPassword), "Passwords do not match.");
            return View(model);
        }

        if (!model.AgreeToTerms)
        {
            ModelState.AddModelError(nameof(model.AgreeToTerms), "You must agree to the Terms of Service.");
            return View(model);
        }

        var request = new RegisterRequest(model.FullName, model.Email, model.Password, model.Role);
        var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            ModelState.AddModelError(string.Empty, error?.Error ?? "Registration failed.");
            return View(model);
        }

        var authResult = await response.Content.ReadFromJsonAsync<AuthResponse>();
        if (authResult == null)
        {
            ModelState.AddModelError(string.Empty, "Unexpected error. Please try again.");
            return View(model);
        }

        // Standard Cookie Authentication SignIn
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.Name, authResult.FullName),
            new(System.Security.Claims.ClaimTypes.Email, authResult.Email),
            new(System.Security.Claims.ClaimTypes.Role, authResult.Role)
        };

        var claimsIdentity = new System.Security.Claims.ClaimsIdentity(claims, "Cookies");
        var authProperties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = authResult.ExpiresAt
        };

        await HttpContext.SignInAsync("Cookies", new System.Security.Claims.ClaimsPrincipal(claimsIdentity), authProperties);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true, Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = authResult.ExpiresAt
        };
        Response.Cookies.Append("jwt", authResult.Token, cookieOptions);
        Response.Cookies.Append("WmsUserName", authResult.FullName, new CookieOptions { HttpOnly = false });
        Response.Cookies.Append("WmsUserEmail", authResult.Email, new CookieOptions { HttpOnly = false });
        Response.Cookies.Append("WmsUserRole", authResult.Role, new CookieOptions { HttpOnly = false });

        return RedirectToAction("Index", "Dashboard");
    }

    // POST: /Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("Cookies");
        Response.Cookies.Delete("jwt");
        Response.Cookies.Delete("WmsUserName");
        Response.Cookies.Delete("WmsUserEmail");
        Response.Cookies.Delete("WmsUserRole");
        return RedirectToAction("Login");
    }

    // Helper error response model
    private record ErrorResponse(string Error);
}
