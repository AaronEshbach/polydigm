namespace Polydigm.Validation
{
    public interface IValidatedModel
    {
        Type UnderlyingType { get; }
        Type ValidatedType { get; }
        IReadOnlyList<IValidationRule> ValidationRules { get; }
    }

    public interface IValidatedModel<TUnderlying> : IValidatedModel
    {
        new IReadOnlyList<IValidationRule<TUnderlying>> ValidationRules { get; }
    }
}
