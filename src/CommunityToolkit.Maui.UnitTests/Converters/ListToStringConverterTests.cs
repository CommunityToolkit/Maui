using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ListToStringConverterTests : BaseOneWayConverterTest<ListToStringConverter>
{
	public static IReadOnlyList<object?[]> TestData { get; } =
	[
		[new[] { "A", "B", "C" }, "+_+", "A+_+B+_+C"],
		[new[] { "A", string.Empty, "C" }, ",", "A,C"],
		[new[] { "A", null, "C" }, ",", "A,C"],
		[new[] { "A" }, ":-:", "A"],
		[Array.Empty<string>(), ",", string.Empty],
		[new[] { "A", "B", "C" }, null, "ABC"],
	];

	[Theory]
	[MemberData(nameof(TestData))]
	public void ListToStringConverter(string[] value, string? parameter, string expectedResult)
	{
		var listToStringConverter = new ListToStringConverter();

		var convertResult = (string?)((ICommunityToolkitValueConverter)listToStringConverter).Convert(value, typeof(string), parameter, null);
		var convertFromResult = listToStringConverter.ConvertFrom(value, parameter);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[MemberData(nameof(TestData))]
	public void ListToStringConverterExplicitParameter(string[] value, string? parameter, string expectedResult)
	{
		var listToStringConverter = new ListToStringConverter()
		{
			Separator = parameter ?? string.Empty
		};

		var convertResult = (string?)((ICommunityToolkitValueConverter)listToStringConverter).Convert(value, typeof(string), null, null);
		var convertFromResult = listToStringConverter.ConvertFrom(value);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Fact]
	public void NullSeparatorValueThrowArgumentNullException()
	{
		var listToStringConverter = new ListToStringConverter();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => listToStringConverter.Separator = null);
		Assert.Throws<ArgumentNullException>(() => new ListToStringConverter { Separator = null });
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[InlineData(0)]
	[InlineData(5.5)]
	[InlineData('c')]
	[InlineData(true)]
	public void InvalidConverterValuesThrowArgumentException(object value)
	{
		var listToStringConverter = new ListToStringConverter();

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)listToStringConverter).Convert(value, typeof(string), null, null));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(5.5)]
	[InlineData('c')]
	[InlineData(true)]
	public void InvalidConverterParametersThrowArgumentException(object parameter)
	{
		var listToStringConverter = new ListToStringConverter();

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)listToStringConverter).Convert(Array.Empty<object>(), typeof(string), parameter, null));
	}

	[Fact]
	public void ListToStringConverterConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ListToStringConverter()).Convert(true, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ListToStringConverter()).Convert(null, typeof(string), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ListToStringConverter()).ConvertBack(string.Empty, null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	protected override object? GetInvalidConvertFromValue() => null;
}