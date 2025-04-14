using CleanBusMediator.Middleware;

namespace CleanBusMediator.Configuration
{
    public class ScopedCommandPipelineBuilder : ICommandPipelineBuilder
    {
        private readonly IServiceProvider _scopedProvider;
        private readonly ICommandPipelineBuilder _innerBuilder;

        public ScopedCommandPipelineBuilder(IServiceProvider scopedProvider, ICommandPipelineBuilder innerBuilder)
        {
            _scopedProvider = scopedProvider;
            _innerBuilder = innerBuilder;
        }

        public IEnumerable<object> Build(Type commandType, Type resultType)
        {
            var contextAwareMiddlewares = TryResolveScopedMiddlewares(commandType, resultType);
            var baseMiddlewares = _innerBuilder.Build(commandType, resultType);

            foreach (var m in contextAwareMiddlewares)
                yield return m;

            foreach (var m in baseMiddlewares)
                yield return m;
        }

        private IEnumerable<object> TryResolveScopedMiddlewares(Type commandType, Type resultType)
        {
            var middlewareType = typeof(ICommandMiddleware<,>).MakeGenericType(commandType, resultType);
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(middlewareType);

            if (_scopedProvider.GetService(enumerableType) is IEnumerable<object> resolved)
                return resolved;

            return Array.Empty<object>();
        }
    }
}
