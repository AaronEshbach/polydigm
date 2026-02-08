namespace PetStore.Models.DTO
{
    /// <summary>
    /// A pet in the store
    /// </summary>
    public record Pet
    {
        /// <summary>
        /// Unique identifier for a pet
        /// </summary>
        public string? Id { get; init; }

        /// <summary>
        /// Name of the pet
        /// </summary>
        public string? Name { get; init; }

        /// <summary>
        /// Type of pet
        /// </summary>
        public string? Type { get; init; }

        /// <summary>
        /// Age of the pet in years
        /// </summary>
        public int? Age { get; init; }

    }
}
