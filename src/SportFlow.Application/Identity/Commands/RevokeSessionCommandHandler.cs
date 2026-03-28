using SportFlow.Application.Abstractions;
using SportFlow.Shared.Results;

namespace SportFlow.Application.Identity.Commands;

public record RevokeSessionCommand(Guid SessionId) : ICommand;

public class RevokeSessionCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork) : ICommandHandler<RevokeSessionCommand>
{
    public async Task<Result> Handle(RevokeSessionCommand command, CancellationToken ct = default)
    {
        var tokens = await refreshTokenRepository.GetActiveByUserAsync(tenantContext.UserId, ct);
        var token = tokens.FirstOrDefault(t => t.Id == command.SessionId);

        if (token is null)
            return Result.Failure("NOT_FOUND", "Session not found.");

        await refreshTokenRepository.RevokeAsync(command.SessionId, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
