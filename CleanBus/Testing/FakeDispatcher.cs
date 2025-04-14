using CleanBusMediator.Core;
using CleanBusMediator.Streaming;

namespace CleanBusMediator.Testing
{
    public class FakeDispatcher : IDispatcher
    {
        public List<object> SentCommands { get; } = new();
        public List<object> PublishedEvents { get; } = new();

        public Func<object, object>? SendHandler { get; set; }
        public Func<object, IAsyncEnumerable<object>>? StreamHandler { get; set; }

        public Task<TResult> Send<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
        {
            SentCommands.Add(command);
            if (SendHandler is not null)
                return Task.FromResult((TResult)SendHandler(command)!);

            throw new InvalidOperationException("No SendHandler provided in FakeDispatcher.");
        }

        public Task Publish<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : IEvent
        {
            PublishedEvents.Add(@event);
            return Task.CompletedTask;
        }

        public IAsyncEnumerable<TResult> Stream<TResult>(IStreamCommand<TResult> command, CancellationToken cancellationToken = default)
        {
            if (StreamHandler is not null)
                return (IAsyncEnumerable<TResult>)StreamHandler(command);

            throw new InvalidOperationException("No StreamHandler provided in FakeDispatcher.");
        }
    }
}
