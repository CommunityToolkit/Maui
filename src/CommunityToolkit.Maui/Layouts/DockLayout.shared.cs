using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Interfaces;
using CommunityToolkit.Maui.Core.Layouts;
using Microsoft.Maui.Layouts;

namespace CommunityToolkit.Maui.Layouts;

/// <summary>DockLayout positions its child elements along the edges of the layout container.</summary>
public class DockLayout : Layout, IDockLayout
{
	/// <summary>Docking position for a view.</summary>
	public static readonly BindableProperty DockPositionProperty = BindableProperty.Create(nameof(DockPosition), typeof(DockPosition), typeof(DockLayout), DockPosition.None);

	/// <summary>If true, the last child fills the remaining space. Default value is true.</summary>
	public static readonly BindableProperty LastChildFillProperty = BindableProperty.Create(nameof(LastChildFill), typeof(bool), typeof(DockLayout), true);

	/// <summary>Horizontal (width) and vertical (height) spacing between docked views.</summary>
	public static readonly BindableProperty SpacingProperty = BindableProperty.Create(nameof(Spacing), typeof(Thickness), typeof(DockLayout), Thickness.Zero);

	/// <inheritdoc/>
	public bool LastChildFill
	{
		get => (bool)GetValue(LastChildFillProperty);
		set => SetValue(LastChildFillProperty, value);
	}

	/// <inheritdoc/>
	public Thickness Spacing
	{
		get => (Thickness)GetValue(SpacingProperty);
		set => SetValue(SpacingProperty, value);
	}

	/// <summary>Gets the docking position for a view.</summary>
	public static DockPosition GetDockPosition(BindableObject view)
	{
		return (DockPosition)view.GetValue(DockPositionProperty);
	}

	/// <summary>Sets the docking position for a view.</summary>
	public static void SetDockPosition(BindableObject view, DockPosition value)
	{
		view.SetValue(DockPositionProperty, value);
	}

	/// <inheritdoc/>
	public DockPosition GetDockPosition(IView view)
	{
		var mauiView = (View)view;
		return (DockPosition)mauiView.GetValue(DockPositionProperty);
	}

	/// <inheritdoc/>
	protected override ILayoutManager CreateLayoutManager() => new DockLayoutManager(this);
}