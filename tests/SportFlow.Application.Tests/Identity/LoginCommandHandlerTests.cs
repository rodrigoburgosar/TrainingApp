using Microsoft.AspNetCore.Identity;
using Moq;
using Shouldly;
using SportFlow.Application.Abstractions;
using SportFlow.Application.Identity.Commands;
using SportFlow.Application.Identity.DTOs;
using SportFlow.Domain.Identity;
using SportFlow.Domain.Shared.ValueObjects;

namespace SportFlow.Application.Tests.Identity;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepo = new();
    private readonly Mock<ITenantRepository> _tenantRepo = new();
    private readonly Mock<IJwtService> _jwtService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly LoginCommandHandler _handler;

    private static readonly Guid TenantGuid = Guid.NewGuid();
    private static readonly Guid UserGuid = Guid.NewGuid();
    private const string ValidSlug = "demo";

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(
            _userRepo.Object,
            _refreshTokenRepo.Object,
            _tenantRepo.Object,
            _jwtService.Object,
            _unitOfWork.Object);
    }

    private static User CreateActiveUser(string email = "user@test.com", string password = "Password1!")
    {
        var hasher = new PasswordHasher<User>();
        var placeholder = User.Create(email, "placeholder", SystemRoles.Member);
        var hash = hasher.HashPassword(placeholder, password);
        return User.Create(email, hash, SystemRoles.Member);
    }

    private static TenantInfo ActiveTenant() =>
        new(TenantGuid, "Demo", ValidSlug, "active", "basic");

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokenResponse()
    {
        // Arrange
        var user = CreateActiveUser();
        var tenantId = TenantId.From(TenantGuid);
        var role = UserTenantRole.Create(user.Id, tenantId, SystemRoles.Member);

        _tenantRepo.Setup(r => r.GetBySlugAsync(ValidSlug, default)).ReturnsAsync(ActiveTenant());
        _userRepo.Setup(r => r.GetByEmailAndTenantAsync(It.IsAny<string>(), tenantId, default)).ReturnsAsync(user);
        _userRepo.Setup(r => r.GetUserTenantRoleAsync(user.Id, tenantId, default)).ReturnsAsync(role);
        _jwtService.Setup(j => j.GenerateAccessToken(user, tenantId, ValidSlug, SystemRoles.Member)).Returns("access-token");
        _jwtService.Setup(j => j.GenerateRefreshToken()).Returns("refresh-raw");
        _jwtService.Setup(j => j.HashToken("refresh-raw")).Returns("refresh-hash");

        var command = new LoginRequest("user@test.com", "Password1!", ValidSlug);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldBe("access-token");
        result.Value.RefreshToken.ShouldBe("refresh-raw");
        _refreshTokenRepo.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), default), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_TenantNotFound_ReturnsFailure()
    {
        _tenantRepo.Setup(r => r.GetBySlugAsync("unknown", default)).ReturnsAsync((TenantInfo?)null);

        var result = await _handler.Handle(new LoginRequest("x@x.com", "pass", "unknown"));

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("TENANT_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_SuspendedTenant_ReturnsFailure()
    {
        _tenantRepo.Setup(r => r.GetBySlugAsync(ValidSlug, default))
            .ReturnsAsync(new TenantInfo(TenantGuid, "Demo", ValidSlug, "suspended", "basic"));

        var result = await _handler.Handle(new LoginRequest("x@x.com", "pass", ValidSlug));

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("TENANT_INACTIVE");
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsInvalidCredentials()
    {
        _tenantRepo.Setup(r => r.GetBySlugAsync(ValidSlug, default)).ReturnsAsync(ActiveTenant());
        _userRepo.Setup(r => r.GetByEmailAndTenantAsync(It.IsAny<string>(), It.IsAny<TenantId>(), default))
            .ReturnsAsync((User?)null);

        var result = await _handler.Handle(new LoginRequest("notfound@x.com", "pass", ValidSlug));

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsInvalidCredentials()
    {
        var user = CreateActiveUser();
        _tenantRepo.Setup(r => r.GetBySlugAsync(ValidSlug, default)).ReturnsAsync(ActiveTenant());
        _userRepo.Setup(r => r.GetByEmailAndTenantAsync(It.IsAny<string>(), It.IsAny<TenantId>(), default))
            .ReturnsAsync(user);

        var result = await _handler.Handle(new LoginRequest("user@test.com", "WrongPass!", ValidSlug));

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Handle_InactiveAccount_ReturnsAccountSuspended()
    {
        var user = CreateActiveUser();
        user.Deactivate();
        _tenantRepo.Setup(r => r.GetBySlugAsync(ValidSlug, default)).ReturnsAsync(ActiveTenant());
        _userRepo.Setup(r => r.GetByEmailAndTenantAsync(It.IsAny<string>(), It.IsAny<TenantId>(), default))
            .ReturnsAsync(user);

        var result = await _handler.Handle(new LoginRequest("user@test.com", "Password1!", ValidSlug));

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("ACCOUNT_SUSPENDED");
    }
}
