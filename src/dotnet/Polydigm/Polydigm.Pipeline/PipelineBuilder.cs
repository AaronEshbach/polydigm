namespace Polydigm.Pipeline
{
    /// <summary>
    /// Concrete implementation of IPipelineBuilder<TContext>.
    /// Composes middleware components into a single pipeline delegate using a chain-of-responsibility pattern.
    /// Components are resolved from the execution context's IServiceProvider per request,
    /// so they support scoped and transient DI lifetimes.
    /// </summary>
    /// <typeparam name="TContext">The type of execution context flowing through the pipeline.</typeparam>
    public sealed class PipelineBuilder<TContext> : IPipelineBuilder<TContext>
        where TContext : IExecutionContext
    {
        private readonly List<Func<PipelineDelegate<TContext>, PipelineDelegate<TContext>>> _components = new();

        /// <inheritdoc />
        public IPipelineBuilder<TContext> Use(Func<PipelineDelegate<TContext>, PipelineDelegate<TContext>> middleware)
        {
            _components.Add(middleware);
            return this;
        }

        /// <inheritdoc />
        public IPipelineBuilder<TContext> Use<TComponent>()
            where TComponent : IPipelineComponent<TContext>
        {
            _components.Add(next => context =>
            {
                // Resolve from DI if available, otherwise create with Activator (useful in tests)
                var resolved = context.Services?.GetService(typeof(TComponent));
                var component = resolved is TComponent typedComponent
                    ? typedComponent
                    : Activator.CreateInstance<TComponent>();
                return component.InvokeAsync(context, next);
            });
            return this;
        }

        /// <inheritdoc />
        public IPipelineBuilder<TContext> Use(IPipelineComponent<TContext> component)
        {
            _components.Add(next => context => component.InvokeAsync(context, next));
            return this;
        }

        /// <inheritdoc />
        public PipelineDelegate<TContext> Build()
        {
            // Start with a no-op terminal delegate at the end of the chain.
            PipelineDelegate<TContext> pipeline = _ => Task.CompletedTask;

            // Wrap each component around the chain from last to first,
            // so the first registered component runs first.
            for (var i = _components.Count - 1; i >= 0; i--)
            {
                pipeline = _components[i](pipeline);
            }

            return pipeline;
        }
    }
}
