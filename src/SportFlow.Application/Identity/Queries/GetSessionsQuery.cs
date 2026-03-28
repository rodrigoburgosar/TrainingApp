using SportFlow.Application.Abstractions;
using SportFlow.Application.Identity.Commands;
using SportFlow.Application.Identity.DTOs;
using SportFlow.Shared.Results;

namespace SportFlow.Application.Identity.Queries;

public record GetSessionsQuery : IQuery<IReadOnlyList<SessionResponse>>;

public class GetSessionsQueryHandler(
    IRefreshTokenRepository refreshTokenRepository,
    ITenantContext tenantContext) : IQueryHandler<GetSessionsQuery, IReadOnlyList<SessionResponse>>
{
    public async Task<Result<IReadOnlyList<SessionResponse>>> Handle(GetSessionsQuery query, CancellationToken ct = default)
    {
        var tokens = await refreshTokenRepository.GetActiveByUserAsync(tenantContext.UserId, ct);
        var sessions = tokens.Select(t => new SessionResponse(
            t.Id, t.IpAddress, t.UserAgent, t.CreatedAt, t.ExpiresAt))
            .ToList();

        return Result.Success<IReadOnlyList<SessionResponse>>(sessions);
    }
}
