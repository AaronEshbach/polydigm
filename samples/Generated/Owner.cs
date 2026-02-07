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
        public required OwnerId id { get; init; }

        /// <summary>
        /// Email address
        /// </summary>
        public required Email email { get; init; }

        /// <summary>
        /// Name of the pet
        /// </summary>
        public PetName name { get; init; }

        public static bool TryCreate(PetStore.Models.DTO.Owner dto, out Owner? validated)
        {
            if (
        OwnerId.TryCreate(dto.id, out var id) &&
        Email.TryCreate(dto.email, out var email) &&
        PetName.TryCreate(dto.name, out var name))
            {
                validated = new Owner
                {
                    id = id,
                    email = email,
                    name = name,
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
                id = OwnerId.Create(dto.id),
                email = Email.Create(dto.email),
                name = PetName.Create(dto.name),
            };
        }

        public static PetStore.Models.DTO.Owner ToDTO(Owner model)
        {
            return new PetStore.Models.DTO.Owner
            {
                id = model.id.Value,
                email = model.email.Value,
                name = model.name.Value,
            };
        }
    }
}
