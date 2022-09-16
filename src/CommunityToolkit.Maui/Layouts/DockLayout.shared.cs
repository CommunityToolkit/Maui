using Microsoft.Maui.Layouts;

namespace CommunityToolkit.Maui.Layouts;

/// <summary>DockLayout positions its child elements along the edges of the layout container.</summary>
public class DockLayout : Layout
{
    /// <summary>Docking position for a view.</summary>
    public static readonly BindableProperty DockProperty = BindableProperty.Create(nameof(DockEnum), typeof(DockEnum), typeof(DockLayout), DockEnum.Top, BindingMode.TwoWay);

    /// <summary>If true, the last child fills the remaining space.</summary>
    public static readonly BindableProperty LastChildFillProperty = BindableProperty.Create(nameof(LastChildFill), typeof(bool), typeof(DockLayout), true, BindingMode.TwoWay);

    /// <summary>Horizontal (width) and vertical (height) spacing between docked views.</summary>
    public static readonly BindableProperty SpacingProperty = BindableProperty.Create(nameof(Spacing), typeof(SizeF), typeof(DockLayout), SizeF.Zero, BindingMode.TwoWay);

    /// <summary>If true, the last child fills the remaining space.</summary>
    public bool LastChildFill
    {
        get { return (bool) GetValue(LastChildFillProperty); }
        set { SetValue(LastChildFillProperty, value); }
    }

    /// <summary>Horizontal (width) and vertical (height) spacing between docked views.</summary>
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
    protected override ILayoutManager CreateLayoutManager()
    {
        return new DockLayoutManager(this);
    }

    #region Helper

    /// <summary>Static helper to get the docking position for a view (or Dock.Top as default value).</summary>
    public static DockEnum GetDock(IView view)
    {
        if (view is BindableObject bindable)
        {
            return (DockEnum) bindable.GetValue(DockProperty);
        }

        return DockEnum.Top;
    }

    #endregion
}
