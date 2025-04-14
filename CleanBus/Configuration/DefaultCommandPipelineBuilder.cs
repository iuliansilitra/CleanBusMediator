using CleanBusMediator.Middleware;

namespace CleanBusMediator.Configuration
{
    public class DefaultCommandPipelineBuilder : ICommandPipelineBuilder
    {
        private readonly IServiceProvider _serviceProvider;

        public DefaultCommandPipelineBuilder(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IEnumerable<object> Build(Type commandType, Type resultType)
        {
            var middlewareType = typeof(ICommandMiddleware<,>).MakeGenericType(commandType, resultType);
            var middlewares = (IEnumerable<object>)_serviceProvider.GetService(
                typeof(IEnumerable<>).MakeGenericType(middlewareType)) ?? Enumerable.Empty<object>();
            return middlewares;
        }
    }

}
