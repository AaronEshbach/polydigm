using Polydigm.Errors;

namespace Polydigm.Validation
{
    public class ValidationException(string name, string message, Exception? innerException = null) 
        : NamedExceptionBase(name, ErrorType, message, innerException)
    {
        public const string ErrorType = "Validation_Error";
    }

    public class ValidationException<TModel>(string name, string message, Exception? innerException = null)
        : ValidationException(name, message, innerException)
    {
        public ValidationException(string message, Exception? innerException = null) : this(
            name: $"Invalid_{typeof(TModel).Name}",
            message: message,
            innerException: innerException)
        {
        }
    }

    public class ValidationException<TPrimitive, TValidated> : ValidationException<TValidated>
    {
        private readonly TPrimitive value;

        public TPrimitive Value => value;

        public ValidationException(string name, string message, TPrimitive value, Exception? innerException = null)
            : base(name, message, innerException)
        {
            this.value = value;
        }

        public ValidationException(TPrimitive value) : this(
            name: $"Invalid_{typeof(TValidated).Name}",
            message: $"The value '{value}' is not a valid {typeof(TValidated).Name}.",
            value: value)
        {
        }
    }

    public class AggregateValidationException(IEnumerable<ValidationException> innerExceptions) 
        : ValidationException(
            name: innerExceptions.Select(e => e.Name).SingleOrDefault() ?? "Multiple_Validation_Errors",
            message: string.Join(" | ", innerExceptions.Select(e => e.Message)),
            innerException: innerExceptions.FirstOrDefault()
         )
    {
        public IEnumerable<ValidationException> InnerExceptions => innerExceptions;
    }

    public class AggregateValidationException<TModel>(IEnumerable<ValidationException<TModel>> innerExceptions)
        : ValidationException(
            name: innerExceptions.Select(e => e.Name).SingleOrDefault() ?? $"Invalid_{typeof(TModel).Name}",
            message: string.Join(" | ", innerExceptions.Select(e => e.Message)),
            innerException: innerExceptions.FirstOrDefault()
         )
    {
        public IEnumerable<ValidationException> InnerExceptions => innerExceptions;
    }
}
