using Polydigm.Metadata;
using System.Text.RegularExpressions;

namespace PetStore.Models
{
    /// <summary>
    /// Unique identifier for a pet
    /// </summary>
    [Validated]
    public readonly record struct PetId
    {
        [Pattern]
        private static readonly Regex Pattern = new(@"^PET-[0-9]{6}$", RegexOptions.Compiled);

        private readonly string value;

        public string Value => value;

        private PetId(string value)
        {
            this.value = value;
        }

        public static bool TryCreate(string? input, out PetId validated)
        {
            if (input is not null && Pattern.IsMatch(input))
            {
                validated = new PetId(input);
                return true;
            }

            validated = default;
            return false;
        }

        [Validation]
        public static PetId Create(string? input)
        {
            if (TryCreate(input, out var validated))
            {
                return validated;
            }

            throw new ValidationException<string?, PetId>(input);
        }

        public override string ToString() => value;
    }
}
