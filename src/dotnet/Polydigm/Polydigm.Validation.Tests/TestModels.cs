using Polydigm.Metadata;
using System.Text.RegularExpressions;

namespace Polydigm.Validation.Tests
{
    // ── TestId ────────────────────────────────────────────────────────────────────

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

        /// <summary>
        /// Creates a validated TestId. Throws on invalid input — the authoritative
        /// place for validation logic. All error specificity originates here.
        /// </summary>
        public static TestId Create(string? input)
        {
            if (input is not null && Pattern.IsMatch(input))
                return new TestId(input);

            throw new ValidationException<string?, TestId>(input);
        }

        /// <summary>
        /// Validates <paramref name="input"/> and returns a discriminated union result.
        /// Callers that need error details without catching exceptions use this method.
        /// Preferred over Create for infrastructure callers (e.g. parameter binders).
        /// </summary>
        [Validation]
        public static ValidationResult<TestId> Validate(string? input)
            => Validator.Try(Create, input);

        /// <summary>
        /// Convenience bool-returning variant, derived from <see cref="Validate"/>.
        /// </summary>
        public static bool TryCreate(string? input, out TestId validated)
        {
            var result = Validate(input);
            if (result is ValidatedResult<TestId>.Valid valid)
            {
                validated = valid.Model;
                return true;
            }

            validated = default;
            return false;
        }

        public override string ToString() => value;
    }

    // ── TestType ──────────────────────────────────────────────────────────────────

    [Validated]
    public readonly record struct TestType
    {
        [Enum]
        public enum TestTypeEnum
        {
            TypeA,
            TypeB,
            TypeC
        }

        private readonly TestTypeEnum value;

        public TestTypeEnum Value => value;

        private TestType(TestTypeEnum value)
        {
            this.value = value;
        }

        public static TestType Create(string? input)
        {
            if (input is not null && Enum.TryParse<TestTypeEnum>(input, out var enumValue))
                return new TestType(enumValue);

            throw new ValidationException<string?, TestType>(input);
        }

        [Validation]
        public static ValidationResult<TestType> Validate(string? input)
            => Validator.Try(Create, input);

        public static bool TryCreate(string? input, out TestType validated)
        {
            var result = Validate(input);
            if (result is ValidatedResult<TestType>.Valid valid)
            {
                validated = valid.Model;
                return true;
            }

            validated = default;
            return false;
        }

        public override string ToString() => Enum.GetName(value) ?? $"{value}";
    }

    // ── TestName ──────────────────────────────────────────────────────────────────

    [Validated]
    public readonly record struct TestName
    {
        [MaxLength]
        private const int MaxLength = 50;

        [Required]
        private const bool IsRequired = true;

        private readonly string value;

        public string Value => value;

        private TestName(string value)
        {
            this.value = value;
        }

        public static TestName Create(string? input)
        {
            if (!string.IsNullOrWhiteSpace(input) && input.Length <= MaxLength)
                return new TestName(input);

            throw new ValidationException<string?, TestName>(input);
        }

        [Validation]
        public static ValidationResult<TestName> Validate(string? input)
            => Validator.Try(Create, input);

        public static bool TryCreate(string? input, out TestName validated)
        {
            var result = Validate(input);
            if (result is ValidatedResult<TestName>.Valid valid)
            {
                validated = valid.Model;
                return true;
            }

            validated = default;
            return false;
        }

        public override string ToString() => value;
    }

    // ── TestModel (complex — validated from a DTO) ────────────────────────────────

    [Validated(typeof(DTO.TestModel))]
    public sealed record TestModel
    {
        public required TestId Id { get; init; }
        public required TestType Type { get; init; }
        public required TestName Name { get; init; }

        /// <summary>
        /// Creates a validated TestModel from an unvalidated DTO.
        /// Each field delegation call throws its own ValidationException on failure,
        /// which propagates out so callers can use Validate() to capture it.
        /// </summary>
        public static TestModel Create(DTO.TestModel dto)
        {
            return new TestModel
            {
                Id   = TestId.Create(dto.Id),
                Type = TestType.Create(dto.Type),
                Name = TestName.Create(dto.Name)
            };
        }

        [Validation]
        public static ValidationResult<TestModel> Validate(DTO.TestModel dto)
            => Validator.Try(Create, dto);

        public static bool TryCreate(DTO.TestModel dto, out TestModel? validated)
        {
            var result = Validate(dto);
            if (result is ValidatedResult<TestModel>.Valid valid)
            {
                validated = valid.Model;
                return true;
            }

            validated = default;
            return false;
        }

        public static DTO.TestModel ToDTO(TestModel model)
        {
            return new DTO.TestModel
            {
                Id   = model.Id.Value,
                Type = model.Type.ToString(),
                Name = model.Name.Value
            };
        }
    }
}
