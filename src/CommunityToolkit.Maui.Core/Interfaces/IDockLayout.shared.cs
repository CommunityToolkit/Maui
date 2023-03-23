namespace CommunityToolkit.Maui.Core;

/// <summary>
/// DockLayout is a layout where views can be docked to the sides (top, left, right, bottom) of the layout container.
/// </summary>
public interface IDockLayout : ILayout
{
	/// <summary>
	/// If true, the last child is expanded to fill the remaining space (default: true).
	/// </summary>
	public bool ShouldExpandLastChild { get; }

	/// <summary>
	/// Horizontal spacing between docked views.
	/// </summary>
	public double HorizontalSpacing { get; }

	/// <summary>
	/// Vertical spacing between docked views.
	/// </summary>
	public double VerticalSpacing { get; }

	/// <summary>
	/// Gets the docking position for a view.
	/// </summary>
	/// <param name="view">A view that belongs to the DockLayout.</param>
	/// <returns>DockPosition that signifies where the view will dock.</returns>
	public DockPosition GetDockPosition(IView view);

	/// <summary>
	/// Adds a view to the layout container at the given docking position.
	/// </summary>
	/// <param name="view">Child view to be added to the container.</param>
	/// <param name="position">Docking position for the view.</param>
	public void Add(IView view, DockPosition position = DockPosition.None);
}