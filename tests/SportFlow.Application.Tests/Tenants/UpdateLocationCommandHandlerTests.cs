using Moq;
using Shouldly;
using SportFlow.Application.Abstractions;
using SportFlow.Application.Tenants.Commands;
using SportFlow.Domain.Shared.ValueObjects;
using SportFlow.Domain.Tenants;

namespace SportFlow.Application.Tests.Tenants;

public class UpdateLocationCommandHandlerTests
{
    private readonly Mock<ILocationRepository> _locationRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITenantContext> _tenantContext = new();
    private readonly UpdateLocationCommandHandler _handler;

    private static readonly TenantId DemoTenantId =
        TenantId.From(Guid.Parse("00000000-0000-0000-0000-000000000001"));

    public UpdateLocationCommandHandlerTests()
    {
        _handler = new UpdateLocationCommandHandler(
            _locationRepo.Object, _unitOfWork.Object, _tenantContext.Object);
    }

    private static Location CreateLocation(TenantId tenantId, string name = "Original")
        => Location.Create(tenantId, name, "UTC", "Old Address", 50);

    [Fact]
    public async Task Handle_PartialUpdate_AppliesOnlyProvidedFields()
    {
        var location = CreateLocation(DemoTenantId);
        _tenantContext.Setup(c => c.TenantId).Returns(DemoTenantId);
        _locationRepo.Setup(r => r.GetByIdAsync(LocationId.From(location.Id.Value), default))
            .ReturnsAsync(location);

        var command = new UpdateLocationCommand(location.Id.Value, "Updated Name", null, null, null);
        var result = await _handler.Handle(command);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Name.ShouldBe("Updated Name");
        result.Value.Address.ShouldBe("Old Address"); // unchanged
        result.Value.Timezone.ShouldBe("UTC"); // unchanged

        _locationRepo.Verify(r => r.UpdateAsync(location, default), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_LocationNotFound_ReturnsFailure()
    {
        var missingId = Guid.NewGuid();
        _tenantContext.Setup(c => c.TenantId).Returns(DemoTenantId);
        _locationRepo.Setup(r => r.GetByIdAsync(LocationId.From(missingId), default))
            .ReturnsAsync((Location?)null);

        var result = await _handler.Handle(new UpdateLocationCommand(missingId, "New Name", null, null, null));

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("NOT_FOUND");
    }

    [Fact]
    public async Task Handle_LocationBelongsToDifferentTenant_ReturnsFailure()
    {
        var otherTenantId = TenantId.New();
        var location = CreateLocation(otherTenantId);
        _tenantContext.Setup(c => c.TenantId).Returns(DemoTenantId);
        _locationRepo.Setup(r => r.GetByIdAsync(LocationId.From(location.Id.Value), default))
            .ReturnsAsync(location);

        var result = await _handler.Handle(new UpdateLocationCommand(location.Id.Value, "Hijack", null, null, null));

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("NOT_FOUND");
    }
}
