using SportFlow.Application.Abstractions;
using SportFlow.Application.Identity.DTOs;
using SportFlow.Shared.Results;

namespace SportFlow.Application.Identity.Commands;

public class LogoutCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IJwtService jwtService,
    IUnitOfWork unitOfWork) : ICommandHandler<LogoutRequest>
{
    public async Task<Result> Handle(LogoutRequest command, CancellationToken ct = default)
    {
        var tokenHash = jwtService.HashToken(command.RefreshToken);
        var existing = await refreshTokenRepository.GetByHashAsync(tokenHash, ct);

        if (existing is not null && existing.IsActive)
        {
            await refreshTokenRepository.RevokeAsync(existing.Id, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }

        return Result.Success();
    }
}
