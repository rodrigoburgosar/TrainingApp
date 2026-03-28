using SportFlow.Shared.Results;

namespace SportFlow.Application.Abstractions;

public interface ICommand;
public interface ICommand<TResult>;

public interface ICommandHandler<TCommand>
    where TCommand : ICommand
{
    Task<Result> Handle(TCommand command, CancellationToken ct = default);
}

public interface ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> Handle(TCommand command, CancellationToken ct = default);
}
