namespace Polydigm.Pipeline
{
    /// <summary>
    /// The transport protocol an endpoint is exposed over.
    /// Used by RouteAttribute and its derivatives to express protocol-specific routing.
    /// </summary>
    public enum ServiceProtocol
    {
        Unspecified,
        Http,
        Grpc,
        Amqp,
        Soap,
        WebSocket
    }

    /// <summary>
    /// Marks a method as a routable endpoint on a service class.
    ///
    /// This is the base, protocol-agnostic route attribute.
    /// Protocol-specific shorthands (e.g. HttpGetAttribute) extend this class and
    /// pre-fill Protocol and Method so callers only need to supply the template:
    ///
    ///   [HttpGet("items")]
    ///   // is shorthand for:
    ///   [Route("items", Protocol = ServiceProtocol.Http, Method = "GET")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class RouteAttribute : Attribute
    {
        /// <summary>The path template relative to the service base path (e.g. "items", "items/{id}").</summary>
        public string Template { get; }

        /// <summary>The transport protocol this route is exposed over.</summary>
        public ServiceProtocol Protocol { get; protected set; } = ServiceProtocol.Unspecified;

        /// <summary>The protocol operation/verb (e.g. "GET", "POST" for HTTP; method name for gRPC).</summary>
        public string Method { get; protected set; } = "GET";

        public RouteAttribute(string template)
        {
            Template = template;
        }
    }

    /// <summary>
    /// Marks a class as a service whose public attributed methods are exposed as endpoints.
    ///
    /// This is the protocol-agnostic base. Use protocol-specific subclasses like
    /// HttpServiceAttribute to express the transport.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceAttribute : Attribute
    {
        /// <summary>
        /// The service identifier, used as the base path segment.
        /// For example, "sample" produces a base path of /sample/.
        /// </summary>
        public string Name { get; }

        public ServiceAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Specifies the API version for a service class.
    /// When present, the version is prepended to all routes as the first path segment.
    /// For example, [Version("v1")] on a service named "sample" gives base path /v1/sample.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class VersionAttribute : Attribute
    {
        public string Version { get; }

        public VersionAttribute(string version)
        {
            Version = version;
        }
    }
}
