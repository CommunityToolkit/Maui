using Microsoft.Maui.Layouts;

namespace CommunityToolkit.Maui.Layouts;

/// <summary>LayoutManager for the <see cref="DockLayout"/>.</summary>
public class DockLayoutManager : LayoutManager
{
    readonly DockLayout dockLayout;

    /// <summary>Ctor of DockLayoutManager.</summary>
    public DockLayoutManager(DockLayout dockLayout)
        : base(dockLayout)
    {
        this.dockLayout = dockLayout;
    }

    /// <inheritdoc />
    public override Size Measure(double widthConstraint, double heightConstraint)
    {
        var padding = dockLayout.Padding;
        var spacing = dockLayout.Spacing;

        widthConstraint -= padding.HorizontalThickness;
        heightConstraint -= padding.VerticalThickness;

        double width = padding.Left;
        double height = padding.Top;

        double totalWidth = 0;
        double totalHeight = 0;

        foreach (var child in dockLayout)
        {
            if (!IsChildVisible(child))
            {
                continue;
            }

            var childSize = child.Measure(widthConstraint, heightConstraint);

            switch (DockLayout.GetDock(child))
            {
                case DockEnum.Left:
                case DockEnum.Right:
                {
                    width += (childSize.Width + spacing.Width);
                    widthConstraint -= spacing.Width;
                    break;
                }
                case DockEnum.Top:
                case DockEnum.Bottom:
                {
                    height += (childSize.Height + spacing.Height);
                    heightConstraint -= spacing.Height;
                    break;
                }
                default:
                {
                    goto case DockEnum.Top;
                }
            }

            totalWidth = Math.Max(totalWidth, width);
            totalHeight = Math.Max(totalHeight, height);
        }

        return new Size(totalWidth + padding.HorizontalThickness, totalHeight + padding.VerticalThickness);
    }

    /// <inheritdoc />
    public override Size ArrangeChildren(Rect bounds)
    {
        var padding = dockLayout.Padding;
        var spacing = dockLayout.Spacing;

        var x = bounds.Left + padding.Left;
        var y = bounds.Top + padding.Top;

        var width = bounds.Width - padding.HorizontalThickness;
        var height = bounds.Height - padding.VerticalThickness;

        var i = 0;
        foreach (var child in dockLayout)
        {
            if (!IsChildVisible(child))
            {
                continue;
            }

            i++;

            var childWidth = Math.Min(width, child.DesiredSize.Width);
            var childHeight = Math.Min(height, child.DesiredSize.Height);

            var lastItem = (i == dockLayout.Count);
            if (lastItem && dockLayout.LastChildFill)
            {
                child.Arrange(new Rect(x, y, width, height));

                return new Size(width, height);
            }

            double childX;
            double childY;

            switch (DockLayout.GetDock(child))
            {
                case DockEnum.Left:
                {
                    childX = x;
                    childY = y;
                    childHeight = height;
                    x += (childWidth + spacing.Width);
                    width -= (childWidth + spacing.Width);
                    break;
                }
                case DockEnum.Top:
                {
                    childX = x;
                    childY = y;
                    childWidth = width;
                    y += (childHeight + spacing.Height);
                    height -= (childHeight + spacing.Height);
                    break;
                }
                case DockEnum.Right:
                {
                    childX = x + width - childWidth;
                    childY = y;
                    childHeight = height;
                    width -= (childWidth + spacing.Width);
                    break;
                }
                case DockEnum.Bottom:
                {
                    childX = x;
                    childY = y + height - childHeight;
                    childWidth = width;
                    height -= (childHeight + spacing.Height);
                    break;
                }
                default:
                {
                    goto case DockEnum.Top;
                }
            }

            child.Arrange(new Rect(childX, childY, childWidth, childHeight));
        }

        return bounds.Size;
    }

    static bool IsChildVisible(IView view)
    {
        if (view is VisualElement visualElement)
        {
            return visualElement.IsVisible;
        }

        return true;
    }
}
