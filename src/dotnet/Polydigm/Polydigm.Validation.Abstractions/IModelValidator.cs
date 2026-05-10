namespace Polydigm.Validation
{
    public interface IModelValidator
    {
        ValidationResult<TModel> Validate<TPrimitive, TModel>(TPrimitive unvalidatedValue);
        TPrimitive GetValue<TPrimitive, TModel>(TModel validatedModel);
    }
}
