using SportFlow.Domain.Shared.ValueObjects;

namespace SportFlow.Domain.Identity;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByEmailAndTenantAsync(string email, TenantId tenantId, CancellationToken ct = default);
    Task<UserTenantRole?> GetUserTenantRoleAsync(UserId userId, TenantId tenantId, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task AddUserTenantRoleAsync(UserTenantRole role, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
}
