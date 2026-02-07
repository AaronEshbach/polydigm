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
        public string? id { get; init; }

        /// <summary>
        /// Email address
        /// </summary>
        public string? email { get; init; }

        /// <summary>
        /// Name of the pet
        /// </summary>
        public string? name { get; init; }

    }
}
