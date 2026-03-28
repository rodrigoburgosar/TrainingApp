using Moq;
using Shouldly;
using SportFlow.Application.Abstractions;
using SportFlow.Application.Tenants.Commands;
using SportFlow.Domain.Shared.ValueObjects;
using SportFlow.Domain.Tenants;

namespace SportFlow.Application.Tests.Tenants;

public class DeactivateLocationCommandHandlerTests
{
    private readonly Mock<ILocationRepository> _locationRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITenantContext> _tenantContext = new();
    private readonly DeactivateLocationCommandHandler _handler;

    private static readonly TenantId DemoTenantId =
        TenantId.From(Guid.Parse("00000000-0000-0000-0000-000000000001"));

    public DeactivateLocationCommandHandlerTests()
    {
        _handler = new DeactivateLocationCommandHandler(
            _locationRepo.Object, _unitOfWork.Object, _tenantContext.Object);
    }

    [Fact]
    public async Task Handle_ExistingLocation_DeactivatesAndPersists()
    {
        var location = Location.Create(DemoTenantId, "Sala A", "UTC");
        _tenantContext.Setup(c => c.TenantId).Returns(DemoTenantId);
        _locationRepo.Setup(r => r.GetByIdAsync(LocationId.From(location.Id.Value), default))
            .ReturnsAsync(location);

        var result = await _handler.Handle(new DeactivateLocationCommand(location.Id.Value));

        result.IsSuccess.ShouldBeTrue();
        location.IsActive.ShouldBeFalse();
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

        var result = await _handler.Handle(new DeactivateLocationCommand(missingId));

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("NOT_FOUND");
        _unitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }
}
