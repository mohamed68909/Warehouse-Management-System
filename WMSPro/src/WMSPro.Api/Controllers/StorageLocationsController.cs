using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMSPro.Application.StorageLocations.Commands;

namespace WMSPro.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class StorageLocationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StorageLocationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetByWarehouse(
        [FromQuery] Guid warehouseId,
        [FromQuery] string? zone,
        CancellationToken cancellationToken)
    {
        if (warehouseId == Guid.Empty)
            return BadRequest(new { error = "warehouseId query parameter is required." });

        var result = await _mediator.Send(new GetStorageLocationsQuery(warehouseId, zone), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Message });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetStorageLocationByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error.Message });
    }

    [HttpGet("summary/{warehouseId:guid}")]
    public async Task<IActionResult> GetSummary(Guid warehouseId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetWarehouseSpatialSummaryQuery(warehouseId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Message });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateStorageLocationCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : BadRequest(new { error = result.Error.Message });
    }

    [HttpPost("assign")]
    public async Task<IActionResult> Assign(
        [FromBody] AssignBinProductCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(new { success = true }) : BadRequest(new { error = result.Error.Message });
    }

    [HttpPost("{id:guid}/empty")]
    public async Task<IActionResult> EmptyBin(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new EmptyBinCommand(id), cancellationToken);
        return result.IsSuccess ? Ok(new { success = true }) : BadRequest(new { error = result.Error.Message });
    }
}
