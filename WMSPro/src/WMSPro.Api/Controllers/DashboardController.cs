using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMSPro.Application.Abstractions;
using WMSPro.Application.Dashboard.Queries;

namespace WMSPro.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    public DashboardController(IMediator mediator) => _mediator = mediator;

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetDashboardStatsQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(500, new { error = result.Error.Message });
    }

    [HttpGet("report-pdf")]
    public async Task<IActionResult> DownloadReportPdf(
        [FromServices] IPdfReportService pdfService, 
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetDashboardStatsQuery(), cancellationToken);
        if (!result.IsSuccess)
            return StatusCode(500, new { error = result.Error.Message });

        var pdfBytes = pdfService.GenerateExecutiveDashboardPdf(result.Value);
        return File(pdfBytes, "application/pdf", $"WMS_Executive_Report_{DateTime.UtcNow:yyyyMMdd_HHmm}.pdf");
    }
}
