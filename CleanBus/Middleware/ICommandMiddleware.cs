using CleanBusMediator.Core;

namespace CleanBusMediator.Middleware
{
    public delegate Task<TResult> CommandHandlerDelegate<TResult>();

    public interface ICommandMiddleware<in TCommand, TResult>
        where TCommand : ICommand<TResult>
    {
        Task<TResult> Handle(TCommand command, CancellationToken cancellationToken, CommandHandlerDelegate<TResult> next);
    }
}
