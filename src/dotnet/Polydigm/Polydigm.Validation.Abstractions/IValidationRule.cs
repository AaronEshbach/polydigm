namespace Polydigm.Validation
{
    public interface IValidationRule
    {
        bool IsValid(object value);
    }

    public interface IValidationRule<T> : IValidationRule
    {
        bool IsValid(T value);
    }
}
