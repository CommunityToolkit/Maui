namespace CommunityToolkit.Maui.Core.Interfaces;

/// <summary>DockLayout positions its child elements along the edges of the layout container.</summary>
public interface IDockLayout : ILayout
{
	/// <summary>If true, the last child is expanded to fill the remaining space (default: true).</summary>
	public bool ExpandLastChild { get; }
	
	/// <summary>Horizontal spacing between docked views.</summary>
	public double HorizontalSpacing { get; }

	/// <summary>Vertical spacing between docked views.</summary>
	public double VerticalSpacing { get; }

	/// <summary>Gets the docking position for a view.</summary>
	/// <param name="view">A view that belongs to the DockLayout.</param>
	/// <returns>DockPosition that signifies where the view will dock.</returns>
	DockPosition GetDockPosition(IView view);
}