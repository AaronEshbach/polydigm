using System.Reflection;
using Polydigm.Hosting.AspNetCore;
using Polydigm.Sample.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ─── Repository ──────────────────────────────────────────────────────────────
builder.Services.AddSingleton<ISampleRepository, InMemorySampleRepository>();

// ─── Polydigm service discovery ───────────────────────────────────────────────
// Scans the assembly for classes decorated with [HttpService] (or [Service]),
// registers each service class as transient in DI, builds the endpoint registry,
// and registers the default HTTP pipeline components.
builder.Services.AddPolydigmHttpServices(Assembly.GetExecutingAssembly());

var app = builder.Build();

// ─── Wire Polydigm into the ASP.NET Core pipeline ────────────────────────────
// UsePolydigmHttp wires the default three-component pipeline:
//   HttpRoutingComponent → HttpExecutionComponent → HttpSerializationComponent
//
// Pass-through is enabled (default): unmatched requests fall through to other
// ASP.NET Core middleware instead of returning 404.
app.UsePolydigmHttp();

app.Run();
