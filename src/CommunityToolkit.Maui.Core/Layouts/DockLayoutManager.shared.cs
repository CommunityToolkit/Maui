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
			var position = dockLayout.GetDockPosition(child);

			switch (position)
			{
				case DockPosition.Left:
				case DockPosition.Right:
					var childWidth = childSize.Width + spacing.Width;
					width += childWidth;
					widthLimit -= childWidth;
					break;

				case DockPosition.Top:
				case DockPosition.Bottom:
					var childHeight = childSize.Height + spacing.Height;
					height += childHeight;
					heightLimit -= childHeight;
					break;

				case DockPosition.None:
					width += childSize.Width;
					widthLimit -= childSize.Width;
					height += childSize.Height;
					heightLimit -= childSize.Height;
					break;

				default:
					throw new NotSupportedException($"{nameof(DockPosition)} {position}	is not yet supported");
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

		foreach (var child in dockLayout)
		{
			if (child.Visibility != Visibility.Visible)
			{
				continue;
			}

			var childWidth = Math.Min(width, child.DesiredSize.Width);
			var childHeight = Math.Min(height, child.DesiredSize.Height);

			var isLastChild = (child == dockLayout[^1]);
			if (isLastChild && dockLayout.LastChildFill)
			{
				child.Arrange(new Rect(x, y, width, height));

				return bounds.Size;
			}

			double childX;
			double childY;

			switch (dockLayout.GetDockPosition(child))
			{
				case DockPosition.Top:
				case DockPosition.None:
				default:
					{
						childX = x;
						childY = y;
						childWidth = width;
						y += (childHeight + spacing.Height);
						height -= (childHeight + spacing.Height);
						break;
					}
				case DockPosition.Left:
					{
						childX = x;
						childY = y;
						childHeight = height;
						x += (childWidth + spacing.Width);
						width -= (childWidth + spacing.Width);
						break;
					}
				case DockPosition.Right:
					{
						childX = x + width - childWidth;
						childY = y;
						childHeight = height;
						width -= (childWidth + spacing.Width);
						break;
					}
				case DockPosition.Bottom:
					{
						childX = x;
						childY = y + height - childHeight;
						childWidth = width;
						height -= (childHeight + spacing.Height);
						break;
					}
			}

			child.Arrange(new Rect(childX, childY, childWidth, childHeight));
		}

		return bounds.Size;
	}
}