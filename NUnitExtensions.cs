using NUnit.Framework.Constraints;

namespace NUnit.Framework;

/// <summary>
/// Extensions for NUnit constraints.
/// </summary>
public static class NUnitExtensions
{
    /// <summary>
    /// Modifies the EqualConstraint to use default comparers including Optional support.
    /// </summary>
    /// <param name="constraint">The EqualConstraint to modify.</param>
    /// <returns>The same constraint instance for method chaining.</returns>
    public static EqualConstraint UsingDefaults(this EqualConstraint constraint) =>
        constraint.UsingPropertiesComparer().UsingOptionalComparer();
}
