using CommunityToolkit.Maui.Converters;

namespace CommunityToolkit.Maui.Sample.Converters;

/// <summary>
/// Compares a double value using the <see cref="CompareConverter.ComparingValue"/> 
/// and returns a <see cref="Color"/> based on the comparison.
/// </summary>
public sealed class CompareDoubleToColorConverter : CompareConverter<double, Color>
{
}