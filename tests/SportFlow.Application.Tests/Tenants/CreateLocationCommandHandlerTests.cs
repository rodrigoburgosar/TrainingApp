using Moq;
using Shouldly;
using SportFlow.Application.Abstractions;
using SportFlow.Application.Tenants.Commands;
using SportFlow.Domain.Shared.ValueObjects;
using SportFlow.Domain.Tenants;

namespace SportFlow.Application.Tests.Tenants;

public class CreateLocationCommandHandlerTests
{
    private readonly Mock<ILocationRepository> _locationRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITenantContext> _tenantContext = new();
    private readonly CreateLocationCommandHandler _handler;

    private static readonly TenantId DemoTenantId =
        TenantId.From(Guid.Parse("00000000-0000-0000-0000-000000000001"));

    public CreateLocationCommandHandlerTests()
    {
        _handler = new CreateLocationCommandHandler(
            _locationRepo.Object, _unitOfWork.Object, _tenantContext.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesAndReturnsLocation()
    {
        _tenantContext.Setup(c => c.TenantId).Returns(DemoTenantId);

        var command = new CreateLocationCommand("Main Hall", "123 Main St", "America/New_York", 100);
        var result = await _handler.Handle(command);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Name.ShouldBe("Main Hall");
        result.Value.Address.ShouldBe("123 Main St");
        result.Value.Timezone.ShouldBe("America/New_York");
        result.Value.MaxCapacity.ShouldBe(100);
        result.Value.IsActive.ShouldBeTrue();

        _locationRepo.Verify(r => r.AddAsync(It.IsAny<Location>(), default), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_NoTenantContext_ReturnsFailure()
    {
        _tenantContext.Setup(c => c.TenantId).Returns((TenantId?)null);

        var result = await _handler.Handle(new CreateLocationCommand("Hall", null, "UTC", null));

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("NO_TENANT_CONTEXT");
        _locationRepo.Verify(r => r.AddAsync(It.IsAny<Location>(), default), Times.Never);
    }
}
