using Polydigm.Pipeline;

namespace Polydigm.Telemetry
{
    /// <summary>
    /// Handles telemetry, tracing, and metrics collection.
    /// Implementations can integrate with OpenTelemetry, Application Insights, Datadog, etc.
    /// </summary>
    public interface ITelemetryProvider
    {
        /// <summary>
        /// Starts a new activity/span for distributed tracing.
        /// </summary>
        /// <param name="name">The activity name (typically the endpoint name).</param>
        /// <param name="context">The execution context.</param>
        /// <returns>An activity scope that should be disposed when the activity completes.</returns>
        IActivityScope StartActivity(string name, IExecutionContext context);

        /// <summary>
        /// Records a metric value.
        /// </summary>
        /// <param name="name">The metric name.</param>
        /// <param name="value">The metric value.</param>
        /// <param name="tags">Optional tags/dimensions for the metric.</param>
        void RecordMetric(string name, double value, IDictionary<string, object>? tags = null);

        /// <summary>
        /// Records a counter increment.
        /// </summary>
        /// <param name="name">The counter name.</param>
        /// <param name="increment">The increment amount (default 1).</param>
        /// <param name="tags">Optional tags/dimensions for the counter.</param>
        void IncrementCounter(string name, long increment = 1, IDictionary<string, object>? tags = null);

        /// <summary>
        /// Records a duration/histogram value.
        /// </summary>
        /// <param name="name">The histogram name.</param>
        /// <param name="duration">The duration to record.</param>
        /// <param name="tags">Optional tags/dimensions for the histogram.</param>
        void RecordDuration(string name, TimeSpan duration, IDictionary<string, object>? tags = null);

        /// <summary>
        /// Extracts trace context from the execution context properties.
        /// Used to propagate distributed tracing context across service boundaries.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <returns>The trace context, or null if none exists.</returns>
        TraceContext? ExtractTraceContext(IExecutionContext context);

        /// <summary>
        /// Injects trace context into the execution context properties.
        /// Used to propagate distributed tracing context across service boundaries.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <param name="traceContext">The trace context to inject.</param>
        void InjectTraceContext(IExecutionContext context, TraceContext traceContext);
    }

    /// <summary>
    /// Represents an activity/span in distributed tracing.
    /// Dispose when the activity completes.
    /// </summary>
    public interface IActivityScope : IDisposable
    {
        /// <summary>
        /// The activity ID for correlation.
        /// </summary>
        string ActivityId { get; }

        /// <summary>
        /// Adds a tag/attribute to the activity.
        /// </summary>
        /// <param name="key">The tag key.</param>
        /// <param name="value">The tag value.</param>
        void SetTag(string key, object value);

        /// <summary>
        /// Adds an event to the activity.
        /// </summary>
        /// <param name="name">The event name.</param>
        /// <param name="attributes">Optional event attributes.</param>
        void AddEvent(string name, IDictionary<string, object>? attributes = null);

        /// <summary>
        /// Records an exception on the activity.
        /// </summary>
        /// <param name="exception">The exception to record.</param>
        void RecordException(Exception exception);
    }

    /// <summary>
    /// Represents distributed tracing context.
    /// </summary>
    public sealed class TraceContext
    {
        /// <summary>
        /// The trace ID (shared across all spans in a trace).
        /// </summary>
        public string TraceId { get; init; } = string.Empty;

        /// <summary>
        /// The parent span ID.
        /// </summary>
        public string? ParentSpanId { get; init; }

        /// <summary>
        /// Trace flags (e.g., sampled, debug).
        /// </summary>
        public byte TraceFlags { get; init; }

        /// <summary>
        /// Additional baggage/context to propagate.
        /// </summary>
        public IDictionary<string, string>? Baggage { get; init; }
    }
}
