namespace Polydigm.Validation
{
    /// <summary>
    /// Contains the two concrete subtypes of <see cref="ValidationResult{TModel}"/>.
    ///
    /// Using a static outer class rather than nesting inside <c>ValidationResult&lt;T&gt;</c>
    /// keeps match-expression syntax readable:
    ///
    ///   result is ValidatedResult&lt;Foo&gt;.Valid valid
    ///   result is ValidatedResult&lt;Foo&gt;.Invalid invalid
    /// </summary>
    public static class ValidatedResult<TModel>
    {
        /// <summary>
        /// The success case — the input passed all validation rules and was converted to
        /// the validated type. Access the result via <see cref="Model"/>.
        /// </summary>
        public sealed class Valid : ValidationResult<TModel>
        {
            public TModel Model { get; }

            public Valid(TModel model)
            {
                Model = model;
            }

            public override bool IsValid => true;
            public override object? UntypedModel => Model;
            public override string? ErrorMessage => null;
            public override Exception? Exception => null;
        }

        /// <summary>
        /// The failure case — the input failed at least one validation rule.
        /// The originating exception is available via <see cref="Error"/> and its message
        /// via <see cref="ErrorMessage"/>, preserving all specificity from the throwing
        /// <c>Create</c> method without requiring callers to catch exceptions.
        /// </summary>
        public sealed class Invalid : ValidationResult<TModel>
        {
            public ValidationException Error { get; }

            public Invalid(ValidationException error)
            {
                Error = error;
            }

            public override bool IsValid => false;
            public override object? UntypedModel => null;
            public override string? ErrorMessage => Error.Message;
            public override Exception? Exception => Error;
        }
    }
}
