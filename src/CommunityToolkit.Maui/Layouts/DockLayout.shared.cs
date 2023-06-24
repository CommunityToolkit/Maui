using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Layouts;
using Microsoft.Maui.Layouts;

namespace CommunityToolkit.Maui.Layouts;

/// <summary>
/// DockLayout positions its child elements along the edges of the layout container.
/// </summary>
public class DockLayout : Layout, IDockLayout
{
	/// <summary>
	/// Docking position for a view.
	/// </summary>
	public static readonly BindableProperty DockPositionProperty = BindableProperty.CreateAttached(nameof(DockPosition),
																									typeof(DockPosition),
																									typeof(DockLayout),
																									DockPosition.None,
																									propertyChanged: OnDockPositionPropertyChanged);

	/// <summary>
	/// If true, the last child is expanded to fill the remaining space (default: true).
	/// </summary>
	public static readonly BindableProperty ShouldExpandLastChildProperty = BindableProperty.Create(nameof(ShouldExpandLastChild),
																										typeof(bool),
																										typeof(DockLayout),
																										true,
																										propertyChanged: OnShouldExpandLastChildPropertyChanged);

	/// <summary>
	/// Horizontal spacing between docked views.
	/// </summary>
	public static readonly BindableProperty HorizontalSpacingProperty = BindableProperty.Create(nameof(HorizontalSpacing),
																									typeof(double),
																									typeof(DockLayout),
																									0.0d,
																									propertyChanged: OnHorizontalSpacingPropertyChanged);

	/// <summary>
	/// Vertical spacing between docked views.
	/// </summary>
	public static readonly BindableProperty VerticalSpacingProperty = BindableProperty.Create(nameof(VerticalSpacing),
																								typeof(double),
																								typeof(DockLayout),
																								0.0d,
																								propertyChanged: OnVerticalSpacingPropertyChanged);

	/// <inheritdoc/>
	public bool ShouldExpandLastChild
	{
		get => (bool)GetValue(ShouldExpandLastChildProperty);
		set => SetValue(ShouldExpandLastChildProperty, value);
	}

	/// <inheritdoc/>
	public double HorizontalSpacing
	{
		get => (double)GetValue(HorizontalSpacingProperty);
		set => SetValue(HorizontalSpacingProperty, value);
	}

	/// <inheritdoc/>
	public double VerticalSpacing
	{
		get => (double)GetValue(VerticalSpacingProperty);
		set => SetValue(VerticalSpacingProperty, value);
	}

	/// <summary>
	/// Gets the docking position for a view.
	/// </summary>
	public static DockPosition GetDockPosition(BindableObject view)
	{
		return (DockPosition)view.GetValue(DockPositionProperty);
	}

	/// <summary>
	/// Sets the docking position for a view.
	/// </summary>
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
	public void Add(IView view, DockPosition position = DockPosition.None)
	{
		Children.Add(view);
		SetDockPosition((View)view, position);
	}

	/// <inheritdoc/>
	protected override ILayoutManager CreateLayoutManager() => new DockLayoutManager(this);

	static void OnDockPositionPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is Element element)
		{
			InvalidateDockLayoutMeasure(element);
		}
	}

	static void OnShouldExpandLastChildPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is Element element)
		{
			InvalidateDockLayoutMeasure(element);
		}
	}

	static void OnHorizontalSpacingPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is Element element)
		{
			InvalidateDockLayoutMeasure(element);
		}
	}

	static void OnVerticalSpacingPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is Element element)
		{
			InvalidateDockLayoutMeasure(element);
		}
	}

	static void InvalidateDockLayoutMeasure(Element element)
	{
		if (element is DockLayout dockLayout)
		{
			dockLayout.InvalidateMeasure();
		}

		while (element.Parent is not null)
		{
			if (element.Parent is DockLayout dockLayoutParent)
			{
				dockLayoutParent.InvalidateMeasure();
			}

			element = element.Parent;
		}
	}
}