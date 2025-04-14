using CleanBusMediator.Core;

namespace CleanBusMediator.Middleware
{
    public interface ICommandExceptionHandler<in TCommand, TResult>
        where TCommand : ICommand<TResult>
    {
        Task<TResult> Handle(TCommand command, Exception exception, CancellationToken cancellationToken);
    }
}