using Polydigm.Metadata;

namespace PetStore.Models
{
    /// <summary>
    /// Age of the pet in years
    /// </summary>
    [Validated]
    public readonly record struct PetAge
    {
        [Minimum]
        private const int Minimum = 0;

        [Maximum]
        private const int Maximum = 50;

        private readonly int value;

        public int Value => value;

        private PetAge(int value)
        {
            this.value = value;
        }

        public static bool TryCreate(int input, out PetAge validated)
        {
            if (input >= Minimum && input <= Maximum)
            {
                validated = new PetAge(input);
                return true;
            }

            validated = default;
            return false;
        }

        [Validation]
        public static PetAge Create(int input)
        {
            if (TryCreate(input, out var validated))
            {
                return validated;
            }

            throw new ValidationException<int, PetAge>(input);
        }

        public override string ToString() => value.ToString();
    }
}
