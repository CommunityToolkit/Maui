using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// The UniformGrid is just like the Grid, with the possibility of multiple rows and columns, but with one important difference:
/// All rows and columns will have the same size.
/// Use this when you need the Grid behavior without the need to specify different sizes for the rows and columns.
/// </summary>
public class UniformGrid : Grid, ILayoutManager
{
	double childWidth;

	double childHeight;

	/// <summary>
	/// Assign this as a LayoutManager
	/// </summary>
	/// <returns><see cref="ILayoutManager"/></returns>
	protected override ILayoutManager CreateLayoutManager()
	{
		return this;
	}

	/// <summary>
	/// Arrange children
	/// </summary>
	/// <param name="rectangle">Grid rectangle</param>
	/// <returns>Child size</returns>
	public Size ArrangeChildren(Rectangle rectangle)
	{
		Measure(rectangle.Width, rectangle.Height, MeasureFlags.None);
		var columns = GetColumnsCount(Children.Count, rectangle.Width, childWidth);
		var rows = GetRowsCount(Children.Count, columns);
		var boundsWidth = rectangle.Width / columns;
		var boundsHeight = childHeight;
		var bounds = new Rectangle(0, 0, boundsWidth, boundsHeight);
		var count = 0;

		for (var i = 0; i < rows; i++)
		{
			for (var j = 0; j < columns && count < Children.Count; j++)
			{
				var item = Children[count];
				bounds.X = j * boundsWidth;
				bounds.Y = i * boundsHeight;
				item.Arrange(bounds);
				count++;
			}
		}

		return new Size(boundsWidth, boundsHeight);
	}

	/// <summary>
	/// Measure grid size
	/// </summary>
	/// <param name="widthConstraint">Width constraint</param>
	/// <param name="heightConstraint">Height constraint</param>
	/// <returns>Grid size</returns>
	public Size Measure(double widthConstraint, double heightConstraint)
	{
		foreach (var child in Children)
		{
			if (child.Visibility != Visibility.Visible)
				continue;

			var sizeRequest = child.Measure(double.PositiveInfinity, double.PositiveInfinity);
			childHeight = sizeRequest.Height;
			childWidth = sizeRequest.Width;
		}

		var columns = GetColumnsCount(Children.Count, widthConstraint, childWidth);
		var rows = GetRowsCount(Children.Count, columns);
		return new Size(columns * childWidth, rows * childHeight);
	}

	int GetColumnsCount(int visibleChildrenCount, double widthConstraint, double maxChildWidth)
		=> double.IsPositiveInfinity(widthConstraint)
			? visibleChildrenCount
			: Math.Min((int)(widthConstraint / maxChildWidth), visibleChildrenCount);

	int GetRowsCount(int visibleChildrenCount, int columnsCount)
		=> (int)Math.Ceiling((double)visibleChildrenCount / columnsCount);
}
