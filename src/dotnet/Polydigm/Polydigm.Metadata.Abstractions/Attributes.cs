using System.Reflection;
using System.Text.RegularExpressions;

namespace Polydigm.Metadata
{
    public abstract class ValidationAttributeBase() : Attribute;

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class PatternAttribute : ValidationAttributeBase
    {
        private static string? GetPatternOfRegex(Regex? regexInstance)
        {
            return regexInstance?.ToString();
        }

        public static string GetPatternOfRegexProperty(PropertyInfo property)
        {
            if (property.PropertyType != typeof(Regex))
            {
                throw new ArgumentException("The provided property is not of type Regex.", nameof(property));
            }

            if (!property.GetMethod.IsStatic)
            {
                throw new ArgumentException("The provided property is not static.", nameof(property));
            }

            var regexInstance = property.GetValue(null) as Regex;
            return GetPatternOfRegex(regexInstance) ?? throw new ArgumentException("The provided property does not contain a valid Regex instance.", nameof(property)); ;
        }

        public static string GetPatternOfRegexField(FieldInfo field)
        {
            if (field.FieldType != typeof(Regex))
            {
                throw new ArgumentException("The provided field is not of type Regex.", nameof(field));
            }

            if (!field.IsStatic)
            {
                throw new ArgumentException("The provided field is not static.", nameof(field));
            }

            var regexInstance = field.GetValue(null) as Regex;
            return GetPatternOfRegex(regexInstance) ?? throw new ArgumentException("The provided field does not contain a valid Regex instance.", nameof(field));
        }
    }
}
