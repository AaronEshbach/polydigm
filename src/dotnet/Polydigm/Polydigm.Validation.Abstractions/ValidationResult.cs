namespace Polydigm.Validation
{
    /// <summary>
    /// Non-generic interface for a validation result.
    /// Used by infrastructure (e.g. service discovery / parameter binding) that needs to
    /// inspect results without knowing the concrete validated type at compile time.
    /// </summary>
    public interface IValidationResult
    {
        bool IsValid { get; }

        /// <summary>The validated model as an untyped object; null when IsValid is false.</summary>
        object? UntypedModel { get; }

        /// <summary>The human-readable error message; null when IsValid is true.</summary>
        string? ErrorMessage { get; }

        /// <summary>The underlying exception; null when IsValid is true.</summary>
        Exception? Exception { get; }
    }

    /// <summary>
    /// A discriminated union representing the outcome of validating an input value.
    ///
    /// Subtypes live in <see cref="ValidatedResult{TModel}"/> so that match expressions
    /// read naturally:
    ///
    ///   if (result is ValidatedResult&lt;Foo&gt;.Valid valid)
    ///       Use(valid.Model);
    ///   else if (result is ValidatedResult&lt;Foo&gt;.Invalid invalid)
    ///       Log(invalid.ErrorMessage);
    ///
    /// Obtain instances through <c>Validation.Try(...)</c> rather than constructing directly.
    /// </summary>
    public abstract class ValidationResult<TModel> : IValidationResult
    {
        public abstract bool IsValid { get; }
        public abstract object? UntypedModel { get; }
        public abstract string? ErrorMessage { get; }
        public abstract Exception? Exception { get; }
    }
}
