using Moq;
using Shouldly;
using SportFlow.Application.Abstractions;
using SportFlow.Application.Tenants.Queries;
using SportFlow.Domain.Shared.ValueObjects;
using SportFlow.Domain.Tenants;

namespace SportFlow.Application.Tests.Tenants;

public class GetTenantMeQueryHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepo = new();
    private readonly Mock<ITenantContext> _tenantContext = new();
    private readonly GetTenantMeQueryHandler _handler;

    private static readonly TenantId DemoTenantId =
        TenantId.From(Guid.Parse("00000000-0000-0000-0000-000000000001"));

    public GetTenantMeQueryHandlerTests()
    {
        _handler = new GetTenantMeQueryHandler(_tenantRepo.Object, _tenantContext.Object);
    }

    [Fact]
    public async Task Handle_ValidTenant_ReturnsTenantMeResponse()
    {
        var tenant = Tenant.Create("Demo Gym", "demo", "basic");
        _tenantContext.Setup(c => c.TenantId).Returns(DemoTenantId);
        _tenantRepo.Setup(r => r.GetByIdAsync(DemoTenantId, default)).ReturnsAsync(tenant);

        var result = await _handler.Handle(new GetTenantMeQuery());

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Name.ShouldBe("Demo Gym");
        result.Value.Slug.ShouldBe("demo");
        result.Value.Plan.ShouldBe("basic");
    }

    [Fact]
    public async Task Handle_TenantNotFound_ReturnsFailure()
    {
        _tenantContext.Setup(c => c.TenantId).Returns(DemoTenantId);
        _tenantRepo.Setup(r => r.GetByIdAsync(DemoTenantId, default)).ReturnsAsync((Tenant?)null);

        var result = await _handler.Handle(new GetTenantMeQuery());

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("TENANT_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_SuperAdminWithoutTenantContext_ReturnsFailure()
    {
        _tenantContext.Setup(c => c.TenantId).Returns((TenantId?)null);

        var result = await _handler.Handle(new GetTenantMeQuery());

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("NO_TENANT_CONTEXT");
        _tenantRepo.Verify(r => r.GetByIdAsync(It.IsAny<TenantId>(), default), Times.Never);
    }
}
