using Microsoft.AspNetCore.Builder;
using Polydigm.Pipeline;
using PipelineExecutionContext = Polydigm.Pipeline.ServiceExecutionContext;

namespace Polydigm.Hosting.AspNetCore
{
    /// <summary>
    /// Extension methods for wiring the Polydigm pipeline into an ASP.NET Core application.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Wires the default HTTP pipeline into the ASP.NET Core middleware pipeline.
        ///
        /// Requires AddPolydigmHttpServices() to have been called during service registration.
        /// Uses the standard three-component pipeline in order:
        ///   HttpRoutingComponent → HttpExecutionComponent → HttpSerializationComponent
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="passThrough">
        /// When true (default), requests that don't match any endpoint are passed to the next
        /// ASP.NET Core middleware. Set to false to have unmatched requests return 404.
        /// </param>
        public static IApplicationBuilder UsePolydigmHttp(
            this IApplicationBuilder app,
            bool passThrough = true)
        {
            return app.UsePolydigm(pipeline =>
            {
                pipeline
                    .Use<Components.HttpRoutingComponent>()
                    .Use<Components.HttpExecutionComponent>()
                    .Use<Components.HttpSerializationComponent>();
            }, passThrough);
        }

        /// <summary>
        /// Adds a custom Polydigm execution pipeline to the ASP.NET Core middleware pipeline.
        /// Use this overload when you want to provide your own pipeline components rather than
        /// the defaults wired by UsePolydigmHttp.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="configurePipeline">
        /// A delegate to configure the Polydigm pipeline components.
        /// Components are executed in the order they are added.
        /// </param>
        /// <param name="passThrough">
        /// When true (default), requests that don't match any endpoint are passed to the next
        /// ASP.NET Core middleware. Set to false to have unmatched requests return 404.
        /// </param>
        /// <example>
        /// <code>
        /// app.UsePolydigm(pipeline =>
        /// {
        ///     pipeline
        ///         .Use&lt;HttpRoutingComponent&gt;()
        ///         .Use&lt;MyCustomAuthComponent&gt;()
        ///         .Use&lt;HttpExecutionComponent&gt;()
        ///         .Use&lt;HttpSerializationComponent&gt;();
        /// });
        /// </code>
        /// </example>
        public static IApplicationBuilder UsePolydigm(
            this IApplicationBuilder app,
            Action<IPipelineBuilder<PipelineExecutionContext>> configurePipeline,
            bool passThrough = true)
        {
            var builder = new PipelineBuilder<PipelineExecutionContext>();
            configurePipeline(builder);
            var pipeline = builder.Build();

            return app.UseMiddleware<PolydigmMiddleware>(pipeline, passThrough);
        }
    }
}
