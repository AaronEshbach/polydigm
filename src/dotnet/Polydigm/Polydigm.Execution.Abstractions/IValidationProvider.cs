namespace Polydigm.Execution
{
    /// <summary>
    /// Validates DTOs and converts them to validated domain models.
    /// Uses the Polydigm validation pattern (TryCreate/Create methods).
    /// </summary>
    public interface IValidationProvider
    {
        /// <summary>
        /// Validates a DTO and attempts to convert it to a validated model.
        /// </summary>
        /// <param name="dto">The unvalidated DTO.</param>
        /// <param name="targetType">The target validated model type.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Validation result with validated model or errors.</returns>
        Task<ValidationResult> ValidateAsync(object? dto, Type targetType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a DTO and attempts to convert it to a validated model.
        /// </summary>
        /// <typeparam name="TDto">The DTO type.</typeparam>
        /// <typeparam name="TValidated">The validated model type.</typeparam>
        /// <param name="dto">The unvalidated DTO.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Validation result with validated model or errors.</returns>
        Task<ValidationResult<TValidated>> ValidateAsync<TDto, TValidated>(TDto? dto, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of a validation operation.
    /// </summary>
    public sealed class ValidationResult
    {
        /// <summary>
        /// Whether validation succeeded.
        /// </summary>
        public bool IsValid { get; init; }

        /// <summary>
        /// The validated model (only set if IsValid is true).
        /// </summary>
        public object? ValidatedValue { get; init; }

        /// <summary>
        /// Validation errors (empty if IsValid is true).
        /// </summary>
        public IReadOnlyList<ValidationError> Errors { get; init; } = Array.Empty<ValidationError>();

        public static ValidationResult Success(object validatedValue)
            => new() { IsValid = true, ValidatedValue = validatedValue };

        public static ValidationResult Failure(params ValidationError[] errors)
            => new() { IsValid = false, Errors = errors };

        public static ValidationResult Failure(IEnumerable<ValidationError> errors)
            => new() { IsValid = false, Errors = errors.ToList() };
    }

    /// <summary>
    /// Strongly-typed validation result.
    /// </summary>
    /// <typeparam name="T">The validated model type.</typeparam>
    public sealed class ValidationResult<T>
    {
        /// <summary>
        /// Whether validation succeeded.
        /// </summary>
        public bool IsValid { get; init; }

        /// <summary>
        /// The validated model (only set if IsValid is true).
        /// </summary>
        public T? ValidatedValue { get; init; }

        /// <summary>
        /// Validation errors (empty if IsValid is true).
        /// </summary>
        public IReadOnlyList<ValidationError> Errors { get; init; } = Array.Empty<ValidationError>();

        public static ValidationResult<T> Success(T validatedValue)
            => new() { IsValid = true, ValidatedValue = validatedValue };

        public static ValidationResult<T> Failure(params ValidationError[] errors)
            => new() { IsValid = false, Errors = errors };

        public static ValidationResult<T> Failure(IEnumerable<ValidationError> errors)
            => new() { IsValid = false, Errors = errors.ToList() };
    }

    /// <summary>
    /// Represents a validation error.
    /// </summary>
    public sealed class ValidationError
    {
        /// <summary>
        /// The field or property path that failed validation (e.g., "email", "address.zipCode").
        /// </summary>
        public string? Field { get; init; }

        /// <summary>
        /// Human-readable error message.
        /// </summary>
        public string Message { get; init; } = string.Empty;

        /// <summary>
        /// Error code for programmatic handling (e.g., "INVALID_EMAIL", "OUT_OF_RANGE").
        /// </summary>
        public string? Code { get; init; }

        /// <summary>
        /// The invalid value that caused the error.
        /// </summary>
        public object? AttemptedValue { get; init; }

        public ValidationError(string message, string? field = null, string? code = null, object? attemptedValue = null)
        {
            Message = message;
            Field = field;
            Code = code;
            AttemptedValue = attemptedValue;
        }
    }
}
