namespace WMSPro.Application.Auth.DTOs;

public record LoginRequest(string Email, string Password);

public record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    string Role);

public record AuthResponse(
    string Token,
    string Email,
    string FullName,
    string Role,
    DateTime ExpiresAt);
