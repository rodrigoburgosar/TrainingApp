using Microsoft.AspNetCore.Identity;
using SportFlow.Application.Abstractions;
using SportFlow.Application.Identity.DTOs;
using SportFlow.Domain.Identity;
using SportFlow.Shared.Results;

namespace SportFlow.Application.Identity.Commands;

public class ChangePasswordCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ITenantContext tenantContext,
    IJwtService jwtService,
    IUnitOfWork unitOfWork) : ICommandHandler<ChangePasswordRequest>
{
    private readonly PasswordHasher<User> _hasher = new();

    public async Task<Result> Handle(ChangePasswordRequest command, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(tenantContext.UserId, ct);
        if (user is null)
            return Result.Failure("NOT_FOUND", "User not found.");

        var verificationResult = _hasher.VerifyHashedPassword(user, user.PasswordHash, command.CurrentPassword);
        if (verificationResult == PasswordVerificationResult.Failed)
            return Result.Failure("INVALID_CURRENT_PASSWORD", "Current password is incorrect.");

        var newHash = _hasher.HashPassword(user, command.NewPassword);
        user.UpdatePasswordHash(newHash);

        await refreshTokenRepository.RevokeAllForUserAsync(user.Id, ct);
        await userRepository.UpdateAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
