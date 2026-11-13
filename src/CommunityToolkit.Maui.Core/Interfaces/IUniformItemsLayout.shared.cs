namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Uniform Items Layout Control
/// </summary>
public interface IUniformItemsLayout : Microsoft.Maui.ILayout
{
	/// <summary>
	/// Max rows
	/// </summary>
	int MaxRows { get; }

	/// <summary>
	/// Max columns
	/// </summary>
	int MaxColumns { get; }
}