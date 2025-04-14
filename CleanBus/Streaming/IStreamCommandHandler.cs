namespace CleanBusMediator.Streaming
{
    public interface IStreamCommandHandler<in TCommand, TResult>
        where TCommand : IStreamCommand<TResult>
    {
        IAsyncEnumerable<TResult> Handle(TCommand command, CancellationToken cancellationToken);
    }
}