using CleanBusMediator.PrePostProcessing;
using CleanBusMediator.Streaming;
using CleanBusMediator.Middleware;
using CleanBusMediator.Events;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Reflection;

namespace CleanBusMediator.Core
{
    public class Dispatcher : IDispatcher
    {
        private readonly IServiceProvider _serviceProvider;
        private static readonly ConcurrentDictionary<Type, MethodInfo> _handleMethodCache = new();
        private static readonly ConcurrentDictionary<Type, Type> _handlerTypeCache = new();

        public Dispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<TResult> Send<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var sp = scope.ServiceProvider;

            var commandType = command.GetType();

            // Cache handler type resolution
            var handlerType = _handlerTypeCache.GetOrAdd(commandType,
                t => typeof(ICommandHandler<,>).MakeGenericType(t, typeof(TResult)));

            var handler = sp.GetService(handlerType);
            if (handler is null)
                throw new InvalidOperationException($"No handler registered for {handlerType.FullName}");

            // Cache method info lookup
            var handleMethod = _handleMethodCache.GetOrAdd(handlerType,
                t => t.GetMethod("Handle") ?? throw new InvalidOperationException($"Handler {t.Name} does not contain a method 'Handle'"));

            var middlewares = GetServicesAsDynamic(sp, typeof(ICommandMiddleware<,>), commandType, typeof(TResult));
            var preProcessors = GetServicesAsDynamic(sp, typeof(ICommandPreProcessor<>), commandType);
            var postProcessors = GetServicesAsDynamic(sp, typeof(ICommandPostProcessor<,>), commandType, typeof(TResult));

            CommandHandlerDelegate<TResult> handlerDelegate = async () =>
            {
                await ProcessPreProcessors(preProcessors, command, cancellationToken);

                var result = await (Task<TResult>)handleMethod.Invoke(handler, new object[] { command, cancellationToken })!;

                await ProcessPostProcessors(postProcessors, command, result, cancellationToken);

                return result;
            };

            handlerDelegate = WrapWithMiddlewares(middlewares, handlerDelegate, command, cancellationToken);

            try
            {
                return await handlerDelegate();
            }
            catch (Exception ex)
            {
                var exceptionHandlerType = typeof(ICommandExceptionHandler<,>).MakeGenericType(commandType, typeof(TResult));
                if (sp.GetService(exceptionHandlerType) is { } exHandler)
                {
                    return await ((dynamic)exHandler).Handle((dynamic)command, ex, cancellationToken);
                }

                throw;
            }
        }

        public async Task Publish<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
        {
            using var scope = _serviceProvider.CreateScope();
            var sp = scope.ServiceProvider;

            var handlers = sp.GetServices<IEventHandler<TEvent>>().ToList();
            if (handlers.Count == 0) return;

            var middlewares = GetServicesAsDynamic(sp, typeof(IEventMiddleware<>), typeof(TEvent));

            foreach (var handler in handlers)
            {
                EventHandlerDelegate handlerDelegate = () => handler.Handle(@event, cancellationToken);
                handlerDelegate = WrapWithMiddlewares(middlewares, handlerDelegate, @event, cancellationToken);
                await handlerDelegate();
            }
        }

        public IAsyncEnumerable<TResult> Stream<TResult>(IStreamCommand<TResult> command, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var sp = scope.ServiceProvider;

            var handlerType = typeof(IStreamCommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
            var handler = sp.GetRequiredService(handlerType);

            return ((dynamic)handler).Handle((dynamic)command, cancellationToken);
        }

        private static IEnumerable<dynamic> GetServicesAsDynamic(IServiceProvider sp, Type genericType, params Type[] typeArguments)
        {
            var serviceType = genericType.MakeGenericType(typeArguments);
            var collectionType = typeof(IEnumerable<>).MakeGenericType(serviceType);

            if (sp.GetService(collectionType) is IEnumerable<object> services)
            {
                return services;
            }

            return Enumerable.Empty<object>();
        }

        private static async Task ProcessPreProcessors(IEnumerable<dynamic> preProcessors, object command, CancellationToken cancellationToken)
        {
            foreach (var pre in preProcessors)
            {
                await pre.Process((dynamic)command, cancellationToken);
            }
        }

        private static async Task ProcessPostProcessors(IEnumerable<dynamic> postProcessors, object command, object result, CancellationToken cancellationToken)
        {
            foreach (var post in postProcessors)
            {
                await post.Process((dynamic)command, (dynamic)result, cancellationToken);
            }
        }

        private static CommandHandlerDelegate<TResult> WrapWithMiddlewares<TResult>(
            IEnumerable<dynamic> middlewares,
            CommandHandlerDelegate<TResult> handlerDelegate,
            object command,
            CancellationToken cancellationToken)
        {
            foreach (var middleware in middlewares.Reverse())
            {
                var next = handlerDelegate;
                handlerDelegate = () => middleware.Handle((dynamic)command, cancellationToken, next);
            }
            return handlerDelegate;
        }

        private static EventHandlerDelegate WrapWithMiddlewares(
            IEnumerable<dynamic> middlewares,
            EventHandlerDelegate handlerDelegate,
            object @event,
            CancellationToken cancellationToken)
        {
            foreach (var middleware in middlewares.Reverse())
            {
                var next = handlerDelegate;
                handlerDelegate = () => middleware.Handle((dynamic)@event, cancellationToken, next);
            }
            return handlerDelegate;
        }
    }
}