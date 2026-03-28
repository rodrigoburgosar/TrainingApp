using SportFlow.Application.Abstractions;

namespace SportFlow.Application.Identity.DTOs;

public record LoginRequest(
    string Identifier,
    string Password,
    string TenantSlug,
    Guid? LocationId = null) : ICommand<TokenResponse>;

public record RefreshTokenRequest(
    string RefreshToken) : ICommand<TokenResponse>;

public record LogoutRequest(
    string RefreshToken) : ICommand;

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword) : ICommand;

public record ForgotPasswordRequest(
    string Email,
    string TenantSlug) : ICommand;

public record ResetPasswordRequest(
    string Token,
    string NewPassword) : ICommand;
