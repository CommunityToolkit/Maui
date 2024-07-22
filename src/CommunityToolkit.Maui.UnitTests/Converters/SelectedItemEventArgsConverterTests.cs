using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class SelectedItemEventArgsConverterTests : BaseOneWayConverterTest<SelectedItemEventArgsConverter>
{
	public static IReadOnlyList<object?[]> Data { get; } =
	[
		[null, null],
		[new SelectedItemChangedEventArgs(1, 1), 1],
		[new SelectedItemChangedEventArgs('c', 1), 'c'],
		[new SelectedItemChangedEventArgs(Colors.Black, 1), Colors.Black],
	];

	[Theory]
	[MemberData(nameof(Data))]
	public void SelectedItemEventArgsConverter(SelectedItemChangedEventArgs value, object? expectedResult)
	{
		var selectedItemEventArgsConverter = new SelectedItemEventArgsConverter();

		var convertResult = ((ICommunityToolkitValueConverter)selectedItemEventArgsConverter).Convert(value, typeof(object), null, CultureInfo.CurrentCulture);
		var convertFromResult = selectedItemEventArgsConverter.ConvertFrom(value);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[InlineData(5.5)]
	[InlineData('c')]
	[InlineData(true)]
	[InlineData("abc")]
	public void InvalidConverterValuesThrowsArgumentException(object value)
	{
		var itemSelectedEventArgsConverter = new SelectedItemEventArgsConverter();
		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)itemSelectedEventArgsConverter).Convert(value, typeof(object), null, CultureInfo.CurrentCulture));
	}

	[Fact]
	public void SelectedItemEventArgsConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new SelectedItemEventArgsConverter()).Convert(new SelectedItemChangedEventArgs("", 1), null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new SelectedItemEventArgsConverter()).ConvertBack(0.0, null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}