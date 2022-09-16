using CommunityToolkit.Maui.Core.Interfaces;
using Microsoft.Maui.Layouts;

namespace CommunityToolkit.Maui.Core.Layouts;

/// <summary>LayoutManager for <see cref="IDockLayout"/>.</summary>
public class DockLayoutManager : LayoutManager, IDockLayoutManager
{
	readonly IDockLayout dockLayout;

	/// <summary>Ctor of DockLayoutManager.</summary>
	public DockLayoutManager(IDockLayout dockLayout)
		: base(dockLayout)
	{
		this.dockLayout = dockLayout;
	}

	/// <inheritdoc />
	public override Size Measure(double widthConstraint, double heightConstraint)
	{
		var padding = dockLayout.Padding;
		var spacing = dockLayout.Spacing;

		double width = padding.HorizontalThickness;
		double height = padding.VerticalThickness;

		var widthLimit = widthConstraint - padding.HorizontalThickness;
		var heightLimit = heightConstraint - padding.VerticalThickness;

		foreach (var child in dockLayout)
		{
			if (child.Visibility != Visibility.Visible)
			{
				continue;
			}

			var childSize = child.Measure(widthLimit, heightLimit);

			switch (dockLayout.GetDock(child))
			{
				case DockEnum.Left:
				case DockEnum.Right:
				{
					var childWidth = childSize.Width + spacing.Width;
					width += childWidth;
					widthLimit -= childWidth;
					break;
				}
				case DockEnum.Top:
				case DockEnum.Bottom:
				{
					var childHeight = childSize.Height + spacing.Height;
					height += childHeight;
					heightLimit -= childHeight;
					break;
				}
				case DockEnum.None:
				default:
				{
					width += childSize.Width;
					widthLimit -= childSize.Width;
					height += childSize.Height;
					heightLimit -= childSize.Height;
					break;
				}
			}
		}

		return new Size(Math.Min(width, widthConstraint), Math.Min(height, heightConstraint));
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
			if (child.Visibility != Visibility.Visible)
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

				return bounds.Size;
			}

			double childX;
			double childY;

			switch (dockLayout.GetDock(child))
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
				case DockEnum.None:
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
}
