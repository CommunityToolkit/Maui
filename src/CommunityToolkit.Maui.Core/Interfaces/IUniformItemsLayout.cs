using Microsoft.Maui.Layouts;

namespace CommunityToolkit.Maui.Core.Interfaces;

/// <summary>
/// Uniform Items Layout Control
/// </summary>
public interface IUniformItemsLayout : ILayoutManager
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