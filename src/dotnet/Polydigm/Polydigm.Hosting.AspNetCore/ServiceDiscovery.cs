using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Polydigm.Metadata;
using Polydigm.Pipeline;
using Polydigm.Validation;

namespace Polydigm.Hosting.AspNetCore
{
    /// <summary>
    /// Scans service classes decorated with [ServiceAttribute] (or a subclass like [HttpServiceAttribute])
    /// and produces endpoint registrations from their attributed methods.
    ///
    /// Path construction:
    ///   /{version}/{serviceName}/{methodTemplate}
    /// where {version} is omitted when no [Version] attribute is present.
    ///
    /// Parameter binding per method parameter (in priority order):
    ///   1. Route parameter   — name matches a {segment} in the method template
    ///   2. Validated type    — type is decorated with [Validated]; value comes from the query string,
    ///                          is parsed via the type's static TryCreate(string, out T) method.
    ///                          Failure throws ValidationException → 400.
    ///   3. Simple/primitive  — string, int, Guid, bool, DateTime, etc.; value from the query string.
    ///   4. Optional/nullable — missing query value yields the parameter's default or null.
    ///
    /// Complex types (non-primitive, non-validated) are not yet supported and raise
    /// NotSupportedException at startup rather than at request time.
    /// </summary>
    internal static class ServiceDiscovery
    {
        private static readonly HashSet<Type> SimpleTypes = new()
        {
            typeof(string), typeof(bool), typeof(byte), typeof(sbyte),
            typeof(short), typeof(ushort), typeof(int), typeof(uint),
            typeof(long), typeof(ulong), typeof(float), typeof(double),
            typeof(decimal), typeof(char), typeof(Guid), typeof(DateTime),
            typeof(DateTimeOffset), typeof(TimeSpan)
        };

        /// <summary>
        /// Discovers all endpoints from the given service types and yields
        /// (metadata, handler) pairs ready to register into HttpEndpointRegistry.
        /// </summary>
        public static IEnumerable<(IEndpointMetadata Metadata, Func<IExecutionContext, Task<object?>> Handler)>
            DiscoverEndpoints(IEnumerable<Type> serviceTypes)
        {
            foreach (var type in serviceTypes)
            {
                var serviceAttr = type.GetCustomAttribute<ServiceAttribute>();
                if (serviceAttr is null)
                    continue;

                var versionAttr = type.GetCustomAttribute<VersionAttribute>();

                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    var routeAttr = method.GetCustomAttribute<RouteAttribute>();
                    if (routeAttr is null)
                        continue;

                    var fullPath = BuildPath(serviceAttr.Name, versionAttr?.Version, routeAttr.Template);
                    var httpMethod = routeAttr.Method.ToUpperInvariant();

                    var metadata = new EndpointMetadata
                    {
                        Name = method.Name,
                        Path = fullPath,
                        Semantics = new EndpointSemantics
                        {
                            Intent = InferIntent(httpMethod),
                            IsSafe = httpMethod == "GET",
                            IsIdempotent = httpMethod is "GET" or "PUT" or "DELETE"
                        },
                        Extensions = new Dictionary<string, object>
                        {
                            [HttpExtensionKeys.Method] = httpMethod
                        }
                    };

                    var handler = BuildHandler(type, method, routeAttr.Template);
                    yield return (metadata, handler);
                }
            }
        }

        // ── Path helpers ──────────────────────────────────────────────────────────

