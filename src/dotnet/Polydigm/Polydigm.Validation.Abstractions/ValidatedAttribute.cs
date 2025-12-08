namespace Polydigm.Validation
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class ValidatedAttribute : Attribute
    {
        private readonly Type? underlyingType;
        public Type? UnderlyingType => underlyingType;

        public ValidatedAttribute()
        {
            this.underlyingType = null;
        }

        public ValidatedAttribute(Type? underlyingType)
        {
            this.underlyingType = underlyingType;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ValidationAttribute : Attribute
    {
    }
}
