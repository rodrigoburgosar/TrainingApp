using Microsoft.EntityFrameworkCore;
using SportFlow.Domain.Identity;
using SportFlow.Domain.Shared.ValueObjects;

namespace SportFlow.Infrastructure.Persistence.Repositories;

public class UserRepository(SportFlowDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default)
        => db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task<User?> GetByEmailAndTenantAsync(string email, TenantId tenantId, CancellationToken ct = default)
    {
        var normalizedEmail = email.ToLowerInvariant();
        return await db.Users
            .Where(u => u.Email == normalizedEmail)
            .Where(u => db.UserTenantRoles
                .Any(r => r.UserId == u.Id && r.TenantId == tenantId && r.IsActive))
            .FirstOrDefaultAsync(ct);
    }

    public Task<UserTenantRole?> GetUserTenantRoleAsync(UserId userId, TenantId tenantId, CancellationToken ct = default)
        => db.UserTenantRoles.FirstOrDefaultAsync(
            r => r.UserId == userId && r.TenantId == tenantId && r.IsActive, ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
        => await db.Users.AddAsync(user, ct);

    public async Task AddUserTenantRoleAsync(UserTenantRole role, CancellationToken ct = default)
        => await db.UserTenantRoles.AddAsync(role, ct);

    public Task UpdateAsync(User user, CancellationToken ct = default)
    {
        db.Users.Update(user);
        return Task.CompletedTask;
    }
}
