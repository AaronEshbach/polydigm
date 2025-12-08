namespace Polydigm.Validation
{
    public interface IModelValidationResult
    {
        bool IsValid { get; }
        Exception? Error { get; }
        object? Result { get; }
    }

    public interface IModelValidationResult<TPrimitive, TValidated> : IModelValidationResult
    {
        TPrimitive PrimitiveValue { get; }
        TValidated? ValidatedValue { get; }
    }
}
