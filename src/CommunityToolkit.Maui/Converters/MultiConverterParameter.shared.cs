namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Represents a parameter to be used in the <see cref="MultiConverter"/>.
/// </summary>
[AcceptEmptyServiceProvider]
public partial class MultiConverterParameter : BindableObject
{
	/// <summary>
	/// The object type of this parameter.
	/// </summary>
	public Type? ConverterType { get; set; }

	/// <summary>
	/// The value of this parameter.
	/// </summary>
	public object? Value { get; set; }
}