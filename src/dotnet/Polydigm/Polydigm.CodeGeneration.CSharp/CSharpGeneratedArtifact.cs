namespace Polydigm.CodeGeneration.CSharp
{
    /// <summary>
    /// Represents a generated C# source file.
    /// </summary>
    public sealed class CSharpGeneratedArtifact : IGeneratedArtifact
    {
        public CSharpGeneratedArtifact(
            string name,
            string content,
            string? relativePath = null,
            ArtifactType type = ArtifactType.File,
            ICodeGenerationTarget? target = null)
        {
            Name = name;
            Content = content;
            RelativePath = relativePath ?? name;
            Type = type;
            Target = target ?? CSharpCodeGenerationTarget.Default;
        }

        public ArtifactType Type { get; }
        public string Name { get; }
        public string RelativePath { get; }
        public string Content { get; }
        public ICodeGenerationTarget Target { get; }

        public void WriteToDisk(string baseDirectory)
        {
            var fullPath = Path.Combine(baseDirectory, RelativePath);
            var directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, Content);
        }

        public async Task WriteToDiskAsync(string baseDirectory, CancellationToken cancellationToken = default)
        {
            var fullPath = Path.Combine(baseDirectory, RelativePath);
            var directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

#if NETSTANDARD2_0
            File.WriteAllText(fullPath, Content);
            await Task.CompletedTask;
#else
            await File.WriteAllTextAsync(fullPath, Content, cancellationToken);
#endif
        }
    }
}
