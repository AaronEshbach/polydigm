namespace Polydigm.Validation
{
    /// <summary>
    /// <summary>
    /// Static helpers for invoking a validated type's <c>Create</c> method and capturing
    /// any <see cref="ValidationException"/> it throws into a <see cref="ValidationResult{TModel}"/>
    /// discriminated union instead of propagating it as an exception.
    ///
    /// Named <c>Validator</c> (not <c>Validation</c>) to avoid conflicting with the
    /// <c>Polydigm.Validation</c> namespace when used via a <c>using</c> directive.
    ///
    /// Intended usage inside a validated type's <c>Validate</c> method:
    ///
    ///   public static ValidationResult&lt;Foo&gt; Validate(string input)
    ///       => Validator.Try(Create, input);
    ///
    /// Only <see cref="ValidationException"/> is caught; all other exceptions propagate
    /// normally so unexpected errors (null refs, IO failures, etc.) are never swallowed.
    /// </summary>
    public static class Validator
    {
        /// <summary>
        /// Invokes <paramref name="factory"/> and returns a <see cref="ValidatedResult{TModel}.Valid"/>
        /// on success or a <see cref="ValidatedResult{TModel}.Invalid"/> if a
        /// <see cref="ValidationException"/> is thrown.
        /// </summary>
        public static ValidationResult<TModel> Try<TModel>(Func<TModel> factory)
        {
            try
            {
                return new ValidatedResult<TModel>.Valid(factory());
            }
            catch (ValidationException ex)
            {
                return new ValidatedResult<TModel>.Invalid(ex);
            }
        }

        /// <summary>
        /// Convenience overload that partially applies <paramref name="input"/> to a
        /// single-argument <paramref name="factory"/>.
        ///
        /// Equivalent to <c>Try(() => factory(input))</c>.
        /// Allows the common pattern:
        ///   <c>Validator.Try(Create, input)</c>
        /// </summary>
        public static ValidationResult<TModel> Try<TInput, TModel>(
            Func<TInput, TModel> factory,
            TInput input)
            => Try(() => factory(input));
    }
}

