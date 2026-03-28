using SportFlow.Application.Abstractions;
using SportFlow.Domain.Shared.ValueObjects;

namespace SportFlow.Infrastructure.Services;

public class TenantContext : ITenantContext
{
    public TenantId? TenantId { get; private set; }
    public string? TenantSlug { get; private set; }
    public UserId UserId { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public bool IsSuperAdmin => Role == Domain.Identity.SystemRoles.SuperAdmin;

    public void Initialize(UserId userId, string role, TenantId? tenantId = null, string? tenantSlug = null)
    {
        UserId = userId;
        Role = role;
        TenantId = tenantId;
        TenantSlug = tenantSlug;
    }
}
