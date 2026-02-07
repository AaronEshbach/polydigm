namespace Polydigm.CodeGeneration.CSharp
{
    /// <summary>
    /// Code generation target for C#.
    /// </summary>
    public sealed class CSharpCodeGenerationTarget : ICodeGenerationTarget
    {
        public CSharpCodeGenerationTarget(string? languageVersion = null, string? targetFramework = null)
        {
            LanguageVersion = languageVersion ?? "10.0";
            TargetFramework = targetFramework ?? ".NET 10";
        }

        public string Language => "C#";
        public string? LanguageVersion { get; }
        public string? TargetFramework { get; }
        public string DisplayName => $"{Language} {LanguageVersion} ({TargetFramework})";
        public string FileExtension => ".cs";

        /// <summary>
        /// C# 10.0 targeting .NET 10.
        /// </summary>
        public static CSharpCodeGenerationTarget Default => new();

        /// <summary>
        /// C# with custom version and framework.
        /// </summary>
        public static CSharpCodeGenerationTarget Create(string languageVersion, string targetFramework)
            => new(languageVersion, targetFramework);
    }
}
