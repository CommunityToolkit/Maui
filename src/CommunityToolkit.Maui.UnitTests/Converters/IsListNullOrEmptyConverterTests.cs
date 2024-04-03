using System.Collections;
using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class IsListNullOrEmptyConverterTests : BaseOneWayConverterTest<IsListNullOrEmptyConverter>
{
	public static IReadOnlyList<object?[]> Data { get; } =
	[
		[new List<string>(), true],
		[new List<string> { "TestValue" }, false],
		[null, true],
		[Enumerable.Range(1, 3), false],
	];

	[Theory]
	[MemberData(nameof(Data))]
	public void IsListNullOrEmptyConverter(IEnumerable? value, bool expectedResult)
	{
		var listIstNullOrEmptyConverter = new IsListNullOrEmptyConverter();

		var convertResult = (bool?)((ICommunityToolkitValueConverter)listIstNullOrEmptyConverter).Convert(value, typeof(bool), null, CultureInfo.CurrentCulture);
		var convertFromResult = listIstNullOrEmptyConverter.ConvertFrom(value);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[MemberData(nameof(Data))]
	public void IsListNullOrEmptyConverter_ShouldConvert_WhenTargetTypeIsNullableBool(IEnumerable? value, bool expectedResult)
	{
		var listIstNullOrEmptyConverter = new IsListNullOrEmptyConverter();

		var convertResult = (bool?)((ICommunityToolkitValueConverter)listIstNullOrEmptyConverter).Convert(value, typeof(bool?), null, CultureInfo.CurrentCulture);
		var convertFromResult = listIstNullOrEmptyConverter.ConvertFrom(value);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[InlineData(0)]
	public void InvalidConverterValuesThrowArgumentException(object value)
	{
		var listIstNullOrEmptyConverter = new IsListNullOrEmptyConverter();

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)listIstNullOrEmptyConverter).Convert(value, typeof(bool), null, CultureInfo.CurrentCulture));
	}

	[Fact]
	public void IsListNullOrEmptyConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new IsListNullOrEmptyConverter()).Convert(true, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new IsListNullOrEmptyConverter()).ConvertBack(true, null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	protected override object? GetInvalidConvertFromValue() => true;
}