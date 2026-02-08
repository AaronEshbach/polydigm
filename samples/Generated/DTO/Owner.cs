namespace PetStore.Models.DTO
{
    /// <summary>
    /// Owner of a pet
    /// </summary>
    public record Owner
    {
        /// <summary>
        /// Unique identifier for an owner
        /// </summary>
        public string? Id { get; init; }

        /// <summary>
        /// Email address
        /// </summary>
        public string? Email { get; init; }

        /// <summary>
        /// Name of the pet
        /// </summary>
        public string? Name { get; init; }

    }
}
