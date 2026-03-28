using Moq;
using Shouldly;
using SportFlow.Application.Abstractions;
using SportFlow.Application.Identity.Commands;
using SportFlow.Application.Identity.DTOs;
using SportFlow.Domain.Identity;
using SportFlow.Domain.Shared.ValueObjects;
using SportFlow.Domain.Tenants;

namespace SportFlow.Application.Tests.Identity;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ITenantRepository> _tenantRepo = new();
    private readonly Mock<IJwtService> _jwtService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly RefreshTokenCommandHandler _handler;

    private static readonly UserId UserId = Domain.Shared.ValueObjects.UserId.New();
    private static readonly TenantId TenantId = Domain.Shared.ValueObjects.TenantId.New();
    private const string RawToken = "raw-refresh-token";
    private const string TokenHash = "hashed-token";

    public RefreshTokenCommandHandlerTests()
    {
        _handler = new RefreshTokenCommandHandler(
            _refreshTokenRepo.Object,
            _userRepo.Object,
            _tenantRepo.Object,
            _jwtService.Object,
            _unitOfWork.Object);

        _jwtService.Setup(j => j.HashToken(RawToken)).Returns(TokenHash);
    }

    private static RefreshToken CreateActiveToken() =>
        RefreshToken.Create(UserId, TenantId, TokenHash, DateTime.UtcNow.AddDays(30));

    private static RefreshToken CreateExpiredToken() =>
        RefreshToken.Create(UserId, TenantId, TokenHash, DateTime.UtcNow.AddDays(-1));

    private static User CreateActiveUser() =>
        User.Create("user@test.com", "hash", SystemRoles.Member);

    [Fact]
    public async Task Handle_ValidToken_ReturnsNewTokenPair()
    {
        // Arrange
        var token = CreateActiveToken();
        var user = CreateActiveUser();
        var role = UserTenantRole.Create(UserId, TenantId, SystemRoles.Member);

        _refreshTokenRepo.Setup(r => r.GetByHashAsync(TokenHash, default)).ReturnsAsync(token);
        _userRepo.Setup(r => r.GetByIdAsync(UserId, default)).ReturnsAsync(user);
        _userRepo.Setup(r => r.GetUserTenantRoleAsync(UserId, TenantId, default)).ReturnsAsync(role);
        _jwtService.Setup(j => j.GenerateRefreshToken()).Returns("new-raw-token");
        _jwtService.Setup(j => j.HashToken("new-raw-token")).Returns("new-hash");
        _jwtService.Setup(j => j.GenerateAccessToken(user, TenantId, null, SystemRoles.Member))
            .Returns("new-access-token");

        // Act
        var result = await _handler.Handle(new RefreshTokenRequest(RawToken));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldBe("new-access-token");
        result.Value.RefreshToken.ShouldBe("new-raw-token");
        _refreshTokenRepo.Verify(r => r.RevokeAsync(token.Id, default), Times.Once);
        _refreshTokenRepo.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), default), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(default), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_TokenNotFound_ReturnsFailure()
    {
        _refreshTokenRepo.Setup(r => r.GetByHashAsync(TokenHash, default)).ReturnsAsync((RefreshToken?)null);

        var result = await _handler.Handle(new RefreshTokenRequest(RawToken));

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("REFRESH_EXPIRED");
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsFailure()
    {
        var expired = CreateExpiredToken();
        _refreshTokenRepo.Setup(r => r.GetByHashAsync(TokenHash, default)).ReturnsAsync(expired);

        var result = await _handler.Handle(new RefreshTokenRequest(RawToken));

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("REFRESH_EXPIRED");
    }

    [Fact]
    public async Task Handle_RevokedToken_RevokesAllAndReturnsFailure()
    {
        // Compromised token scenario: rotation attack
        var token = CreateActiveToken();
        token.Revoke(); // simulate already revoked

        _refreshTokenRepo.Setup(r => r.GetByHashAsync(TokenHash, default)).ReturnsAsync(token);

        var result = await _handler.Handle(new RefreshTokenRequest(RawToken));

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("REFRESH_EXPIRED");
        _refreshTokenRepo.Verify(r => r.RevokeAllForUserAsync(UserId, default), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }
}
