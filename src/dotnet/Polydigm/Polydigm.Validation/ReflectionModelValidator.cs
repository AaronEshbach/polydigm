using System.Collections.Concurrent;
using System.Reflection;

namespace Polydigm.Validation
{
    internal sealed class PrimitiveValidator
    {
        private readonly Func<object, IValidationResult> validate;

        public PrimitiveValidator(Type modelType, Type primitiveType)
        {
            var staticMethods = modelType.GetMethods(BindingFlags.Public | BindingFlags.Static);

            var candidateMethods = staticMethods.Select(m => {
                var parameters = m.GetParameters();
                return new { Method = m, Parameters = parameters, ValidationAttribute = m.GetCustomAttribute<ValidationAttribute>() };
            }).Where(m => m.Parameters.Length == 1
                    && m.Parameters[0].ParameterType == primitiveType
                    && (typeof(IValidationResult).IsAssignableFrom(m.Method.ReturnType) || modelType.IsAssignableFrom(m.Method.ReturnType)))
            .ToList();
                
            var validationMethod = candidateMethods.FirstOrDefault(m => m.ValidationAttribute is not null);

            if (validationMethod is null)
            {
                validationMethod = candidateMethods.FirstOrDefault();
            }

            if (validationMethod is not null)
            {
                var m = validationMethod.Method;

                if (typeof(IValidationResult).IsAssignableFrom(m.ReturnType))
                {
                    this.validate = value => (IValidationResult)m.Invoke(null, [value])!;
                }
                else if (modelType.IsAssignableFrom(m.ReturnType))
                {
                    this.validate = value =>
                    {
                        try
                        {
                            var result = m.Invoke(null, [value])!;
                            return new ValidationResult(isValid: true, model: result);
                        }
                        catch (TargetInvocationException ex) when (ex.InnerException is ValidationException)
                        {
                            return new ValidationResult(isValid: false, exception: ex.InnerException);
                        }
                        catch (Exception ex)
                        {
                            return new ValidationResult(isValid: false, exception: new ValidationException("Unexpected_Validation_Error", $"An unexpected error occurred during validation of type '{modelType.FullName}': {ex.Message}", ex));
                        }
                    };
                }
            }

            if (this.validate is null)
            {
                this.validate = value => new ValidationResult(isValid: false, exception: new ValidationException("Validated_Model_Definition_Error", $"No suitable validation method found for model type '{modelType.FullName}' and primitive type '{primitiveType.FullName}'."));
            }
        }

        public IValidationResult Validate(object value) => this.validate(value);
    }

    internal sealed class ValueExtractor
    {
        private readonly Func<object, object> getValue;

        public ValueExtractor(Type modelType, Type primitiveType)
        {
            var staticMethods = modelType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            var valueMethod = staticMethods.Where(m => m.ReturnType == modelType).FirstOrDefault(m =>
            {
                var parameters = m.GetParameters();
                return parameters.Length == 1
                    && parameters[0].ParameterType == primitiveType;
            });

            if (valueMethod is null)
            {
                var instanceProperties = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var valueProperty = instanceProperties.FirstOrDefault(p => p.PropertyType == primitiveType);

                if (valueProperty is not null)
                {
                    this.getValue = model => valueProperty.GetValue(model)!;
                }
                else
                {
                    throw new InvalidOperationException($"No suitable value extractor found for model type '{modelType.FullName}' and primitive type '{primitiveType.FullName}'.");
                }
            }
            else
            {
                this.getValue = model => valueMethod.Invoke(null, [model])!;
            }
        }

        public object GetValue(object model) => this.getValue(model);
    }

    public class ReflectionModelValidator : IModelValidator
    {
        private static readonly ConcurrentDictionary<Type, ValueExtractor> valueExtractors = new();
        private static readonly ConcurrentDictionary<Type, PrimitiveValidator> primitiveValidators = new();

        public TPrimitive GetValue<TPrimitive, TModel>(TModel validatedModel)
        {
            var extractor = valueExtractors.GetOrAdd(typeof(TModel), modelType => new ValueExtractor(modelType, typeof(TPrimitive)));
            return (TPrimitive)extractor.GetValue(validatedModel!);
        }

        /// <summary>
        /// Generic entry point for compile-time-typed callers.
        /// Discovers the validation method on <typeparamref name="TModel"/> via reflection,
        /// invokes it with <paramref name="unvalidatedValue"/>, and returns a typed result.
        /// </summary>
        public ValidationResult<TModel> Validate<TPrimitive, TModel>(TPrimitive unvalidatedValue)
        {
            var validator = primitiveValidators.GetOrAdd(typeof(TModel), modelType => new PrimitiveValidator(modelType, typeof(TPrimitive)));
            var result = validator.Validate(unvalidatedValue!);
            if (result.IsValid)
            {
                return new ValidatedResult<TModel>.Valid((TModel)result.UntypedModel!);
            }
            else
            {
                if (result.Exception is ValidationException validationException)
                {
                    return new ValidatedResult<TModel>.Invalid(new ValidationException<TPrimitive, TModel>(validationException.Name, validationException.Message, unvalidatedValue, validationException.InnerException));
                }

                return new ValidatedResult<TModel>.Invalid(new ValidationException<TPrimitive, TModel>($"{typeof(TModel).Name}_Validation_Error", result.ErrorMessage ?? "Validation failed without a specific error message.", unvalidatedValue, result.Exception));
            }
        }

        /// <summary>
        /// Non-generic entry point for infrastructure callers (e.g. service discovery parameter
        /// binding) that know <paramref name="primitiveType"/> and <paramref name="modelType"/>
        /// only at runtime.
        ///
        /// Returns a non-generic <see cref="IValidationResult"/> so the caller can read
        /// <see cref="IValidationResult.UntypedModel"/> and
        /// <see cref="IValidationResult.ErrorMessage"/> without needing the concrete type.
        /// </summary>
        public static IValidationResult Validate(object? unvalidatedValue, Type primitiveType, Type modelType)
        {
            var validator = primitiveValidators.GetOrAdd(modelType, mt => new PrimitiveValidator(mt, primitiveType));
            return validator.Validate(unvalidatedValue!);
        }
    }
}
