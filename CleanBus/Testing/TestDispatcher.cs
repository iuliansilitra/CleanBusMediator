using CleanBusMediator.Core;
using CleanBusMediator.Streaming;

namespace CleanBusMediator.Testing
{
    public class TestDispatcher : IDispatcher
    {
        private readonly Dictionary<Type, object> _handlers = new();

        public void RegisterHandler<TCommand, TResult>(ICommandHandler<TCommand, TResult> handler)
            where TCommand : ICommand<TResult>
        {
            _handlers[typeof(ICommandHandler<TCommand, TResult>)] = handler;
        }

        public Task<TResult> Send<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
        {
            var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
            if (_handlers.TryGetValue(handlerType, out var handler))
            {
                return ((ICommandHandler<ICommand<TResult>, TResult>)handler).Handle(command, cancellationToken);
            }

            throw new InvalidOperationException("Handler not registered in TestDispatcher.");
        }

        public Task Publish<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
        {
            // No-op by default, can be extended later
            return Task.CompletedTask;
        }

        public IAsyncEnumerable<TResult> Stream<TResult>(IStreamCommand<TResult> command, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Streaming not implemented for TestDispatcher.");
        }
    }
}
