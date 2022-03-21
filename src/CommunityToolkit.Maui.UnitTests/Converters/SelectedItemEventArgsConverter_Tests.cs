using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class SelectedItemEventArgsConverter_Tests : BaseTest
{
	public static IReadOnlyList<object[]> Data { get; } = new[]
	{
		new object[] { new SelectedItemChangedEventArgs(1, 1), 1},
		new object[] { new SelectedItemChangedEventArgs(new object(), 1), new object()},
		new object[] { new SelectedItemChangedEventArgs('c', 1), 'c'},
		new object[] { new SelectedItemChangedEventArgs(Colors.Black, 1), Colors.Black},
	};

	[Theory]
	[MemberData(nameof(Data))]
	public void SelectedItemEventArgsConverter(SelectedItemChangedEventArgs value, object? expectedResult)
	{
		var selectedItemEventArgsConverter = new SelectedItemEventArgsConverter();

		var convertResult = ((ICommunityToolkitValueConverter)selectedItemEventArgsConverter).Convert(value, typeof(DateTime), null, CultureInfo.CurrentCulture);
		var convertFromResult = selectedItemEventArgsConverter.ConvertFrom(value, typeof(DateTime), null, CultureInfo.CurrentCulture);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[InlineData(5.5)]
	[InlineData('c')]
	[InlineData(true)]
	[InlineData("abc")]
	public void InvalidConverterValuesThrowsArgumenException(object value)
	{
		var itemSelectedEventArgsConverter = new SelectedItemEventArgsConverter();
		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)itemSelectedEventArgsConverter).Convert(value, typeof(object), null, CultureInfo.CurrentCulture));
	}
}