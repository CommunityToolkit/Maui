using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Layouts;
using Microsoft.Maui.Layouts;

namespace CommunityToolkit.Maui.Layouts;

/// <summary>
/// The <see cref="UniformItemsLayout"/> is just like the <see cref="Grid"/>, with the possibility of multiple rows and columns, but with one important difference:
/// All rows and columns will have the same size.
/// Use this when you need the Grid behavior without the need to specify different sizes for the rows and columns.
/// </summary>
public partial class UniformItemsLayout : Layout, IUniformItemsLayout
{
	/// <summary>
	/// Gets or sets the maximum number of rows to display or process.
	/// </summary>
	[BindableProperty(PropertyChangingMethodName = nameof(OnMaxRowsPropertyChanging))]
	public partial int MaxRows { get; set; } = UniformItemLayoutDefaults.MaxRows;

	/// <summary>
	/// Gets or sets the maximum number of columns to display in the layout.
	/// </summary>
	/// <remarks>
	/// Set this property to limit the number of columns arranged by the layout. The value must be greater
	/// than or equal to 1. The default value is <see cref="int.MaxValue"/>, which allows an unlimited number of
	/// columns.
	/// </remarks>
	[BindableProperty(PropertyChangingMethodName = nameof(OnMaxColumnsPropertyChanging))]
	public partial int MaxColumns { get; set; } = UniformItemLayoutDefaults.MaxColumns;

	/// <inheritdoc	/>
	protected override ILayoutManager CreateLayoutManager() => new UniformItemsLayoutManager(this);

	static void OnMaxRowsPropertyChanging(BindableObject bindable, object oldValue, object newValue)
	{
		var maxRows = (int)newValue;
		if (maxRows < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(newValue), newValue, $"{nameof(MaxRows)} must be greater or equal to 1.");
		}
	}

	static void OnMaxColumnsPropertyChanging(BindableObject bindable, object oldValue, object newValue)
	{
		var maxColumns = (int)newValue;
		if (maxColumns < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(newValue), newValue, $"{nameof(MaxColumns)} must be greater or equal to 1.");
		}
	}
}