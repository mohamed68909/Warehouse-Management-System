using WMSPro.Application.Dashboard.Queries;
using WMSPro.Domain.Common;

namespace WMSPro.Application.Abstractions;

/// <summary>
/// Provides aggregated dashboard statistics.
/// Implemented in Infrastructure with direct EF Core queries for efficiency.
/// </summary>
public interface IDashboardService
{
    Task<Result<DashboardStatsDto>> GetStatsAsync(CancellationToken cancellationToken = default);
}
