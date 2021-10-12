using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Graphics;

namespace CommunityToolkit.Maui.Views
{
	/// <summary>
	/// The UniformGrid is just like the Grid, with the possibility of multiple rows and columns, but with one important difference:
	/// All rows and columns will have the same size.
	/// Use this when you need the Grid behavior without the need to specify different sizes for the rows and columns.
	/// </summary>
	public class UniformGrid : Layout<View>
	{
		double childWidth;

		double childHeight;

		protected override void LayoutChildren(double x, double y, double width, double height)
		{
			Measure(width, height, 0);
			var columns = GetColumnsCount(Children.Count, width, childWidth);
			var rows = GetRowsCount(Children.Count, columns);
			var boundsWidth = width / columns;
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
					item.Layout(bounds);
					count++;
				}
			}
		}

		protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
		{
			foreach (var child in Children)
			{
				if (!child.IsVisible)
					continue;

				var sizeRequest = child.Measure(double.PositiveInfinity, double.PositiveInfinity, 0);
				var minimum = sizeRequest.Minimum;
				var request = sizeRequest.Request;

				childHeight = Math.Max(minimum.Height, request.Height);
				childWidth = Math.Max(minimum.Width, request.Width);
			}

			var columns = GetColumnsCount(Children.Count, widthConstraint, childWidth);
			var rows = GetRowsCount(Children.Count, columns);
			var size = new Size(columns * childWidth, rows * childHeight);
			return new SizeRequest(size, size);
		}

		int GetColumnsCount(int visibleChildrenCount, double widthConstraint, double maxChildWidth)
			=> double.IsPositiveInfinity(widthConstraint)
				? visibleChildrenCount
				: Math.Min((int)(widthConstraint / maxChildWidth), visibleChildrenCount);

		int GetRowsCount(int visibleChildrenCount, int columnsCount)
			=> (int)Math.Ceiling((double)visibleChildrenCount / columnsCount);
	}
}