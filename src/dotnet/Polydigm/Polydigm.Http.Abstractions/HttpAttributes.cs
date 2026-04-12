using Polydigm.Pipeline;

namespace Polydigm.Http
{
    /// <summary>
    /// Marks a class as an HTTP service.
    /// Shorthand for [Service(name)] scoped to the HTTP protocol.
    ///
    /// Usage:
    ///   [HttpService("orders")]
    ///   public class OrderService { ... }
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class HttpServiceAttribute : ServiceAttribute
    {
        public HttpServiceAttribute(string name) : base(name) { }
    }

    /// <summary>
    /// Maps a method to an HTTP GET route.
    /// Shorthand for [Route(template, Protocol = Http, Method = "GET")].
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class HttpGetAttribute : RouteAttribute
    {
        public HttpGetAttribute(string template) : base(template)
        {
            Protocol = ServiceProtocol.Http;
            Method = "GET";
        }
    }

    /// <summary>
    /// Maps a method to an HTTP POST route.
    /// Shorthand for [Route(template, Protocol = Http, Method = "POST")].
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class HttpPostAttribute : RouteAttribute
    {
        public HttpPostAttribute(string template) : base(template)
        {
            Protocol = ServiceProtocol.Http;
            Method = "POST";
        }
    }

    /// <summary>
    /// Maps a method to an HTTP PUT route.
    /// Shorthand for [Route(template, Protocol = Http, Method = "PUT")].
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class HttpPutAttribute : RouteAttribute
    {
        public HttpPutAttribute(string template) : base(template)
        {
            Protocol = ServiceProtocol.Http;
            Method = "PUT";
        }
    }

    /// <summary>
    /// Maps a method to an HTTP DELETE route.
    /// Shorthand for [Route(template, Protocol = Http, Method = "DELETE")].
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class HttpDeleteAttribute : RouteAttribute
    {
        public HttpDeleteAttribute(string template) : base(template)
        {
            Protocol = ServiceProtocol.Http;
            Method = "DELETE";
        }
    }

    /// <summary>
    /// Maps a method to an HTTP PATCH route.
    /// Shorthand for [Route(template, Protocol = Http, Method = "PATCH")].
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class HttpPatchAttribute : RouteAttribute
    {
        public HttpPatchAttribute(string template) : base(template)
        {
            Protocol = ServiceProtocol.Http;
            Method = "PATCH";
        }
    }
}
