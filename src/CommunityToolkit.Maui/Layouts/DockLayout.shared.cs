using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Layouts;
using Microsoft.Maui.Layouts;

namespace CommunityToolkit.Maui.Layouts;

/// <summary>
/// DockLayout positions its child elements along the edges of the layout container.
/// </summary>
[AttachedBindableProperty<DockPosition>(nameof(DockPosition), DefaultValue = DockLayoutDefaults.DockPosition, PropertyChangedMethodName = nameof(OnDockPositionPropertyChanged), BindablePropertyXmlDocumentation = dockPositionBindablePropertyXmlDocumentation)]
public partial class DockLayout : Layout, IDockLayout
{
	const string dockPositionBindablePropertyXmlDocumentation =
		/* language=C#-test */
		//lang=csharp
		"""
		/// <summary>
		/// Docking position for a view.
		/// </summary>
		""";

	/// <summary>
	/// If true, the last child is expanded to fill the remaining space (default: true).
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnShouldExpandLastChildPropertyChanged))]
	public partial bool ShouldExpandLastChild { get; set; } = DockLayoutDefaults.ShouldExpandLastChild;

	/// <summary>
	/// Gets or sets the horizontal spacing between docked views.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnHorizontalSpacingPropertyChanged))]
	public partial double HorizontalSpacing { get; set; } = DockLayoutDefaults.HorizontalSpacing;

	/// <summary>
	/// Gets or sets the vertical spacing between docked views.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnVerticalSpacingPropertyChanged))]
	public partial double VerticalSpacing { get; set; } = DockLayoutDefaults.VerticalSpacing;


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