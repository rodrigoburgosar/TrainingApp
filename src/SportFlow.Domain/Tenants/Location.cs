using SportFlow.Domain.Shared.ValueObjects;

namespace SportFlow.Domain.Tenants;

public class Location
{
    public LocationId Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Address { get; private set; }
    public string Timezone { get; private set; } = string.Empty;
    public int? MaxCapacity { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Location() { }

    public static Location Create(TenantId tenantId, string name, string timezone, string? address = null, int? maxCapacity = null)
    {
        return new Location
        {
            Id = LocationId.New(),
            TenantId = tenantId,
            Name = name,
            Timezone = timezone,
            Address = address,
            MaxCapacity = maxCapacity,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string? name, string? address, string? timezone, int? maxCapacity)
    {
        if (name is not null) Name = name;
        if (address is not null) Address = address;
        if (timezone is not null) Timezone = timezone;
        if (maxCapacity is not null) MaxCapacity = maxCapacity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
