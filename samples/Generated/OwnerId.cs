using Polydigm.Metadata;
using System.Text.RegularExpressions;

namespace PetStore.Models
{
    /// <summary>
    /// Unique identifier for an owner
    /// </summary>
    [Validated]
    public readonly record struct OwnerId
    {
        [Pattern]
        private static readonly Regex Pattern = new(@"^OWN-[0-9]{6}$", RegexOptions.Compiled);

        private readonly string value;

        public string Value => value;

        private OwnerId(string value)
        {
            this.value = value;
        }

        public static bool TryCreate(string? input, out OwnerId validated)
        {
            if (input is not null && Pattern.IsMatch(input))
            {
                validated = new OwnerId(input);
                return true;
            }

            validated = default;
            return false;
        }

        [Validation]
        public static OwnerId Create(string? input)
        {
            if (TryCreate(input, out var validated))
            {
                return validated;
            }

            throw new ValidationException<string?, OwnerId>(input);
        }

        public override string ToString() => value;
    }
}
