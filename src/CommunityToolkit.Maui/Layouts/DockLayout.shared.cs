using CommunityToolkit.Maui.Core.Interfaces;
using CommunityToolkit.Maui.Core.Layouts;
using Microsoft.Maui.Layouts;

namespace CommunityToolkit.Maui.Layouts;

/// <summary>DockLayout positions its child elements along the edges of the layout container.</summary>
public class DockLayout : Layout, IDockLayout
{
	/// <summary>Docking position for a view.</summary>
	public static readonly BindableProperty DockProperty = BindableProperty.Create(nameof(DockEnum), typeof(DockEnum), typeof(DockLayout), DockEnum.None, BindingMode.TwoWay);

	/// <summary>If true, the last child fills the remaining space.</summary>
	public static readonly BindableProperty LastChildFillProperty = BindableProperty.Create(nameof(LastChildFill), typeof(bool), typeof(DockLayout), true, BindingMode.TwoWay);

	/// <summary>Horizontal (width) and vertical (height) spacing between docked views.</summary>
	public static readonly BindableProperty SpacingProperty = BindableProperty.Create(nameof(Spacing), typeof(SizeF), typeof(DockLayout), SizeF.Zero, BindingMode.TwoWay);

	/// <inheritdoc/>
	public bool LastChildFill
	{
		get { return (bool) GetValue(LastChildFillProperty); }
		set { SetValue(LastChildFillProperty, value); }
	}

	/// <inheritdoc/>
	public SizeF Spacing
	{
		get { return (SizeF) GetValue(SpacingProperty); }
		set { SetValue(SpacingProperty, value); }
	}
	
	/// <summary>Gets the docking position for a view.</summary>
	public static DockEnum GetDock(BindableObject view)
	{
		return (DockEnum) view.GetValue(DockProperty);
	}

	/// <summary>Sets the docking position for a view.</summary>
	public static void SetDock(BindableObject view, DockEnum value)
	{
		view.SetValue(DockProperty, value);
	}

	/// <inheritdoc/>
	public DockEnum GetDock(IView view)
	{
		if (view is BindableObject bindable)
		{
			return (DockEnum) bindable.GetValue(DockProperty);
		}

		return DockEnum.None;
	}
	
	/// <inheritdoc/>
	protected override ILayoutManager CreateLayoutManager()
	{
		return new DockLayoutManager(this);
	}
}
