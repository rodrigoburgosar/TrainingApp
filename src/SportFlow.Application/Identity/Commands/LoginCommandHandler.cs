using Microsoft.AspNetCore.Identity;
using SportFlow.Application.Abstractions;
using SportFlow.Application.Identity.DTOs;
using SportFlow.Domain.Identity;
using SportFlow.Domain.Shared.ValueObjects;
using SportFlow.Domain.Tenants;
using SportFlow.Shared.Results;

namespace SportFlow.Application.Identity.Commands;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string hash, CancellationToken ct = default);
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task RevokeAsync(Guid tokenId, CancellationToken ct = default);
    Task RevokeAllForUserAsync(UserId userId, CancellationToken ct = default);
    Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(UserId userId, CancellationToken ct = default);
}

public class LoginCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ITenantRepository tenantRepository,
    IJwtService jwtService,
    IUnitOfWork unitOfWork) : ICommandHandler<LoginRequest, TokenResponse>
{
    private readonly PasswordHasher<User> _hasher = new();

    public async Task<Result<TokenResponse>> Handle(LoginRequest command, CancellationToken ct = default)
    {
        var tenant = await tenantRepository.GetBySlugAsync(command.TenantSlug, ct);
        if (tenant is null)
            return Result.Failure<TokenResponse>("TENANT_NOT_FOUND", $"Tenant '{command.TenantSlug}' not found.");

        if (tenant.Status is TenantStatus.Suspended or TenantStatus.Cancelled)
            return Result.Failure<TokenResponse>("TENANT_INACTIVE", "The tenant is not active.");

        var tenantId = tenant.Id;
        var user = await userRepository.GetByEmailAndTenantAsync(command.Identifier.ToLowerInvariant(), tenantId, ct);
        if (user is null)
            return Result.Failure<TokenResponse>("INVALID_CREDENTIALS", "Invalid credentials.");

        if (!user.IsActive)
            return Result.Failure<TokenResponse>("ACCOUNT_SUSPENDED", "Account is suspended.");

        var verificationResult = _hasher.VerifyHashedPassword(user, user.PasswordHash, command.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
            return Result.Failure<TokenResponse>("INVALID_CREDENTIALS", "Invalid credentials.");

        var tenantRole = await userRepository.GetUserTenantRoleAsync(user.Id, tenantId, ct);
        if (tenantRole is null)
            return Result.Failure<TokenResponse>("INVALID_CREDENTIALS", "Invalid credentials.");

        user.RecordLogin();

        var accessToken = jwtService.GenerateAccessToken(user, tenantId, tenant.Slug, tenantRole.Role);
        var rawRefreshToken = jwtService.GenerateRefreshToken();
        var tokenHash = jwtService.HashToken(rawRefreshToken);

        var refreshToken = RefreshToken.Create(
            user.Id, tenantId, tokenHash,
            DateTime.UtcNow.AddDays(30));

        await refreshTokenRepository.AddAsync(refreshToken, ct);
        await userRepository.UpdateAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var scopes = SystemRoles.GetScopesForRole(tenantRole.Role);
        var me = new MeResponse(
            user.Id.Value,
            user.Email,
            tenantRole.Role,
            scopes,
            new TenantRef(tenant.Id.Value, tenant.Name, tenant.Slug, tenant.Plan));

        return Result.Success(new TokenResponse(accessToken, rawRefreshToken, 900, "Bearer", me));
    }
}
