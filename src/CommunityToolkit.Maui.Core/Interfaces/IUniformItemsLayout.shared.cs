using Microsoft.Maui.Layouts;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Uniform Items Layout Control
/// </summary>
public interface IUniformItemsLayout : ILayout
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