namespace SportFlow.Application.Identity.DTOs;

public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType,
    MeResponse Me);

public record MeResponse(
    Guid UserId,
    string Email,
    string Role,
    string[] Scopes,
    TenantRef? Tenant);

public record TenantRef(
    Guid Id,
    string Name,
    string Slug,
    string Plan);

public record SessionResponse(
    Guid Id,
    string? IpAddress,
    string? UserAgent,
    DateTime CreatedAt,
    DateTime ExpiresAt);
