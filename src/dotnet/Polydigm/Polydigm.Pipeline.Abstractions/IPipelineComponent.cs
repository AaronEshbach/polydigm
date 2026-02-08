namespace Polydigm.Pipeline
{
    /// <summary>
    /// Represents a component in the execution pipeline.
    /// Components process requests in order: deserialization → validation → routing → execution → serialization.
    /// Each component can inspect/modify the context and choose to continue or short-circuit the pipeline.
    /// </summary>
    /// <typeparam name="TContext">The type of execution context flowing through the pipeline.</typeparam>
    public interface IPipelineComponent<TContext> where TContext : IExecutionContext
    {
        /// <summary>
        /// Processes the context and invokes the next component in the pipeline.
        /// </summary>
        /// <param name="context">The execution context containing request/response data.</param>
        /// <param name="next">Delegate to invoke the next component in the pipeline.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task InvokeAsync(TContext context, PipelineDelegate<TContext> next);
    }

    /// <summary>
    /// Delegate representing the next component in the pipeline.
    /// </summary>
    /// <typeparam name="TContext">The type of execution context.</typeparam>
    /// <param name="context">The execution context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public delegate Task PipelineDelegate<TContext>(TContext context) where TContext : IExecutionContext;

    /// <summary>
    /// Builder for constructing execution pipelines.
    /// Allows composing middleware components in a specific order.
    /// </summary>
    /// <typeparam name="TContext">The type of execution context.</typeparam>
    public interface IPipelineBuilder<TContext> where TContext : IExecutionContext
    {
        /// <summary>
        /// Adds a middleware component to the pipeline using a factory function.
        /// </summary>
        /// <param name="middleware">Factory function that wraps the next delegate.</param>
        /// <returns>The pipeline builder for chaining.</returns>
        IPipelineBuilder<TContext> Use(Func<PipelineDelegate<TContext>, PipelineDelegate<TContext>> middleware);

        /// <summary>
        /// Adds a typed middleware component to the pipeline.
        /// </summary>
        /// <typeparam name="TComponent">The component type.</typeparam>
        /// <returns>The pipeline builder for chaining.</returns>
        IPipelineBuilder<TContext> Use<TComponent>() where TComponent : IPipelineComponent<TContext>;

        /// <summary>
        /// Adds a middleware component instance to the pipeline.
        /// </summary>
        /// <param name="component">The component instance.</param>
        /// <returns>The pipeline builder for chaining.</returns>
        IPipelineBuilder<TContext> Use(IPipelineComponent<TContext> component);

        /// <summary>
        /// Builds the pipeline and returns the final delegate.
        /// </summary>
        /// <returns>The composed pipeline delegate.</returns>
        PipelineDelegate<TContext> Build();
    }
}
