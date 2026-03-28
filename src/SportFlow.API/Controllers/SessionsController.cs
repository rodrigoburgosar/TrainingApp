using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportFlow.Application.Abstractions;
using SportFlow.Application.Identity.Commands;
using SportFlow.Application.Identity.DTOs;
using SportFlow.Application.Identity.Queries;

namespace SportFlow.API.Controllers;

[ApiController]
[Route("v1/auth/me/sessions")]
[Authorize]
public class SessionsController(
    IQueryHandler<GetSessionsQuery, IReadOnlyList<SessionResponse>> getSessionsHandler,
    ICommandHandler<RevokeSessionCommand> revokeSessionHandler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SessionResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessions(CancellationToken ct)
    {
        var result = await getSessionsHandler.Handle(new GetSessionsQuery(), ct);
        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeSession(Guid id, CancellationToken ct)
    {
        var result = await revokeSessionHandler.Handle(new RevokeSessionCommand(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { result.ErrorCode, result.ErrorMessage });
        return NoContent();
    }
}
