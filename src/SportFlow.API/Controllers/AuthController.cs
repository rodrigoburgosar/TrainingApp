using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportFlow.Application.Abstractions;
using SportFlow.Application.Identity.Commands;
using SportFlow.Application.Identity.DTOs;
using SportFlow.Application.Identity.Queries;

namespace SportFlow.API.Controllers;

[ApiController]
[Route("v1/auth")]
public class AuthController(
    ICommandHandler<LoginRequest, TokenResponse> loginHandler,
    ICommandHandler<RefreshTokenRequest, TokenResponse> refreshHandler,
    ICommandHandler<LogoutRequest> logoutHandler,
    ICommandHandler<ChangePasswordRequest> changePasswordHandler,
    ICommandHandler<ForgotPasswordRequest> forgotPasswordHandler,
    ICommandHandler<ResetPasswordRequest> resetPasswordHandler,
    IQueryHandler<GetMeQuery, MeResponse> getMeHandler) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await loginHandler.Handle(request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { result.ErrorCode, result.ErrorMessage });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await refreshHandler.Handle(request, ct);
        if (!result.IsSuccess)
            return Unauthorized(new { result.ErrorCode, result.ErrorMessage });
        return Ok(result.Value);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken ct)
    {
        await logoutHandler.Handle(request, ct);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var result = await getMeHandler.Handle(new GetMeQuery(), ct);
        return result.IsSuccess ? Ok(result.Value) : Unauthorized();
    }

    [HttpPatch("me/password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var result = await changePasswordHandler.Handle(request, ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { result.ErrorCode, result.ErrorMessage });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        await forgotPasswordHandler.Handle(request, ct);
        return NoContent();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var result = await resetPasswordHandler.Handle(request, ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { result.ErrorCode, result.ErrorMessage });
    }
}
