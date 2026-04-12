using Polydigm.Http;
using Polydigm.Pipeline;
using Polydigm.Sample.WebApi.Models;

namespace Polydigm.Sample.WebApi.Services
{
    /// <summary>
    /// Sample HTTP service demonstrating attribute-based routing.
    ///
    /// [HttpService("sample")] names the service and sets the base path segment.
    /// [Version("v1")] prepends a version prefix to all routes in this class.
    ///
    /// The resulting base path is /v1/sample, so:
    ///   [HttpGet("items")]         → GET  /v1/sample/items
    ///   [HttpGet("items/{id}")]    → GET  /v1/sample/items/{id}
    ///   [HttpPost("items")]        → POST /v1/sample/items
    ///
    /// Parameters:
    ///   - Simple/nullable types    → bound from the query string by name
    ///   - Route template segments  → bound from the matched route parameter
    ///   - [Validated] types        → bound from the query string and validated via TryCreate;
    ///                                invalid values produce an automatic 400 response
    ///
    /// ISampleRepository is resolved from DI; the service itself is transient.
    /// </summary>
    [HttpService("sample")]
    [Version("v1")]
    public class SampleService(ISampleRepository repository)
    {
        /// <summary>
        /// Returns all items, optionally filtered by a validated description fragment.
        /// GET /v1/sample/items?description=widget
        /// </summary>
        [HttpGet("items")]
        public Task<IEnumerable<Item>> GetItemsAsync(ItemDescription? description = null)
        {
            var items = description is null
                ? (IEnumerable<Item>)repository.Items
                : repository.Items.Where(i =>
                    i.Description.Contains(description.Value.Value,
                        StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(items);
        }

        /// <summary>
        /// Returns a single item by its ID.
        /// GET /v1/sample/items/{id}
        /// </summary>
        [HttpGet("items/{id}")]
        public Task<Item?> GetItemByIdAsync(string id)
        {
            var item = repository.Items.FirstOrDefault(i =>
                i.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(item);
        }

        /// <summary>
        /// Returns the service health status.
        /// GET /v1/sample/health
        /// </summary>
        [HttpGet("health")]
        public Task<object> GetHealthAsync()
        {
            return Task.FromResult<object>(new
            {
                status = "ok",
                service = "sample",
                timestamp = DateTime.UtcNow
            });
        }
    }
}
