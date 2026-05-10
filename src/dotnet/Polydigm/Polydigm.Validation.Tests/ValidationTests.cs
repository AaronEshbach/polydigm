namespace Polydigm.Validation.Tests
{
    public class ValidationTests
    {
        // ── Create ────────────────────────────────────────────────────────────────

        [Fact]
        public void Create_with_valid_input_returns_the_validated_type()
        {
            var validString = "abc123-789xyz";

            var validated = TestId.Create(validString);

            Assert.IsType<TestId>(validated);
            Assert.Equal(validString, validated.Value);
        }

        [Fact]
        public void Create_with_invalid_input_throws_ValidationException()
        {
            var invalidString = "invalid";

            var exception = Assert.Throws<ValidationException<string?, TestId>>(
                () => TestId.Create(invalidString));

            Assert.Equal(ValidationException.ErrorType, exception.Type);
            Assert.Equal($"Invalid_{typeof(TestId).Name}", exception.Name);
        }

        // ── Validate ──────────────────────────────────────────────────────────────

        [Fact]
        public void Validate_with_valid_input_returns_Valid_result_with_model()
        {
            var validString = "abc123-789xyz";

            var result = TestId.Validate(validString);

            var valid = Assert.IsType<ValidatedResult<TestId>.Valid>(result);
            Assert.Equal(validString, valid.Model.Value);
        }

        [Fact]
        public void Validate_with_invalid_input_returns_Invalid_result_with_error()
        {
            var invalidString = "invalid";

            var result = TestId.Validate(invalidString);

            var invalid = Assert.IsType<ValidatedResult<TestId>.Invalid>(result);
            Assert.False(result.IsValid);
            Assert.NotNull(invalid.Error);
            Assert.NotEmpty(invalid.ErrorMessage!);
        }

        [Fact]
        public void Validate_result_IsValid_is_true_for_valid_input()
        {
            var result = TestId.Validate("abc123-789xyz");
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_result_IsValid_is_false_for_invalid_input()
        {
            var result = TestId.Validate("invalid");
            Assert.False(result.IsValid);
        }

        [Fact]
        public void Validate_Invalid_UntypedModel_is_null()
        {
            var result = TestId.Validate("invalid");
            Assert.Null(result.UntypedModel);
        }

        [Fact]
        public void Validate_Valid_UntypedModel_is_the_model()
        {
            var validString = "abc123-789xyz";
            var result = TestId.Validate(validString);
            Assert.NotNull(result.UntypedModel);
        }

        // ── TryCreate ─────────────────────────────────────────────────────────────

        [Fact]
        public void TryCreate_with_valid_input_returns_true_and_sets_out_parameter()
        {
            var validString = "abc123-789xyz";

            var success = TestId.TryCreate(validString, out var validated);

            Assert.True(success);
            Assert.Equal(validString, validated.Value);
        }

        [Fact]
        public void TryCreate_with_invalid_input_returns_false_and_out_is_default()
        {
            var success = TestId.TryCreate("invalid", out var validated);

            Assert.False(success);
            Assert.Equal(default, validated);
        }

        // ── Equality ──────────────────────────────────────────────────────────────

        [Fact]
        public void Two_models_from_the_same_value_are_equal()
        {
            var s = "abc123-789xyz";
            Assert.Equal(TestId.Create(s), TestId.Create(s));
        }

        [Fact]
        public void Two_models_from_different_values_are_not_equal()
        {
            Assert.NotEqual(TestId.Create("abc123-789xyz"), TestId.Create("def456-012uvw"));
        }

        // ── Complex model (TestModel) ─────────────────────────────────────────────

        [Fact]
        public void Validate_complex_model_returns_Valid_for_correct_dto()
        {
            var dto = new DTO.TestModel { Id = "abc123-789xyz", Type = "TypeA", Name = "Alice" };

            var result = TestModel.Validate(dto);

            var valid = Assert.IsType<ValidatedResult<TestModel>.Valid>(result);
            Assert.Equal("abc123-789xyz", valid.Model.Id.Value);
        }

        [Fact]
        public void Validate_complex_model_returns_Invalid_when_field_fails()
        {
            var dto = new DTO.TestModel { Id = "bad-id", Type = "TypeA", Name = "Alice" };

            var result = TestModel.Validate(dto);

            Assert.IsType<ValidatedResult<TestModel>.Invalid>(result);
            Assert.False(result.IsValid);
        }
    }
}
