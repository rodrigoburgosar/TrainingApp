using SportFlow.Application.Abstractions;
using SportFlow.Application.Tenants.DTOs;
using SportFlow.Domain.Tenants;
using SportFlow.Shared.Results;

namespace SportFlow.Application.Tenants.Queries;

public record GetLocationsQuery : IQuery<IReadOnlyList<LocationResponse>>;

public class GetLocationsQueryHandler(
    ILocationRepository locationRepository,
    ITenantContext tenantContext) : IQueryHandler<GetLocationsQuery, IReadOnlyList<LocationResponse>>
{
    public async Task<Result<IReadOnlyList<LocationResponse>>> Handle(GetLocationsQuery query, CancellationToken ct = default)
    {
        if (!tenantContext.TenantId.HasValue)
            return Result.Failure<IReadOnlyList<LocationResponse>>("NO_TENANT_CONTEXT", "This endpoint requires a tenant context.");

        var locations = await locationRepository.GetByTenantAsync(tenantContext.TenantId.Value, ct);

        var response = locations.Select(l => new LocationResponse(
            l.Id.Value, l.Name, l.Address, l.Timezone,
            l.MaxCapacity, l.IsActive, l.CreatedAt, l.UpdatedAt))
            .ToList();

        return Result.Success<IReadOnlyList<LocationResponse>>(response);
    }
}

public record GetLocationByIdQuery(Guid Id) : IQuery<LocationResponse>;

public class GetLocationByIdQueryHandler(
    ILocationRepository locationRepository,
    ITenantContext tenantContext) : IQueryHandler<GetLocationByIdQuery, LocationResponse>
{
    public async Task<Result<LocationResponse>> Handle(GetLocationByIdQuery query, CancellationToken ct = default)
    {
        if (!tenantContext.TenantId.HasValue)
            return Result.Failure<LocationResponse>("NO_TENANT_CONTEXT", "This endpoint requires a tenant context.");

        var location = await locationRepository.GetByIdAsync(
            Domain.Shared.ValueObjects.LocationId.From(query.Id), ct);

        if (location is null || location.TenantId != tenantContext.TenantId.Value)
            return Result.Failure<LocationResponse>("NOT_FOUND", "Location not found.");

        return Result.Success(new LocationResponse(
            location.Id.Value, location.Name, location.Address, location.Timezone,
            location.MaxCapacity, location.IsActive, location.CreatedAt, location.UpdatedAt));
    }
}
