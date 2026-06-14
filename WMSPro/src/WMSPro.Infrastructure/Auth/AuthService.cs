using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WMSPro.Application.Auth;
using WMSPro.Application.Auth.DTOs;
using WMSPro.Domain.Common;
using WMSPro.Infrastructure.Persistence;

namespace WMSPro.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IOptions<JwtSettings> jwtSettingsOptions)
    {
        _userManager = userManager;
        _jwtSettings = jwtSettingsOptions.Value;
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Result.Failure<AuthResponse>(Error.Validation("Credentials", "Invalid email or password."));
        }

        if (!user.IsActive)
        {
            return Result.Failure<AuthResponse>(Error.Validation("Account", "Account is inactive."));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var mainRole = roles.FirstOrDefault() ?? string.Empty;

        var token = GenerateJwtToken(user, roles);

        var response = new AuthResponse(
            token,
            user.Email!,
            user.FullName,
            mainRole,
            DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes));

        return Result.Success(response);
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return Result.Failure<AuthResponse>(Error.Conflict("Email is already in use."));
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure<AuthResponse>(Error.Validation("Registration", errors));
        }

        // Validate role against enum logic or known string values
        var roleToAdd = string.IsNullOrWhiteSpace(request.Role) ? "WarehouseEmployee" : request.Role;
        await _userManager.AddToRoleAsync(user, roleToAdd);

        var token = GenerateJwtToken(user, new[] { roleToAdd });

        var response = new AuthResponse(
            token,
            user.Email!,
            user.FullName,
            roleToAdd,
            DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes));

        return Result.Success(response);
    }

    private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("FullName", user.FullName)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
