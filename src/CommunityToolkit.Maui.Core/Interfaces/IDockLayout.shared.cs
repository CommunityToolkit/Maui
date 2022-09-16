namespace CommunityToolkit.Maui.Core.Interfaces;

/// <summary>Docking position for a view in <see cref="IDockLayout"/>.</summary>
public enum DockEnum
{
	/// <summary>Do not dock.</summary>
	None = 0,
	/// <summary>Dock at the top.</summary>
	Top = 1,
	/// <summary>Dock to the left.</summary>
	Left = 2,
	/// <summary>Dock to the right.</summary>
	Right = 3,
	/// <summary>Dock at the bottom.</summary>
	Bottom = 4
}

/// <summary>DockLayout positions its child elements along the edges of the layout container.</summary>
public interface IDockLayout : ILayout
{
	/// <summary>If true, the last child fills the remaining space.</summary>
	public bool LastChildFill { get; }

	/// <summary>Horizontal (width) and vertical (height) spacing between docked views.</summary>
	public SizeF Spacing { get; }

	/// <summary>Gets the docking position for a view (or Dock.Top as default value).</summary>
	/// <param name="view">A view that belongs to the DockLayout.</param>
	/// <returns>DockEnum that signifies where the item will dock.</returns>
	DockEnum GetDock(IView view);
}