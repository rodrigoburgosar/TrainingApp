using Microsoft.EntityFrameworkCore;
using SportFlow.Application.Identity.Commands;
using SportFlow.Domain.Identity;
using SportFlow.Domain.Shared.ValueObjects;

namespace SportFlow.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository(SportFlowDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(string hash, CancellationToken ct = default)
        => db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
        => await db.RefreshTokens.AddAsync(token, ct);

    public async Task RevokeAsync(Guid tokenId, CancellationToken ct = default)
    {
        var token = await db.RefreshTokens.FindAsync([tokenId], ct);
        token?.Revoke();
    }

    public async Task RevokeAllForUserAsync(UserId userId, CancellationToken ct = default)
    {
        var tokens = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);

        foreach (var token in tokens)
            token.Revoke();
    }

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(UserId userId, CancellationToken ct = default)
    {
        return await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }
}
