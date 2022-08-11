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
		set
		{
			if (value < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(MaxRows)} must be greater or equal to 1.");
			}

			SetValue(MaxRowsProperty, value);
		}
	}

	/// <summary>
	/// Max columns
	/// </summary>
	public int MaxColumns
	{
		get => (int)GetValue(MaxColumnsProperty);
		set
		{
			if (value < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(MaxColumns)} must be greater or equal to 1.");
			}

			SetValue(MaxColumnsProperty, value);
		}
	}

	/// <summary>
	/// Arrange children
	/// </summary>
	/// <param name="rectangle">Grid rectangle</param>
	/// <returns>Child size</returns>
	public Size ArrangeChildren(Rect rectangle)
	{
		var width = Width - Padding.HorizontalThickness;
		var visibleChildren = Children.Where(x => x.Visibility == Visibility.Visible).ToArray();

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
				bounds.X = j * boundsWidth + Padding.Left;
				bounds.Y = i * boundsHeight + Padding.Top;
				item.Arrange(bounds);
				count++;
			}
		}

		return bounds.Size;
	}

	/// <summary>
	/// Measure grid size
	/// </summary>
	/// <param name="widthConstraint">Width constraint</param>
	/// <param name="heightConstraint">Height constraint</param>
	/// <returns>Grid size</returns>
	public Size Measure(double widthConstraint, double heightConstraint)
	{
		var visibleChildren = Children.Where(x => x.Visibility == Visibility.Visible).ToArray();

		if (childWidth == 0)
		{
			var sizeRequest = visibleChildren[0].Measure(double.PositiveInfinity, double.PositiveInfinity);

			childWidth = sizeRequest.Width;
			childHeight = sizeRequest.Height;
		}

		var columns = GetColumnsCount(visibleChildren.Length, widthConstraint - Padding.HorizontalThickness);
		var rows = GetRowsCount(visibleChildren.Length, columns);

		return new Size(columns * childWidth + Padding.HorizontalThickness, rows * childHeight + Padding.VerticalThickness);
	}

	/// <inheritdoc	/>
	protected override ILayoutManager CreateLayoutManager() => this;

	int GetColumnsCount(int visibleChildrenCount, double widthConstraint)
	{
		var columnsCount = visibleChildrenCount;
		if (childWidth != 0 && !double.IsPositiveInfinity(widthConstraint))
		{
			columnsCount = Math.Clamp((int)(widthConstraint / childWidth), 1, visibleChildrenCount);
		}

		return Math.Min(columnsCount, MaxColumns);
	}

	int GetRowsCount(int visibleChildrenCount, int columnsCount)
		=> Math.Min(
			(int)Math.Ceiling((double)visibleChildrenCount / columnsCount),
			MaxRows);
}