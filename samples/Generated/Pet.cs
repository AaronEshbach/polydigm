using Polydigm.Metadata;

namespace PetStore.Models
{
    /// <summary>
    /// A pet in the store
    /// </summary>
    [Validated(typeof(PetStore.Models.DTO.Pet))]
    public sealed record Pet
    {
        /// <summary>
        /// Unique identifier for a pet
        /// </summary>
        public required PetId id { get; init; }

        /// <summary>
        /// Name of the pet
        /// </summary>
        public required PetName name { get; init; }

        /// <summary>
        /// Type of pet
        /// </summary>
        public required PetType type { get; init; }

        /// <summary>
        /// Age of the pet in years
        /// </summary>
        public PetAge age { get; init; }

        public static bool TryCreate(PetStore.Models.DTO.Pet dto, out Pet? validated)
        {
            if (
        PetId.TryCreate(dto.id, out var id) &&
        PetName.TryCreate(dto.name, out var name) &&
        PetType.TryCreate(dto.type, out var type) &&
        PetAge.TryCreate(dto.age, out var age))
            {
                validated = new Pet
                {
                    id = id,
                    name = name,
                    type = type,
                    age = age,
                };

                return true;
            }

            validated = default;
            return false;
        }

        [Validation]
        public static Pet Create(PetStore.Models.DTO.Pet dto)
        {
            return new Pet
            {
                id = PetId.Create(dto.id),
                name = PetName.Create(dto.name),
                type = PetType.Create(dto.type),
                age = PetAge.Create(dto.age),
            };
        }

        public static PetStore.Models.DTO.Pet ToDTO(Pet model)
        {
            return new PetStore.Models.DTO.Pet
            {
                id = model.id.Value,
                name = model.name.Value,
                type = model.type.Value,
                age = model.age.Value,
            };
        }
    }
}
