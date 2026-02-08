using Polydigm.Metadata;

namespace Polydigm.Execution
{
    /// <summary>
    /// Executes endpoint handlers with validated input.
    /// The executor invokes the actual application logic for the endpoint.
    /// </summary>
    public interface IEndpointExecutor
    {
        /// <summary>
        /// Executes the endpoint handler with validated input.
        /// </summary>
        /// <param name="endpoint">The endpoint metadata.</param>
        /// <param name="validatedInput">The validated input model.</param>
        /// <param name="context">The execution context.</param>
        /// <returns>The result from the endpoint handler.</returns>
        Task<object?> ExecuteAsync(IEndpointMetadata endpoint, object? validatedInput, IExecutionContext context);
    }

    /// <summary>
    /// Represents an endpoint handler implementation.
    /// Applications implement handlers for their business logic.
    /// </summary>
    /// <typeparam name="TInput">The validated input type.</typeparam>
    /// <typeparam name="TOutput">The output type.</typeparam>
    public interface IEndpointHandler<TInput, TOutput>
    {
        /// <summary>
        /// Handles the endpoint request.
        /// </summary>
        /// <param name="input">The validated input.</param>
        /// <param name="context">The execution context.</param>
        /// <returns>The handler result.</returns>
        Task<TOutput> HandleAsync(TInput input, IExecutionContext context);
    }

    /// <summary>
    /// Represents an endpoint handler with no input.
    /// </summary>
    /// <typeparam name="TOutput">The output type.</typeparam>
    public interface IEndpointHandler<TOutput>
    {
        /// <summary>
        /// Handles the endpoint request.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <returns>The handler result.</returns>
        Task<TOutput> HandleAsync(IExecutionContext context);
    }
}
