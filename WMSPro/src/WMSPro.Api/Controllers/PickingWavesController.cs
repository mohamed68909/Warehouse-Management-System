using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMSPro.Application.Abstractions;
using WMSPro.Application.Picking.Commands;

namespace WMSPro.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/picking-waves")]
public class PickingWavesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPdfReportService _pdfService;

    public PickingWavesController(IMediator mediator, IPdfReportService pdfService)
    {
        _mediator = mediator;
        _pdfService = pdfService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? warehouseId, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPickingWavesQuery(warehouseId, status), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Message });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPickingWaveByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error.Message });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePickingWaveCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : BadRequest(new { error = result.Error.Message });
    }

    [HttpPost("{id:guid}/items/{itemId:guid}/confirm")]
    public async Task<IActionResult> ConfirmItemPick(
        Guid id,
        Guid itemId,
        [FromBody] ConfirmPickItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ConfirmPickItemCommand(id, itemId, request.QuantityPicked), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Message });
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelPickingWaveCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error.Message });
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPickingWaveByIdQuery(id), cancellationToken);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error.Message });

        var pdfBytes = _pdfService.GeneratePickListPdf(result.Value);
        return File(pdfBytes, "application/pdf", $"PickList_{result.Value.WaveNumber}.pdf");
    }
}

public record ConfirmPickItemRequest(int QuantityPicked);
