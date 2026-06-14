using MediatR;
using WMSPro.Application.Abstractions;
using WMSPro.Application.Dashboard.Queries;
using WMSPro.Domain.Common;

namespace WMSPro.Application.Dashboard.Handlers;

public class GetDashboardStatsHandler : IRequestHandler<GetDashboardStatsQuery, Result<DashboardStatsDto>>
{
    private readonly IDashboardService _dashboardService;

    public GetDashboardStatsHandler(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public Task<Result<DashboardStatsDto>> Handle(
        GetDashboardStatsQuery request, CancellationToken cancellationToken) =>
        _dashboardService.GetStatsAsync(cancellationToken);
}
