namespace Polydigm.Sample.WebApi.Services
{
    public sealed record Item(
        string Id,
        string Name,
        string Description,
        DateTime CreatedAt);

    public interface ISampleRepository
    {
        IReadOnlyList<Item> Items { get; }
    }

    /// <summary>
    /// In-memory repository seeded with a small fixed data set.
    /// Registered as a singleton in Program.cs.
    /// </summary>
    public sealed class InMemorySampleRepository : ISampleRepository
    {
        private static readonly List<Item> _items =
        [
            new("1", "Widget A",  "A standard widget",  DateTime.UtcNow.AddDays(-10)),
            new("2", "Widget B",  "An advanced widget", DateTime.UtcNow.AddDays(-5)),
            new("3", "Gadget X",  "A premium gadget",   DateTime.UtcNow.AddDays(-2)),
            new("4", "Gadget Y",  "A budget gadget",    DateTime.UtcNow.AddDays(-1)),
        ];

        public IReadOnlyList<Item> Items => _items;
    }
}
