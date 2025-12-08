namespace Polydigm.Validation.Tests
{
    public class ValidationTests
    {
        [Fact]
        public void Attempting_to_Create_a_Valid_Value_Produces_the_Validated_Type()
        {
            // Arrange
            var validString = "abc123-789xyz";

            // Act & Assert
            var validated = TestId.Create(validString);

            Assert.IsType<TestId>(validated);
            Assert.Equal(validString, validated.Value);
        }

        [Fact]
        public void Attempting_to_Create_an_Invalid_Value_Raises_a_Validation_Exception()
        {
            // Arrange
            var invalidString = "invalid";

            // Act & Assert
            var exception = Assert.Throws<ValidationException<string, TestId>>(
                () => TestId.Create(invalidString));

            Assert.Equal(ValidationException.ErrorType, exception.Type);
            Assert.Equal($"Invalid_{typeof(TestId).Name}", exception.Name);
        }

        [Fact]
        public void Two_Valid_Models_Initialized_from_the_Same_Values_are_Equal()
        {
            // Arrange
            var validString = "abc123-789xyz";
            var first = TestId.Create(validString);
            var second = TestId.Create(validString);

            // Act & Assert
            Assert.Equal(first, second);
        }

        [Fact]
        public void Two_Valid_Models_Initialized_from_Different_Values_are_not_Equal()
        {
            // Arrange
            var firstString = "abc123-789xyz";
            var secondString = "def456-012uvw";

            var first = TestId.Create(firstString);
            var second = TestId.Create(secondString);

            // Act & Assert
            Assert.NotEqual(first, second);
        }
    }
}