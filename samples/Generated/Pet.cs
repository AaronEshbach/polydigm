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
        public required PetId Id { get; init; }

        /// <summary>
        /// Name of the pet
        /// </summary>
        public required PetName Name { get; init; }

        /// <summary>
        /// Type of pet
        /// </summary>
        public required PetType Type { get; init; }

        /// <summary>
        /// Age of the pet in years
        /// </summary>
        public PetAge Age { get; init; }

        public static bool TryCreate(PetStore.Models.DTO.Pet dto, out Pet? validated)
        {
            if (
        PetId.TryCreate(dto.Id, out var id) &&
        PetName.TryCreate(dto.Name, out var name) &&
        PetType.TryCreate(dto.Type, out var type) &&
        PetAge.TryCreate(dto.Age, out var age))
            {
                validated = new Pet
                {
                    Id = id,
                    Name = name,
                    Type = type,
                    Age = age,
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
                Id = PetId.Create(dto.Id),
                Name = PetName.Create(dto.Name),
                Type = PetType.Create(dto.Type),
                Age = PetAge.Create(dto.Age),
            };
        }

        public static PetStore.Models.DTO.Pet ToDTO(Pet model)
        {
            return new PetStore.Models.DTO.Pet
            {
                Id = model.Id.Value,
                Name = model.Name.Value,
                Type = model.Type.Value,
                Age = model.Age.Value,
            };
        }
    }
}
