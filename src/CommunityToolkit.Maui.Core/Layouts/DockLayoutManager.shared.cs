using Microsoft.Maui.Layouts;

namespace CommunityToolkit.Maui.Core.Layouts;

/// <summary>
/// <see cref="LayoutManager"/> for <see cref="IDockLayout"/>.
/// </summary>
public class DockLayoutManager : LayoutManager
{
	readonly IDockLayout dockLayout;

	/// <summary>
	/// Initialize a new instance of <see cref="DockLayoutManager"/>.
	/// </summary>
	public DockLayoutManager(IDockLayout dockLayout)
		: base(dockLayout)
	{
		this.dockLayout = dockLayout;
	}

	/// <inheritdoc />
	public override Size Measure(double widthConstraint, double heightConstraint)
	{
		var padding = dockLayout.Padding;
		var horizontalSpacing = dockLayout.HorizontalSpacing;
		var verticalSpacing = dockLayout.VerticalSpacing;

		var width = padding.HorizontalThickness;
		var height = padding.VerticalThickness;

		var widthLimit = widthConstraint - padding.HorizontalThickness;
		var heightLimit = heightConstraint - padding.VerticalThickness;

		foreach (var child in dockLayout)
		{
			if (child.Visibility is not Visibility.Visible)
			{
				continue;
			}

			var childSize = child.Measure(widthLimit, heightLimit);
			var dockPosition = dockLayout.GetDockPosition(child);

			switch (dockPosition)
			{
				case DockPosition.Left:
				case DockPosition.Right:
					var childWidth = childSize.Width + horizontalSpacing;
					width += childWidth;
					widthLimit -= childWidth;
					break;

				case DockPosition.Top:
				case DockPosition.Bottom:
					var childHeight = childSize.Height + verticalSpacing;
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
					throw new NotSupportedException($"{nameof(DockPosition)} {dockPosition} is not supported");
			}
		}

		return new Size(Math.Min(width, widthConstraint), Math.Min(height, heightConstraint));
	}

	/// <inheritdoc />
	public override Size ArrangeChildren(Rect bounds)
	{
		var padding = dockLayout.Padding;
		var horizontalSpacing = dockLayout.HorizontalSpacing;
		var verticalSpacing = dockLayout.VerticalSpacing;

		var x = bounds.Left + padding.Left;
		var y = bounds.Top + padding.Top;

		var width = bounds.Width - padding.HorizontalThickness;
		var height = bounds.Height - padding.VerticalThickness;

		foreach (var child in dockLayout)
		{
			if (child.Visibility is not Visibility.Visible)
			{
				continue;
			}

			var childWidth = Math.Min(width, child.DesiredSize.Width);
			var childHeight = Math.Min(height, child.DesiredSize.Height);

			var isLastChild = (child == dockLayout[^1]);
			if (isLastChild && dockLayout.ShouldExpandLastChild)
			{
				child.Arrange(new Rect(x, y, width, height));

				return bounds.Size;
			}

			double childX;
			double childY;

			var dockPosition = dockLayout.GetDockPosition(child);
			switch (dockPosition)
			{
				case DockPosition.Top:
				case DockPosition.None:
					{
						childX = x;
						childY = y;
						childWidth = width;
						y += (childHeight + verticalSpacing);
						height -= (childHeight + verticalSpacing);
						break;
					}
				case DockPosition.Left:
					{
						childX = x;
						childY = y;
						childHeight = height;
						x += (childWidth + horizontalSpacing);
						width -= (childWidth + horizontalSpacing);
						break;
					}
				case DockPosition.Right:
					{
						childX = x + width - childWidth;
						childY = y;
						childHeight = height;
						width -= (childWidth + horizontalSpacing);
						break;
					}
				case DockPosition.Bottom:
					{
						childX = x;
						childY = y + height - childHeight;
						childWidth = width;
						height -= (childHeight + verticalSpacing);
						break;
					}
				default:
					throw new NotSupportedException($"{nameof(DockPosition)} {dockPosition} is not supported");
			}

			child.Arrange(new Rect(childX, childY, childWidth, childHeight));
		}

		return bounds.Size;
	}
}