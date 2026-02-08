using Polydigm.Pipeline;

namespace Polydigm.Logging
{
    /// <summary>
    /// Handles request and response logging/auditing.
    /// Implementations can log to files, databases, external services, etc.
    /// </summary>
    public interface IRequestLogger
    {
        /// <summary>
        /// Logs an incoming request before processing.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <returns>A task representing the async operation.</returns>
        Task LogRequestAsync(IExecutionContext context);

        /// <summary>
        /// Logs an outgoing response after processing.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <returns>A task representing the async operation.</returns>
        Task LogResponseAsync(IExecutionContext context);

        /// <summary>
        /// Logs an error that occurred during processing.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <param name="exception">The exception that occurred.</param>
        /// <returns>A task representing the async operation.</returns>
        Task LogErrorAsync(IExecutionContext context, Exception exception);
    }

    /// <summary>
    /// Represents a logged request.
    /// </summary>
    public sealed class RequestLogEntry
    {
        public DateTimeOffset Timestamp { get; init; }
        public string? EndpointPath { get; init; }
        public string? EndpointName { get; init; }
        public string? CorrelationId { get; init; }
        public string? UserId { get; init; }
        public IDictionary<string, object>? Metadata { get; init; }
    }

    /// <summary>
    /// Represents a logged response.
    /// </summary>
    public sealed class ResponseLogEntry
    {
        public DateTimeOffset Timestamp { get; init; }
        public string? EndpointPath { get; init; }
        public string? EndpointName { get; init; }
        public string? CorrelationId { get; init; }
        public int StatusCode { get; init; }
        public TimeSpan Duration { get; init; }
        public bool Success { get; init; }
        public IDictionary<string, object>? Metadata { get; init; }
    }
}
