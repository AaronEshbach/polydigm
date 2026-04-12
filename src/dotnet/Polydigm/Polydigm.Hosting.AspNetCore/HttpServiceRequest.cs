using Microsoft.AspNetCore.Http;
using Polydigm.Pipeline;

namespace Polydigm.Hosting.AspNetCore
{
    /// <summary>
    /// Adapts an ASP.NET Core HttpRequest to the Polydigm IServiceRequest model.
    /// The body stream is passed through directly — no buffering or deserialization occurs here.
    /// </summary>
    public sealed class HttpServiceRequest : IServiceRequest
    {
        public string Path { get; }
        public string Method { get; }
        public IReadOnlyDictionary<string, string> Headers { get; }
        public IReadOnlyDictionary<string, string> QueryParameters { get; }
        public IDictionary<string, string> RouteParameters { get; }
        public Stream? Body { get; }
        public string? ContentType { get; }
        public string CorrelationId { get; }
        public IReadOnlyDictionary<string, object> Properties { get; }

        public HttpServiceRequest(HttpRequest request)
        {
            Path = request.Path.Value ?? string.Empty;
            Method = request.Method.ToUpperInvariant();
            ContentType = request.ContentType;
            CorrelationId = ExtractCorrelationId(request);
            Body = request.ContentLength > 0 || request.Headers.ContainsKey("Transfer-Encoding")
                ? request.Body
                : null;

            Headers = FlattenHeaders(request);
            QueryParameters = FlattenQuery(request);
            RouteParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Properties = new Dictionary<string, object>();
        }

        private static string ExtractCorrelationId(HttpRequest request)
        {
            if (request.Headers.TryGetValue("X-Correlation-ID", out var id) && id.Count > 0)
                return id[0]!;
            if (request.Headers.TryGetValue("X-Request-ID", out var reqId) && reqId.Count > 0)
                return reqId[0]!;
            return Guid.NewGuid().ToString();
        }

        private static IReadOnlyDictionary<string, string> FlattenHeaders(HttpRequest request)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, values) in request.Headers)
                result[key] = values.ToString(); // joins multiple values with ", "
            return result;
        }

        private static IReadOnlyDictionary<string, string> FlattenQuery(HttpRequest request)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, values) in request.Query)
                result[key] = values.ToString();
            return result;
        }
    }
}
