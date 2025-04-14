using CleanBusMediator.Core;
using CleanBusMediator.Events;
using Microsoft.Extensions.DependencyInjection;

namespace CleanBusMediator.Publishing
{
    public static class DispatcherParallelExtensions
    {
        public static async Task PublishParallel<TEvent>(this IDispatcher dispatcher, IServiceProvider serviceProvider, TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : IEvent
        {
            using var scope = serviceProvider.CreateScope();
            var sp = scope.ServiceProvider;

            var handlers = sp.GetServices<IEventHandler<TEvent>>().ToList();

            var middlewareType = typeof(IEventMiddleware<>).MakeGenericType(typeof(TEvent));
            var middlewareEnumerableType = typeof(IEnumerable<>).MakeGenericType(middlewareType);
            var middlewares = (sp.GetService(middlewareEnumerableType) as IEnumerable<object>)
                              ?? Enumerable.Empty<object>();

            var reversedMiddlewares = middlewares.Cast<dynamic>().Reverse().ToList();

            var tasks = handlers.Select(handler =>
            {
                EventHandlerDelegate handlerDelegate = () => handler.Handle(@event, cancellationToken);

                foreach (var middleware in reversedMiddlewares)
                {
                    var next = handlerDelegate;
                    handlerDelegate = () => middleware.Handle((dynamic)@event, cancellationToken, next);
                }

                return handlerDelegate();
            });

            await Task.WhenAll(tasks);
        }
    }
}