        private static string BuildPath(string serviceName, string? version, string template)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(version))
                parts.Add(version.Trim('/'));
            parts.Add(serviceName.Trim('/'));
            var t = template.Trim('/');
            if (!string.IsNullOrEmpty(t))
                parts.Add(t);
            return "/" + string.Join("/", parts);
        }

        private static OperationIntent InferIntent(string method) => method switch
        {
            "GET"    => OperationIntent.Query,
            "POST"   => OperationIntent.Create,
            "PUT"    => OperationIntent.Update,
            "DELETE" => OperationIntent.Delete,
            _        => OperationIntent.Action
        };

        // ── Handler builder ───────────────────────────────────────────────────────

        private static Func<IExecutionContext, Task<object?>> BuildHandler(
            Type serviceType, MethodInfo method, string routeTemplate)
        {
            // Build per-parameter binders once at startup; close over them in the lambda.
            var parameters = method.GetParameters();
            var binders = parameters
                .Select(p => BuildParameterBinder(p, routeTemplate))
                .ToArray();

            return async context =>
            {
                var service = context.Services!.GetRequiredService(serviceType);

                var args = new object?[parameters.Length];
                for (var i = 0; i < parameters.Length; i++)
                    args[i] = binders[i](context);

                // Invoke and unwrap Task/Task<T>
                var rawResult = method.Invoke(service, args);

                if (rawResult is Task task)
                {
                    await task.ConfigureAwait(false);

                    var taskType = task.GetType();
                    if (taskType.IsGenericType)
                    {
                        var resultProp = taskType.GetProperty(nameof(Task<object>.Result));
                        return resultProp?.GetValue(task);
                    }
                    return null;
                }

                return rawResult;
            };
        }

        // ── Parameter binder builder ──────────────────────────────────────────────

        /// <summary>
        /// Returns a function that, given an execution context, extracts and converts
        /// the value for one method parameter. The function is built once at startup
        /// and called per request.
        /// </summary>
        private static Func<IExecutionContext, object?> BuildParameterBinder(
            ParameterInfo param, string routeTemplate)
        {
            var paramType = param.ParameterType;
            var paramName = param.Name!;
            var hasDefault = param.HasDefaultValue;
            var defaultValue = param.DefaultValue;

            // Unwrap Nullable<T>
            var underlyingNullable = Nullable.GetUnderlyingType(paramType);
            var effectiveType = underlyingNullable ?? paramType;

            // Check for [Validated] on the effective type
            var validatedAttr = effectiveType.GetCustomAttribute<ValidatedAttribute>();

            // Route parameter?
            var isRouteParam = IsRouteParameter(paramName, routeTemplate);

            if (isRouteParam)
            {
                return ctx =>
                {
                    if (!ctx.Request.RouteParameters.TryGetValue(paramName, out var raw))
                    {
                        if (hasDefault) return defaultValue;
                        throw new ValidationException(
                            $"Missing_{paramName}",
                            $"Route parameter '{paramName}' is missing.");
                    }
                    return ConvertValue(raw, effectiveType, validatedAttr, paramName);
                };
            }

            if (IsSimpleType(effectiveType) || validatedAttr is not null)
            {
                // Simple/validated → query string
                return ctx =>
                {
                    if (!ctx.Request.QueryParameters.TryGetValue(paramName, out var raw)
                        || string.IsNullOrEmpty(raw))
                    {
                        if (hasDefault) return defaultValue;
                        if (underlyingNullable is not null) return null;
                        throw new ValidationException(
                            $"Missing_{paramName}",
                            $"Required query parameter '{paramName}' is missing.");
                    }
                    return ConvertValue(raw, effectiveType, validatedAttr, paramName);
                };
            }

            // Complex types (body binding) are not yet implemented.
            // Fail at startup so the error is caught immediately.
            throw new NotSupportedException(
                $"Parameter '{paramName}' on method '{param.Member.Name}' has type '{paramType.Name}' " +
                $"which is not a simple type or [Validated] type. " +
                $"Body binding for complex parameters is not yet supported.");
        }

        // ── Conversion helpers ────────────────────────────────────────────────────

        private static bool IsRouteParameter(string paramName, string routeTemplate)
            => routeTemplate.Contains($"{{{paramName}}}", StringComparison.OrdinalIgnoreCase);

        private static bool IsSimpleType(Type type)
            => type.IsEnum || SimpleTypes.Contains(type);

        private static object? ConvertValue(
            string raw, Type targetType, ValidatedAttribute? validatedAttr, string paramName)
        {
            if (validatedAttr is not null)
                return InvokeValidatedTryCreate(raw, targetType, paramName);

            if (targetType == typeof(string))
                return raw;

            if (targetType.IsEnum)
                return Enum.Parse(targetType, raw, ignoreCase: true);

            // Use TypeDescriptor/IConvertible for common primitives
            try
            {
                return Convert.ChangeType(raw, targetType,
                    System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                throw new ValidationException(
                    $"Invalid_{paramName}",
                    $"Cannot convert '{raw}' to {targetType.Name}.",
                    ex);
            }
        }

        /// <summary>
        /// Calls the static TryCreate(string, out T) method on a [Validated] type.
        /// Throws ValidationException if the type doesn't have such a method or if validation fails.
        /// </summary>
        private static object? InvokeValidatedTryCreate(string raw, Type validatedType, string paramName)
        {
            // Find static TryCreate(string?, out TValidated) → bool
            var tryCreate = validatedType
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                    m.Name == "TryCreate" &&
                    m.ReturnType == typeof(bool) &&
                    m.GetParameters().Length == 2 &&
                    m.GetParameters()[1].IsOut);

            if (tryCreate is null)
                throw new InvalidOperationException(
                    $"Type '{validatedType.Name}' is decorated with [Validated] but has no " +
                    $"static TryCreate(string, out {validatedType.Name}) method.");

            var args = new object?[] { raw, null };
            var success = (bool)tryCreate.Invoke(null, args)!;

            if (!success)
                throw new ValidationException(
                    $"Invalid_{validatedType.Name}",
                    $"The value '{raw}' is not a valid {validatedType.Name}.");

            return args[1]; // the out parameter holds the validated value
        }
    }
}
