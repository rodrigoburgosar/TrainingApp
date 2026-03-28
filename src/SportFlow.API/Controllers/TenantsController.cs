using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportFlow.Application.Abstractions;
using SportFlow.Application.Tenants.Commands;
using SportFlow.Application.Tenants.DTOs;
using SportFlow.Application.Tenants.Queries;

namespace SportFlow.API.Controllers;

[ApiController]
[Route("v1/tenants")]
[Authorize]
public class TenantsController(
    IQueryHandler<GetTenantMeQuery, TenantMeResponse> getTenantMeHandler,
    IQueryHandler<GetLocationsQuery, IReadOnlyList<LocationResponse>> getLocationsHandler,
    IQueryHandler<GetLocationByIdQuery, LocationResponse> getLocationByIdHandler,
    ICommandHandler<CreateLocationCommand, LocationResponse> createLocationHandler,
    ICommandHandler<UpdateLocationCommand, LocationResponse> updateLocationHandler,
    ICommandHandler<DeactivateLocationCommand> deactivateLocationHandler) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType<TenantMeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantMe(CancellationToken ct)
    {
        var result = await getTenantMeHandler.Handle(new GetTenantMeQuery(), ct);
        if (!result.IsSuccess)
            return result.ErrorCode == "TENANT_NOT_FOUND"
                ? NotFound(new { result.ErrorCode, result.ErrorMessage })
                : Forbid();
        return Ok(result.Value);
    }

    [HttpGet("me/locations")]
    [ProducesResponseType<IReadOnlyList<LocationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLocations(CancellationToken ct)
    {
        var result = await getLocationsHandler.Handle(new GetLocationsQuery(), ct);
        if (!result.IsSuccess) return Forbid();
        return Ok(result.Value);
    }

    [HttpGet("me/locations/{id:guid}")]
    [ProducesResponseType<LocationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLocationById(Guid id, CancellationToken ct)
    {
        var result = await getLocationByIdHandler.Handle(new GetLocationByIdQuery(id), ct);
        if (!result.IsSuccess)
            return result.ErrorCode == "NOT_FOUND"
                ? NotFound(new { result.ErrorCode, result.ErrorMessage })
                : Forbid();
        return Ok(result.Value);
    }

    [HttpPost("me/locations")]
    [Authorize(Policy = "RequireTenantManagerOrAbove")]
    [ProducesResponseType<LocationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateLocation([FromBody] CreateLocationRequest request, CancellationToken ct)
    {
        var command = new CreateLocationCommand(request.Name, request.Address, request.Timezone, request.MaxCapacity);
        var result = await createLocationHandler.Handle(command, ct);
        if (!result.IsSuccess)
            return result.ErrorCode == "NO_TENANT_CONTEXT"
                ? Forbid()
                : BadRequest(new { result.ErrorCode, result.ErrorMessage });
        return CreatedAtAction(nameof(GetLocationById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPatch("me/locations/{id:guid}")]
    [Authorize(Policy = "RequireTenantManagerOrAbove")]
    [ProducesResponseType<LocationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLocation(Guid id, [FromBody] UpdateLocationRequest request, CancellationToken ct)
    {
        var command = new UpdateLocationCommand(id, request.Name, request.Address, request.Timezone, request.MaxCapacity);
        var result = await updateLocationHandler.Handle(command, ct);
        if (!result.IsSuccess)
            return result.ErrorCode switch
            {
                "NOT_FOUND" => NotFound(new { result.ErrorCode, result.ErrorMessage }),
                "NO_TENANT_CONTEXT" => Forbid(),
                _ => BadRequest(new { result.ErrorCode, result.ErrorMessage })
            };
        return Ok(result.Value);
    }

    [HttpDelete("me/locations/{id:guid}")]
    [Authorize(Policy = "RequireTenantManagerOrAbove")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateLocation(Guid id, CancellationToken ct)
    {
        var result = await deactivateLocationHandler.Handle(new DeactivateLocationCommand(id), ct);
        if (!result.IsSuccess)
            return result.ErrorCode switch
            {
                "NOT_FOUND" => NotFound(new { result.ErrorCode, result.ErrorMessage }),
                _ => Forbid()
            };
        return NoContent();
    }
}
