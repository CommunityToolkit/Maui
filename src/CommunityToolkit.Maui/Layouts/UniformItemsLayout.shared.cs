using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Layouts;
using Microsoft.Maui.Layouts;

namespace CommunityToolkit.Maui.Layouts;

/// <summary>
/// The <see cref="UniformItemsLayout"/> is just like the <see cref="Grid"/>, with the possibility of multiple rows and columns, but with one important difference:
/// All rows and columns will have the same size.
/// Use this when you need the Grid behavior without the need to specify different sizes for the rows and columns.
/// </summary>
public class UniformItemsLayout : Layout, IUniformItemsLayout
{
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

	/// <inheritdoc	/>
	protected override ILayoutManager CreateLayoutManager() => new UniformItemsLayoutManager(this);
}