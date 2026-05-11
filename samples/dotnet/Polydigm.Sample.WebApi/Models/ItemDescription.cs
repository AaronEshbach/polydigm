using Polydigm.Metadata;
using Polydigm.Validation;

namespace Polydigm.Sample.WebApi.Models
{
    /// <summary>
    /// A validated description string for an item.
    ///
    /// Follows the Polydigm validated-primitive pattern:
    ///   Create    — throws ValidationException; is the authoritative source of error specificity.
    ///   Validate  — wraps Create via Validation.Try; returns a discriminated union result.
    ///   TryCreate — derived from Validate; classic bool-returning variant for convenience.
    ///   FromOptional — helper for optional query string parameters; returns null on empty input.
    ///
    /// The parameter binder in ServiceDiscovery discovers Validate via reflection and calls
    /// it automatically for [Validated] parameters, propagating the specific error message
    /// from Create as a 400 response when validation fails.
    /// </summary>
    [Validated]
    public readonly record struct ItemDescription
    {
        [MaxLength]
        private const int MaxLength = 200;

        private readonly string value;

        public string Value => value;

        private ItemDescription(string value)
        {
            this.value = value;
        }

        public static ItemDescription Create(string input)
        {
            if (input.Length > MaxLength)
                throw new ValidationException<string, ItemDescription>(input);

            return new ItemDescription(input);
        }

        [Validation]
        public static ValidationResult<ItemDescription> Validate(string input)
            => Validator.Try(Create, input);

        public static bool TryCreate(string input, out ItemDescription validated)
        {
            var result = Validate(input);
            if (result is ValidatedResult<ItemDescription>.Valid valid)
            {
                validated = valid.Model;
                return true;
            }

            validated = default;
            return false;
        }

        /// <summary>
        /// Returns null when <paramref name="input"/> is null or empty, otherwise Creates.
        /// Use this for optional query string parameters.
        /// </summary>
        public static ItemDescription? FromOptional(string? input = null)
        {
            if (string.IsNullOrEmpty(input))
                return null;

            return Create(input);
        }

        public override string ToString() => value;
    }
}
