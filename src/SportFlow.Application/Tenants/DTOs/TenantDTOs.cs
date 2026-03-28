namespace SportFlow.Application.Tenants.DTOs;

public record TenantMeResponse(
    Guid Id,
    string Name,
    string Slug,
    string Status,
    string Plan,
    DateTime CreatedAt
);

public record LocationResponse(
    Guid Id,
    string Name,
    string? Address,
    string Timezone,
    int? MaxCapacity,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateLocationRequest(
    string Name,
    string? Address,
    string Timezone,
    int? MaxCapacity
);

public record UpdateLocationRequest(
    string? Name,
    string? Address,
    string? Timezone,
    int? MaxCapacity
);
