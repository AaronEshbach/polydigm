namespace Polydigm.Specifications
{
    /// <summary>
    /// Factory methods for creating specification sources.
    /// </summary>
    public static class SpecificationSource
    {
        public static ISpecificationSource FromFile(string filePath)
            => new FileSpecificationSource(filePath);

        public static ISpecificationSource FromString(string content, string displayName = "string")
            => new StringSpecificationSource(content, displayName);

        public static ISpecificationSource FromUrl(string url)
            => new UrlSpecificationSource(url);

        public static ISpecificationSource FromStream(Stream stream, string displayName = "stream")
            => new StreamSpecificationSource(stream, displayName);
    }

    /// <summary>
    /// Specification source from a file.
    /// </summary>
    public sealed class FileSpecificationSource : ISpecificationSource
    {
        private readonly string filePath;

        public FileSpecificationSource(string filePath)
        {
            this.filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        }

        public SpecificationSourceType SourceType => SpecificationSourceType.File;
        public string DisplayName => filePath;

        public async Task<string> ReadAsStringAsync(CancellationToken cancellationToken = default)
        {
#if NETSTANDARD2_0
            return await Task.Run(() => File.ReadAllText(filePath), cancellationToken);
#else
            return await File.ReadAllTextAsync(filePath, cancellationToken);
#endif
        }

        public Task<Stream> ReadAsStreamAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Stream>(File.OpenRead(filePath));
        }
    }

    /// <summary>
    /// Specification source from a string.
    /// </summary>
    public sealed class StringSpecificationSource : ISpecificationSource
    {
        private readonly string content;
        private readonly string displayName;

        public StringSpecificationSource(string content, string displayName = "string")
        {
            this.content = content ?? throw new ArgumentNullException(nameof(content));
            this.displayName = displayName;
        }

        public SpecificationSourceType SourceType => SpecificationSourceType.String;
        public string DisplayName => displayName;

        public Task<string> ReadAsStringAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(content);
        }

        public Task<Stream> ReadAsStreamAsync(CancellationToken cancellationToken = default)
        {
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            return Task.FromResult<Stream>(stream);
        }
    }

    /// <summary>
    /// Specification source from a URL.
    /// </summary>
    public sealed class UrlSpecificationSource : ISpecificationSource
    {
        private readonly string url;
        private static readonly HttpClient httpClient = new();

        public UrlSpecificationSource(string url)
        {
            this.url = url ?? throw new ArgumentNullException(nameof(url));
        }

        public SpecificationSourceType SourceType => SpecificationSourceType.Url;
        public string DisplayName => url;

        public async Task<string> ReadAsStringAsync(CancellationToken cancellationToken = default)
        {
            return await httpClient.GetStringAsync(url
#if !NETSTANDARD2_0
                , cancellationToken
#endif
            );
        }

        public async Task<Stream> ReadAsStreamAsync(CancellationToken cancellationToken = default)
        {
            return await httpClient.GetStreamAsync(url
#if !NETSTANDARD2_0
                , cancellationToken
#endif
            );
        }
    }

    /// <summary>
    /// Specification source from a stream.
    /// </summary>
    public sealed class StreamSpecificationSource : ISpecificationSource
    {
        private readonly Stream stream;
        private readonly string displayName;

        public StreamSpecificationSource(Stream stream, string displayName = "stream")
        {
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
            this.displayName = displayName;
        }

        public SpecificationSourceType SourceType => SpecificationSourceType.Stream;
        public string DisplayName => displayName;

        public async Task<string> ReadAsStringAsync(CancellationToken cancellationToken = default)
        {
#if NETSTANDARD2_0
            using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
            return await reader.ReadToEndAsync();
#else
            using var reader = new StreamReader(stream, leaveOpen: true);
            return await reader.ReadToEndAsync(cancellationToken);
#endif
        }

        public Task<Stream> ReadAsStreamAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(stream);
        }
    }
}
