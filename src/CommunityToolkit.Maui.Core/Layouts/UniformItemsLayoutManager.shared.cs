using Microsoft.Maui.Layouts;

namespace CommunityToolkit.Maui.Core.Layouts;

/// <summary>
/// <see cref="LayoutManager"/> for <see cref="IUniformItemsLayout"/>.
/// </summary>
/// <remarks>
/// Initialize a new instance of <see cref="UniformItemsLayoutManager"/>.
/// </remarks>
public class UniformItemsLayoutManager(IUniformItemsLayout uniformItemsLayout) : LayoutManager(uniformItemsLayout)
{
	double childWidth, childHeight;

	readonly IUniformItemsLayout uniformItemsLayout = uniformItemsLayout;

	/// <summary>
	/// Arrange children
	/// </summary>
	/// <param name="rectangle">Grid rectangle</param>
	/// <returns>Child size</returns>
	public override Size ArrangeChildren(Rect rectangle)
	{
		var width = rectangle.Width - uniformItemsLayout.Padding.HorizontalThickness;
		var visibleChildren = uniformItemsLayout.Where(static x => x.Visibility is Visibility.Visible).ToArray();

		if (visibleChildren.Length is 0 || width is 0)
		{
			return rectangle.Size;
		}

		var columns = GetColumnsCount(visibleChildren.Length, width);
		var rows = GetRowsCount(visibleChildren.Length, columns);

		var boundsWidth = width / columns;
		var boundsHeight = childHeight;
		var bounds = new Rect(0, 0, boundsWidth, boundsHeight);
		var count = 0;

		for (var i = 0; i < rows; i++)
		{
			for (var j = 0; j < columns && count < visibleChildren.Length; j++)
			{
				var item = visibleChildren[count];
				bounds.X = j * boundsWidth + uniformItemsLayout.Padding.Left;
				bounds.Y = i * boundsHeight + uniformItemsLayout.Padding.Top;
				item.Measure(bounds.Width, bounds.Height);
				item.Arrange(bounds);
				count++;
			}
		}

		return rectangle.Size;
	}

	/// <summary>
	/// Measure grid size
	/// </summary>
	/// <param name="widthConstraint">Width constraint</param>
	/// <param name="heightConstraint">Height constraint</param>
	/// <returns>Grid size</returns>
	public override Size Measure(double widthConstraint, double heightConstraint)
	{
		var visibleChildren = uniformItemsLayout.Where(static x => x.Visibility is Visibility.Visible).ToArray();

		if (childWidth == 0)
		{
			var sizeRequest = visibleChildren.Length == 0 ? Size.Zero : visibleChildren[0].Measure(double.PositiveInfinity, double.PositiveInfinity);

			childWidth = sizeRequest.Width;
			childHeight = sizeRequest.Height;
		}

		var columns = GetColumnsCount(visibleChildren.Length, widthConstraint - uniformItemsLayout.Padding.HorizontalThickness);
		var rows = GetRowsCount(visibleChildren.Length, columns);

		return new Size(columns * childWidth + uniformItemsLayout.Padding.HorizontalThickness, rows * childHeight + uniformItemsLayout.Padding.VerticalThickness);
	}

	int GetColumnsCount(int visibleChildrenCount, double widthConstraint)
	{
		var columnsCount = visibleChildrenCount;
		if (childWidth != 0 && !double.IsPositiveInfinity(widthConstraint))
		{
			columnsCount = Math.Min((int)(widthConstraint / childWidth), visibleChildrenCount);
		}

		return Math.Clamp(columnsCount, 1, uniformItemsLayout.MaxColumns);
	}

	int GetRowsCount(int visibleChildrenCount, int columnsCount)
		=> Math.Min(
			(int)Math.Ceiling((double)visibleChildrenCount / columnsCount),
			uniformItemsLayout.MaxRows);
}