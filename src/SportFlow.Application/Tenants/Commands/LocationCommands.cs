using SportFlow.Application.Abstractions;
using SportFlow.Application.Tenants.DTOs;
using SportFlow.Domain.Shared.ValueObjects;
using SportFlow.Domain.Tenants;
using SportFlow.Shared.Results;

namespace SportFlow.Application.Tenants.Commands;

// ── Create ──────────────────────────────────────────────────────────────────

public record CreateLocationCommand(
    string Name,
    string? Address,
    string Timezone,
    int? MaxCapacity) : ICommand<LocationResponse>;

public class CreateLocationCommandHandler(
    ILocationRepository locationRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext) : ICommandHandler<CreateLocationCommand, LocationResponse>
{
    public async Task<Result<LocationResponse>> Handle(CreateLocationCommand command, CancellationToken ct = default)
    {
        if (!tenantContext.TenantId.HasValue)
            return Result.Failure<LocationResponse>("NO_TENANT_CONTEXT", "This endpoint requires a tenant context.");

        var location = Location.Create(
            tenantContext.TenantId.Value,
            command.Name,
            command.Timezone,
            command.Address,
            command.MaxCapacity);

        await locationRepository.AddAsync(location, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(new LocationResponse(
            location.Id.Value, location.Name, location.Address, location.Timezone,
            location.MaxCapacity, location.IsActive, location.CreatedAt, location.UpdatedAt));
    }
}

// ── Update ──────────────────────────────────────────────────────────────────

public record UpdateLocationCommand(
    Guid Id,
    string? Name,
    string? Address,
    string? Timezone,
    int? MaxCapacity) : ICommand<LocationResponse>;

public class UpdateLocationCommandHandler(
    ILocationRepository locationRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext) : ICommandHandler<UpdateLocationCommand, LocationResponse>
{
    public async Task<Result<LocationResponse>> Handle(UpdateLocationCommand command, CancellationToken ct = default)
    {
        if (!tenantContext.TenantId.HasValue)
            return Result.Failure<LocationResponse>("NO_TENANT_CONTEXT", "This endpoint requires a tenant context.");

        var location = await locationRepository.GetByIdAsync(LocationId.From(command.Id), ct);
        if (location is null || location.TenantId != tenantContext.TenantId.Value)
            return Result.Failure<LocationResponse>("NOT_FOUND", "Location not found.");

        location.Update(command.Name, command.Address, command.Timezone, command.MaxCapacity);
        await locationRepository.UpdateAsync(location, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(new LocationResponse(
            location.Id.Value, location.Name, location.Address, location.Timezone,
            location.MaxCapacity, location.IsActive, location.CreatedAt, location.UpdatedAt));
    }
}

// ── Deactivate ───────────────────────────────────────────────────────────────

public record DeactivateLocationCommand(Guid Id) : ICommand;

public class DeactivateLocationCommandHandler(
    ILocationRepository locationRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext) : ICommandHandler<DeactivateLocationCommand>
{
    public async Task<Result> Handle(DeactivateLocationCommand command, CancellationToken ct = default)
    {
        if (!tenantContext.TenantId.HasValue)
            return Result.Failure("NO_TENANT_CONTEXT", "This endpoint requires a tenant context.");

        var location = await locationRepository.GetByIdAsync(LocationId.From(command.Id), ct);
        if (location is null || location.TenantId != tenantContext.TenantId.Value)
            return Result.Failure("NOT_FOUND", "Location not found.");

        location.Deactivate();
        await locationRepository.UpdateAsync(location, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
