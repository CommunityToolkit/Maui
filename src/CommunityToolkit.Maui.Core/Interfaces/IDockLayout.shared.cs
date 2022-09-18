namespace CommunityToolkit.Maui.Core.Interfaces;

/// <summary>DockLayout positions its child elements along the edges of the layout container.</summary>
public interface IDockLayout : ILayout
{
	/// <summary>If true, the last child fills the remaining space.</summary>
	public bool LastChildFill { get; }

	/// <summary>Horizontal (width) and vertical (height) spacing between docked views.</summary>
	public SizeF Spacing { get; }

	/// <summary>Gets the docking position for a view (or Dock.Top as default value).</summary>
	/// <param name="view">A view that belongs to the DockLayout.</param>
	/// <returns>DockPosition that signifies where the item will dock.</returns>
	DockPosition GetDockPosition(IView view);
}