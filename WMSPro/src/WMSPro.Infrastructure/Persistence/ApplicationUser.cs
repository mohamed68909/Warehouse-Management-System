using Microsoft.AspNetCore.Identity;

namespace WMSPro.Infrastructure.Persistence;

/// <summary>
/// Extends IdentityUser with custom WMS properties.
/// Keeps Identity concerns in Infrastructure, away from Domain.
/// </summary>
public sealed class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
