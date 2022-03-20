using CommunityToolkit.Maui.Core.Interfaces;
using Microsoft.Maui.Layouts;

namespace CommunityToolkit.Maui.Layouts;

/// <summary>
/// The <see cref="UniformItemsLayout"/> is just like the <see cref="Grid"/>, with the possibility of multiple rows and columns, but with one important difference:
/// All rows and columns will have the same size.
/// Use this when you need the Grid behavior without the need to specify different sizes for the rows and columns.
/// </summary>
public class UniformItemsLayout : Layout, IUniformItemsLayout
{
	double childWidth, childHeight;

	/// <summary>
	/// Backing BindableProperty for the <see cref="MaxRows"/> property.
	/// </summary>
	public static readonly BindableProperty MaxRowsProperty = BindableProperty.Create(nameof(MaxRows), typeof(int), typeof(UniformItemsLayout), int.MaxValue);

	/// <summary>
	/// Backing BindableProperty for the <see cref="MaxColumns"/> property.
	/// </summary>
	public static readonly BindableProperty MaxColumnsProperty = BindableProperty.Create(nameof(MaxColumns), typeof(int), typeof(UniformItemsLayout), int.MaxValue);

	/// <summary>
	/// Max rows
	/// </summary>
	public int MaxRows
	{
		get => (int)GetValue(MaxRowsProperty);
		set => SetValue(MaxRowsProperty, value);
	}

	/// <summary>
	/// Max columns
	/// </summary>
	public int MaxColumns
	{
		get => (int)GetValue(MaxColumnsProperty);
		set => SetValue(MaxColumnsProperty, value);
	}

	/// <summary>
	/// Arrange children
	/// </summary>
	/// <param name="rectangle">Grid rectangle</param>
	/// <returns>Child size</returns>
	public Size ArrangeChildren(Rect rectangle)
	{
		Measure(rectangle.Width, rectangle.Height, MeasureFlags.None);

		var columns = GetColumnsCount(Children.Count, rectangle.Width, childWidth);
		var rows = GetRowsCount(Children.Count, columns);
		var boundsWidth = rectangle.Width / columns;
		var boundsHeight = childHeight;
		var bounds = new Rect(0, 0, boundsWidth, boundsHeight);
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
			if (child.Visibility is not Visibility.Visible)
			{
				continue;
			}

			var sizeRequest = child.Measure(double.PositiveInfinity, double.PositiveInfinity);
			childHeight = sizeRequest.Height;
			childWidth = sizeRequest.Width;
		}

		var columns = GetColumnsCount(Children.Count, widthConstraint, childWidth);
		var rows = GetRowsCount(Children.Count, columns);

		return new Size(columns * childWidth, rows * childHeight);
	}

	/// <inheritdoc	/>
	protected override ILayoutManager CreateLayoutManager() => this;

	int GetColumnsCount(int visibleChildrenCount, double widthConstraint, double maxChildWidth)
		=> Math.Min(double.IsPositiveInfinity(widthConstraint)
					   ? visibleChildrenCount
					   : Math.Min((int)(widthConstraint / maxChildWidth), visibleChildrenCount), MaxColumns);

	int GetRowsCount(int visibleChildrenCount, int columnsCount)
		=> Math.Min((int)Math.Ceiling((double)visibleChildrenCount / columnsCount), MaxRows);
}