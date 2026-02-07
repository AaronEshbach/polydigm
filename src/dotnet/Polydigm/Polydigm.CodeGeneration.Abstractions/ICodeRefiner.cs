using Polydigm.Metadata;

namespace Polydigm.CodeGeneration
{
    /// <summary>
    /// Refines metadata for language-specific code generation.
    /// Refiners transform the universal metadata model into language-specific idioms
    /// before code generation (e.g., naming conventions, type mappings, language features).
    ///
    /// Inspired by Kiota's ILanguageRefiner pattern.
    /// </summary>
    public interface ICodeRefiner
    {
        /// <summary>
        /// The target language this refiner is for.
        /// </summary>
        ICodeGenerationTarget Target { get; }

        /// <summary>
        /// Refines a data type for the target language.
        /// </summary>
        /// <param name="dataType">The data type to refine.</param>
        /// <param name="context">Optional context for refinement decisions.</param>
        /// <returns>The refined data type (may be the same instance if no changes needed).</returns>
        IDataType RefineDataType(IDataType dataType, RefinementContext? context = null);

        /// <summary>
        /// Refines a model for the target language.
        /// </summary>
        /// <param name="model">The model to refine.</param>
        /// <param name="context">Optional context for refinement decisions.</param>
        /// <returns>The refined model (may be the same instance if no changes needed).</returns>
        IModelMetadata RefineModel(IModelMetadata model, RefinementContext? context = null);

        /// <summary>
        /// Refines all types in a generation input.
        /// </summary>
        /// <param name="input">The generation input to refine.</param>
        /// <param name="context">Optional context for refinement decisions.</param>
        /// <returns>Refined generation input.</returns>
        GenerationInput RefineAll(GenerationInput input, RefinementContext? context = null);
    }

    /// <summary>
    /// Context information for refinement decisions.
    /// </summary>
    public sealed class RefinementContext
    {
        /// <summary>
        /// All types being refined (useful for cross-type decisions).
        /// </summary>
        public IReadOnlyList<IDataType>? AllDataTypes { get; init; }

        /// <summary>
        /// All models being refined (useful for cross-model decisions).
        /// </summary>
        public IReadOnlyList<IModelMetadata>? AllModels { get; init; }

        /// <summary>
        /// Code generation options that may influence refinement.
        /// </summary>
        public CodeGenerationOptions? Options { get; init; }

        /// <summary>
        /// Extension data for custom refinement logic.
        /// </summary>
        public Dictionary<string, object>? ExtensionData { get; init; }
    }

    /// <summary>
    /// Base class for code refiners with optional pass-through behavior.
    /// </summary>
    public abstract class CodeRefinerBase : ICodeRefiner
    {
        protected CodeRefinerBase(ICodeGenerationTarget target)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
        }

        public ICodeGenerationTarget Target { get; }

        /// <summary>
        /// Refines a data type. Default implementation returns the input unchanged.
        /// Override to add language-specific transformations.
        /// </summary>
        public virtual IDataType RefineDataType(IDataType dataType, RefinementContext? context = null)
        {
            return dataType;
        }

        /// <summary>
        /// Refines a model. Default implementation returns the input unchanged.
        /// Override to add language-specific transformations.
        /// </summary>
        public virtual IModelMetadata RefineModel(IModelMetadata model, RefinementContext? context = null)
        {
            return model;
        }

        /// <summary>
        /// Refines all types. Can be overridden for cross-type optimizations.
        /// </summary>
        public virtual GenerationInput RefineAll(GenerationInput input, RefinementContext? context = null)
        {
            var refinedDataTypes = input.DataTypes.Select(dt => RefineDataType(dt, context)).ToList();
            var refinedModels = input.Models.Select(m => RefineModel(m, context)).ToList();

            return GenerationInput.From(refinedDataTypes, refinedModels);
        }
    }

    /// <summary>
    /// Pass-through refiner that makes no changes.
    /// Useful as a default when no refinement is needed.
    /// </summary>
    public sealed class PassThroughRefiner : CodeRefinerBase
    {
        public PassThroughRefiner(ICodeGenerationTarget target) : base(target)
        {
        }

        // Base implementation already passes through unchanged
    }
}
