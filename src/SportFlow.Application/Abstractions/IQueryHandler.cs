using SportFlow.Shared.Results;

namespace SportFlow.Application.Abstractions;

public interface IQuery<TResult>;

public interface IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<Result<TResult>> Handle(TQuery query, CancellationToken ct = default);
}
