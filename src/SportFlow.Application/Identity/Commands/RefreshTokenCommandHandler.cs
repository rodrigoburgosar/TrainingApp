using SportFlow.Application.Abstractions;
using SportFlow.Application.Identity.Commands;
using SportFlow.Application.Identity.DTOs;
using SportFlow.Domain.Identity;
using SportFlow.Domain.Shared.ValueObjects;
using SportFlow.Shared.Results;

namespace SportFlow.Application.Identity.Commands;

public class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    ITenantRepository tenantRepository,
    IJwtService jwtService,
    IUnitOfWork unitOfWork) : ICommandHandler<RefreshTokenRequest, TokenResponse>
{
    public async Task<Result<TokenResponse>> Handle(RefreshTokenRequest command, CancellationToken ct = default)
    {
        var tokenHash = jwtService.HashToken(command.RefreshToken);
        var existing = await refreshTokenRepository.GetByHashAsync(tokenHash, ct);

        if (existing is null || existing.IsExpired)
            return Result.Failure<TokenResponse>("REFRESH_EXPIRED", "Refresh token is expired or invalid.");

        if (existing.IsRevoked)
        {
            // Token comprometido: revocar toda la familia
            await refreshTokenRepository.RevokeAllForUserAsync(existing.UserId, ct);
            await unitOfWork.SaveChangesAsync(ct);
            return Result.Failure<TokenResponse>("REFRESH_EXPIRED", "Refresh token has been compromised.");
        }

        var user = await userRepository.GetByIdAsync(existing.UserId, ct);
        if (user is null || !user.IsActive)
            return Result.Failure<TokenResponse>("REFRESH_EXPIRED", "User not found or inactive.");

        // Obtener rol del tenant
        string role = SystemRoles.Member;
        TenantInfo? tenant = null;
        TenantId? tenantId = existing.TenantId;

        if (tenantId.HasValue)
        {
            var tenantRole = await userRepository.GetUserTenantRoleAsync(user.Id, tenantId.Value, ct);
            role = tenantRole?.Role ?? SystemRoles.Member;
            // Nota: en una implementación completa obtendríamos el tenant completo
        }

        // Rotation: revocar el anterior, emitir nuevo
        await refreshTokenRepository.RevokeAsync(existing.Id, ct);

        var rawNewToken = jwtService.GenerateRefreshToken();
        var newHash = jwtService.HashToken(rawNewToken);
        var newRefreshToken = RefreshToken.Create(
            user.Id, tenantId, newHash,
            DateTime.UtcNow.AddDays(30));

        await refreshTokenRepository.AddAsync(newRefreshToken, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var accessToken = jwtService.GenerateAccessToken(user, tenantId, null, role);
        var scopes = SystemRoles.GetScopesForRole(role);

        var me = new MeResponse(user.Id.Value, user.Email, role, scopes, null);
        return Result.Success(new TokenResponse(accessToken, rawNewToken, 900, "Bearer", me));
    }
}
