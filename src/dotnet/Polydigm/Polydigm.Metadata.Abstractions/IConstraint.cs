using System.Text.RegularExpressions;

namespace Polydigm.Metadata
{
    /// <summary>
    /// A constraint placed on a data type to restrict the values it can hold.
    /// </summary>
    public interface IConstraint;

    public interface IRequiredConstraint : IConstraint
    {
        bool IsRequired { get; }
    }

    public interface IBoundaryConstraint : IConstraint
    {
        BoundaryMode BoundaryMode { get; }
    }

    public interface IMinimumConstraint : IBoundaryConstraint
    {
        IComparable Minimum { get; }
    }

    public interface IMaximumConstraint : IBoundaryConstraint
    {
        IComparable Maximum { get; }
    }

    public interface IMinimumLengthConstraint : IBoundaryConstraint
    {
        long MinimumLength { get; }
    }

    public interface IMaximumLengthConstraint : IBoundaryConstraint
    {
        long MaximumLength { get; }
    }

    public interface IPatternConstraint : IConstraint
    {
        Regex Pattern { get; }
    }

    public interface IEnumConstraint : IConstraint
    {
        IEnumerable<string> AllowedValues { get; }
    }
}
