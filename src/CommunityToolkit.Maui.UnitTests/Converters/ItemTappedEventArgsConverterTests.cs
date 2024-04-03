using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ItemTappedEventArgsConverterTests : BaseOneWayConverterTest<ItemTappedEventArgsConverter>
{
	public static IReadOnlyList<object?[]> Data { get; } =
	[
		[null, null],
		[new ItemTappedEventArgs("", 1, 1), 1],
		[new ItemTappedEventArgs("", 'c', 1), 'c'],
		[new ItemTappedEventArgs("", Colors.Black, 1), Colors.Black],
	];

	[Theory]
	[MemberData(nameof(Data))]
	public void ItemTappedEventArgsConverter(ItemTappedEventArgs value, object? expectedResult)
	{
		var itemTappedEventArgsConverter = new ItemTappedEventArgsConverter();

		var convertResult = ((ICommunityToolkitValueConverter)itemTappedEventArgsConverter).Convert(value, typeof(object), null, CultureInfo.CurrentCulture);
		var convertFromResult = itemTappedEventArgsConverter.ConvertFrom(value);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[InlineData(5.5)]
	[InlineData('c')]
	[InlineData(true)]
	[InlineData("abc")]
	public void InvalidConverterValuesThrowArgumentException(object value)
	{
		var itemTappedEventArgsConverter = new ItemTappedEventArgsConverter();
		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)itemTappedEventArgsConverter).Convert(value, typeof(object), null, CultureInfo.CurrentCulture));
	}

	[Fact]
	public void ItemTappedEventArgsConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ItemTappedEventArgsConverter()).Convert(new ItemTappedEventArgs("", "", 1), null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ItemTappedEventArgsConverter()).ConvertBack(true, null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}