using Polydigm.Metadata;

namespace Polydigm.Validation
{
    public abstract class ValidationRuleBase : IValidationRule, IConstraint
    {
        public virtual bool IsValid(object value)
        {
            throw new NotImplementedException();
        }
    }
}
