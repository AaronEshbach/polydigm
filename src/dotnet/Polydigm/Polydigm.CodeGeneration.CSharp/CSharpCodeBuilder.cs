using System.Text;

namespace Polydigm.CodeGeneration.CSharp
{
    /// <summary>
    /// Helper for building C# source code with proper indentation.
    /// </summary>
    public sealed class CSharpCodeBuilder
    {
        private readonly StringBuilder sb = new();
        private int indentLevel = 0;
        private const string IndentString = "    "; // 4 spaces

        public CSharpCodeBuilder AppendLine(string? line = null)
        {
            if (line == null)
            {
                sb.AppendLine();
                return this;
            }

            // Handle closing braces - dedent first
            if (line.TrimStart().StartsWith("}"))
            {
                indentLevel = Math.Max(0, indentLevel - 1);
            }

            // Add indentation
            for (int i = 0; i < indentLevel; i++)
            {
                sb.Append(IndentString);
            }

            sb.AppendLine(line);

            // Handle opening braces - indent after
            if (line.TrimEnd().EndsWith("{"))
            {
                indentLevel++;
            }

            return this;
        }

        public CSharpCodeBuilder Append(string text)
        {
            sb.Append(text);
            return this;
        }

        public CSharpCodeBuilder AppendLineIf(bool condition, string line)
        {
            if (condition)
            {
                AppendLine(line);
            }
            return this;
        }

        public CSharpCodeBuilder AppendXmlDoc(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return this;

            AppendLine("/// <summary>");
            foreach (var line in description!.Split('\n'))
            {
                AppendLine($"/// {line.TrimEnd()}");
            }
            AppendLine("/// </summary>");
            return this;
        }

        public CSharpCodeBuilder OpenBlock(string line)
        {
            AppendLine(line);
            return this;
        }

        public CSharpCodeBuilder CloseBlock()
        {
            AppendLine("}");
            return this;
        }

        public override string ToString() => sb.ToString();
    }
}
