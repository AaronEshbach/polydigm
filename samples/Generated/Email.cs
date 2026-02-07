using Polydigm.Metadata;
using System.Text.RegularExpressions;

namespace PetStore.Models
{
    /// <summary>
    /// Email address
    /// </summary>
    [Validated]
    public readonly record struct Email
    {
        [Pattern]
        private static readonly Regex Pattern = new(@"^[^@]+@[^@]+\.[^@]+$", RegexOptions.Compiled);

        private readonly string value;

        public string Value => value;

        private Email(string value)
        {
            this.value = value;
        }

        public static bool TryCreate(string? input, out Email validated)
        {
            if (input is not null && Pattern.IsMatch(input))
            {
                validated = new Email(input);
                return true;
            }

            validated = default;
            return false;
        }

        [Validation]
        public static Email Create(string? input)
        {
            if (TryCreate(input, out var validated))
            {
                return validated;
            }

            throw new ValidationException<string?, Email>(input);
        }

        public override string ToString() => value;
    }
}
