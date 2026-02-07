using Polydigm.Metadata;

namespace PetStore.Models
{
    /// <summary>
    /// Name of the pet
    /// </summary>
    [Validated]
    public readonly record struct PetName
    {
        [MaxLength]
        private const int MaxLength = 50;

        private readonly string value;

        public string Value => value;

        private PetName(string value)
        {
            this.value = value;
        }

        public static bool TryCreate(string? input, out PetName validated)
        {
            if (input is not null && input.Length <= MaxLength)
            {
                validated = new PetName(input);
                return true;
            }

            validated = default;
            return false;
        }

        [Validation]
        public static PetName Create(string? input)
        {
            if (TryCreate(input, out var validated))
            {
                return validated;
            }

            throw new ValidationException<string?, PetName>(input);
        }

        public override string ToString() => value;
    }
}
