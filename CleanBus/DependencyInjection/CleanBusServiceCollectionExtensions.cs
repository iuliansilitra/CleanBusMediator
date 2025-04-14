using CleanBusMediator.Configuration;
using CleanBusMediator.Core;
using CleanBusMediator.Middleware;
using CleanBusMediator.PrePostProcessing;
using CleanBusMediator.Streaming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace CleanBusMediator.DependencyInjection
{
    public class CleanBusMediatorConfig
    {
        internal readonly HashSet<Assembly> Assemblies = new();
        internal readonly HashSet<Type> MiddlewareTypes = new();
        internal ServiceLifetime HandlerLifetime { get; set; } = ServiceLifetime.Transient;
    }

    public static class CleanBusServiceCollectionExtensions
    {
        public static IServiceCollection AddCleanBusMediator(
            this IServiceCollection services,
            Action<CleanBusMediatorConfig> configure)
        {
            var config = new CleanBusMediatorConfig();
            configure(config);

            if (config.Assemblies.Count == 0)
                throw new InvalidOperationException("At least one assembly must be registered.");

            services.TryAddScoped<IDispatcher, Dispatcher>();
            services.TryAddSingleton<ICommandPipelineBuilder, DefaultCommandPipelineBuilder>();

            RegisterHandlersAndMiddlewares(services, config);

            return services;
        }

        private static void RegisterHandlersAndMiddlewares(
            IServiceCollection services,
            CleanBusMediatorConfig config)
        {
            foreach (var assembly in config.Assemblies)
            {
                RegisterImplementations(services, assembly, typeof(ICommandHandler<,>), config.HandlerLifetime);
                RegisterImplementations(services, assembly, typeof(IEventHandler<>), config.HandlerLifetime);
                RegisterImplementations(services, assembly, typeof(IStreamCommandHandler<,>), config.HandlerLifetime);
                RegisterImplementations(services, assembly, typeof(ICommandPreProcessor<>), config.HandlerLifetime);
                RegisterImplementations(services, assembly, typeof(ICommandPostProcessor<,>), config.HandlerLifetime);
            }

            foreach (var middlewareType in config.MiddlewareTypes)
            {
                services.TryAddTransient(typeof(ICommandMiddleware<,>), middlewareType);
            }
        }

        private static void RegisterImplementations(
            IServiceCollection services,
            Assembly assembly,
            Type serviceType,
            ServiceLifetime lifetime)
        {
            var implementationTypes = assembly.DefinedTypes
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .SelectMany(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == serviceType)
                    .Select(i => new { ServiceType = i, ImplementationType = t }));

            foreach (var type in implementationTypes)
            {
                services.Add(new ServiceDescriptor(type.ServiceType, type.ImplementationType, lifetime));
            }
        }
    }

    public static class CleanBusMediatorConfigExtensions
    {
        public static CleanBusMediatorConfig RegisterServicesFromAssembly(
            this CleanBusMediatorConfig config,
            Assembly assembly)
        {
            config.Assemblies.Add(assembly);
            return config;
        }

        public static CleanBusMediatorConfig AddMiddleware(
            this CleanBusMediatorConfig config,
            Type middlewareType)
        {
            if (!middlewareType.IsGenericTypeDefinition ||
                !middlewareType.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(ICommandMiddleware<,>)))
            {
                throw new ArgumentException("Middleware must be an open generic type implementing ICommandMiddleware<,>");
            }

            config.MiddlewareTypes.Add(middlewareType);
            return config;
        }

        public static CleanBusMediatorConfig SetHandlerLifetime(
            this CleanBusMediatorConfig config,
            ServiceLifetime lifetime)
        {
            config.HandlerLifetime = lifetime;
            return config;
        }
    }
}