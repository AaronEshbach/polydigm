using Polydigm.Metadata;
using Polydigm.Validation;

namespace Polydigm.Sample.WebApi.Models
{
    /// <summary>
    /// A validated description string for an item.
    /// Ensures the value is non-empty and within the allowed length.
    ///
    /// Following the Polydigm validated-primitive pattern:
    ///   - Private constructor — can only be created via TryCreate / Create.
    ///   - Static TryCreate discovered by the parameter binder for automatic query-string validation.
    ///   - If the query string value fails validation the pipeline returns 400 automatically.
    /// </summary>
    [Validated]
    public readonly record struct ItemDescription
    {
        [MaxLength]
        private const int MaxLength = 200;

        [Required]
        private const bool IsRequired = true;

        private readonly string value;

        public string Value => value;

        private ItemDescription(string value)
        {
            this.value = value;
        }

        public static bool TryCreate(string? input, out ItemDescription validated)
        {
            if (!string.IsNullOrWhiteSpace(input) && input.Length <= MaxLength)
            {
                validated = new ItemDescription(input);
                return true;
            }

            validated = default;
            return false;
        }

        [Validation]
        public static ItemDescription Create(string? input)
        {
            if (TryCreate(input, out var validated))
                return validated;

            throw new ValidationException<string?, ItemDescription>(input);
        }

        public override string ToString() => value;
    }
}
