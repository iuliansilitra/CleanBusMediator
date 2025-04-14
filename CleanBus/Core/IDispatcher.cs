using CleanBusMediator.Streaming;

namespace CleanBusMediator.Core
{
    public interface IDispatcher
    {
        Task<TResult> Send<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);
        Task Publish<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : IEvent;
        IAsyncEnumerable<TResult> Stream<TResult>(IStreamCommand<TResult> command, CancellationToken cancellationToken = default);
    }
}