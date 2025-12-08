using Polydigm.Errors;

namespace Polydigm.Validation
{
    public class ValidationException(string name, string message, Exception? innerException = null) 
        : NamedExceptionBase(name, ErrorType, message, innerException)
    {
        public const string ErrorType = "Validation_Error";
    }

    public class ValidationException<T>(string name, string message, T value, Exception? innerException = null)
        : ValidationException(name, message, innerException)
    {
        public T Value => value;
    }

    public class ValidationException<TPrimitive, TValidated> : ValidationException<TPrimitive>
    {
        public ValidationException(string name, string message, TPrimitive value, Exception? innerException = null)
            : base(name, message, value, innerException)
        {
        }

        public ValidationException(TPrimitive value) : this(
            name: $"Invalid_{typeof(TValidated).Name}",
            message: $"The value '{value}' is not a valid {typeof(TValidated).Name}.",
            value: value)
        {
        }
    }
}
