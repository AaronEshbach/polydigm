using Polydigm.Metadata;
using System.Text.RegularExpressions;

namespace Polydigm.Validation.Tests
{
    [Validated]
    public readonly record struct TestId
    {
        [Pattern]
        private static readonly Regex Pattern = new(@"^[a-zA-Z0-9]{6}-[a-zA-Z0-9]{6}$", RegexOptions.Compiled);

        private readonly string value;

        public string Value => value;

        private TestId(string value)
        {
            this.value = value;
        }

        [Validation]
        public static bool TryCreate(string input, out TestId validated)
        {
            if (Pattern.IsMatch(input))
            {
                validated = new TestId(input);
                return true;
            }
            
            validated = default;
            return false;
        }

        public static TestId Create(string input)
        {
            if (TryCreate(input, out var validated))
            {
                return validated;
            }

            throw new ValidationException<string, TestId>(input);
        }

        public override string ToString() => value;        
    }
}
