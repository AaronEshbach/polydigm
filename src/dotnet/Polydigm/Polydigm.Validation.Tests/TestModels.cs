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
        public static bool TryCreate(string? input, out TestId validated)
        {
            if (input is not null && Pattern.IsMatch(input))
            {
                validated = new TestId(input);
                return true;
            }
            
            validated = default;
            return false;
        }

        public static TestId Create(string? input)
        {
            if (TryCreate(input, out var validated))
            {
                return validated;
            }

            throw new ValidationException<string?, TestId>(input);
        }

        public override string ToString() => value;        
    }


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

        public static bool TryCreate(string? input, out TestType validated)
        {
            if (input is not null && Enum.TryParse<TestTypeEnum>(input, out var enumValue))
            {
                validated = new TestType(enumValue);
                return true;
            }

            validated = default;
            return false;
        }

        [Validation]
        public static TestType Create(string? input)
        {
            if (TryCreate(input, out var validated))
            {
                return validated;
            }

            throw new ValidationException<string?, TestId>(input);
        }

        public override string ToString() => Enum.GetName(value) ?? $"{value}";
    }

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

        public static bool TryCreate(string? input, out TestName validated)
        {
            if (!string.IsNullOrWhiteSpace(input) && input.Length <= MaxLength)
            {
                validated = new TestName(input);
                return true;
            }
            validated = default;
            return false;
        }

        [Validation]
        public static TestName Create(string? input)
        {
            if (TryCreate(input, out var validated))
            {
                return validated;
            }
            throw new ValidationException<string?, TestId>(input);
        }

        public override string ToString() => value;
    }

    [Validated(typeof(DTO.TestModel))]
    public sealed record TestModel
    {
        public required TestId Id { get; init; }
        public required TestType Type { get; init; }
        public required TestName Name { get; init; }

        public static bool TryCreate(DTO.TestModel dto, out TestModel? validated)
        {
            if (TestId.TryCreate(dto.Id, out var id) &&
                TestType.TryCreate(dto.Type, out var type) &&
                TestName.TryCreate(dto.Name, out var name))
            {
                validated = new TestModel
                {
                    Id = id,
                    Type = type,
                    Name = name
                };

                return true;
            }

            validated = default;
            return false;
        }

        [Validation]
        public static TestModel Create(DTO.TestModel dto)
        {
            return new TestModel
            {
                Id = TestId.Create(dto.Id),
                Type = TestType.Create(dto.Type),
                Name = TestName.Create(dto.Name)
            };
        }

        public static DTO.TestModel ToDTO(TestModel model)
        {
            return new DTO.TestModel
            {
                Id = model.Id.Value,
                Type = model.Type.ToString(),
                Name = model.Name.Value
            };
        }
    }
}
