using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Polydigm.Pipeline;

namespace Polydigm.Hosting.AspNetCore
{
    /// <summary>
    /// Extension methods for registering Polydigm services into the DI container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Scans the given assemblies for classes decorated with [ServiceAttribute] (or any subclass,
        /// e.g. [HttpServiceAttribute]) and automatically registers:
        ///
        ///   1. Each service class as a transient in DI so it can be resolved per request.
        ///   2. An HttpEndpointRegistry singleton populated with all discovered endpoints.
        ///   3. The default HTTP pipeline components (HttpRoutingComponent, HttpExecutionComponent,
        ///      HttpSerializationComponent) as transient services.
        ///
        /// Call this in Program.cs before building the app, then wire the middleware with
        /// UsePolydigmHttp() on the application builder.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="assemblies">
        /// One or more assemblies to scan. Typically Assembly.GetExecutingAssembly() or
        /// typeof(MyService).Assembly.
        /// </param>
        public static IServiceCollection AddPolydigmHttpServices(
            this IServiceCollection services,
            params Assembly[] assemblies)
        {
            var serviceTypes = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract
                         && t.GetCustomAttribute<ServiceAttribute>() is not null)
                .ToList();

            // Register each service class so it can be resolved per request
            foreach (var type in serviceTypes)
                services.AddTransient(type);

            // Build and register the endpoint registry as a singleton
            services.AddSingleton(sp =>
            {
                var registry = new HttpEndpointRegistry();
                foreach (var (metadata, handler) in ServiceDiscovery.DiscoverEndpoints(serviceTypes))
                    registry.Register(metadata, handler);
                return registry;
            });

            // Register default HTTP pipeline components
            services.AddTransient<Components.HttpRoutingComponent>();
            services.AddTransient<Components.HttpExecutionComponent>();
            services.AddTransient<Components.HttpSerializationComponent>();

            return services;
        }
    }
}
