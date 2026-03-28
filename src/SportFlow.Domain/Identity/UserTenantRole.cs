using SportFlow.Domain.Shared.ValueObjects;

namespace SportFlow.Domain.Identity;

public class UserTenantRole
{
    public Guid Id { get; private set; }
    public UserId UserId { get; private set; }
    public TenantId TenantId { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private UserTenantRole() { }

    public static UserTenantRole Create(UserId userId, TenantId tenantId, string role)
    {
        return new UserTenantRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Deactivate() => IsActive = false;
}
