using Polydigm.Metadata;

namespace PetStore.Models
{
    /// <summary>
    /// Owner of a pet
    /// </summary>
    [Validated(typeof(PetStore.Models.DTO.Owner))]
    public sealed record Owner
    {
        /// <summary>
        /// Unique identifier for an owner
        /// </summary>
        public required OwnerId Id { get; init; }

        /// <summary>
        /// Email address
        /// </summary>
        public required Email Email { get; init; }

        /// <summary>
        /// Name of the pet
        /// </summary>
        public PetName Name { get; init; }

        public static bool TryCreate(PetStore.Models.DTO.Owner dto, out Owner? validated)
        {
            if (
        OwnerId.TryCreate(dto.Id, out var id) &&
        Email.TryCreate(dto.Email, out var email) &&
        PetName.TryCreate(dto.Name, out var name))
            {
                validated = new Owner
                {
                    Id = id,
                    Email = email,
                    Name = name,
                };

                return true;
            }

            validated = default;
            return false;
        }

        [Validation]
        public static Owner Create(PetStore.Models.DTO.Owner dto)
        {
            return new Owner
            {
                Id = OwnerId.Create(dto.Id),
                Email = Email.Create(dto.Email),
                Name = PetName.Create(dto.Name),
            };
        }

        public static PetStore.Models.DTO.Owner ToDTO(Owner model)
        {
            return new PetStore.Models.DTO.Owner
            {
                Id = model.Id.Value,
                Email = model.Email.Value,
                Name = model.Name.Value,
            };
        }
    }
}
