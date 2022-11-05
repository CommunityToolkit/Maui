namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Docking position for a view in <see cref="IDockLayout"/>.
/// </summary>
public enum DockPosition
{
	/// <summary>
	/// Do not dock.
	/// </summary>
	None = 0,

	/// <summary>
	/// Dock at the top.
	/// </summary>
	Top = 1,

	/// <summary>
	/// Dock to the left.
	/// </summary>
	Left = 2,

	/// <summary>
	/// Dock to the right.
	/// </summary>
	Right = 3,

	/// <summary>
	/// Dock at the bottom.
	/// </summary>
	Bottom = 4
}