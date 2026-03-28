using SportFlow.Domain.Shared.ValueObjects;

namespace SportFlow.Domain.Tenants;

public class Tenant
{
    public TenantId Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Status { get; private set; } = TenantStatus.Active;
    public string Plan { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Tenant() { }

    public static Tenant Create(string name, string slug, string plan)
    {
        return new Tenant
        {
            Id = TenantId.New(),
            Name = name,
            Slug = slug.ToLowerInvariant(),
            Status = TenantStatus.Active,
            Plan = plan,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Suspend()
    {
        Status = TenantStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
    }
}

public static class TenantStatus
{
    public const string Active = "active";
    public const string Suspended = "suspended";
    public const string Cancelled = "cancelled";
}
