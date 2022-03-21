using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class StringToListConverter_Tests : BaseTest
{
	public static IReadOnlyList<object?[]> ListData { get; } = new[]
	{
		new object?[] { "A,B.C;D", new string[] { ",", ".", ";" }, new string[] { "A", "B", "C", "D" } },
		new object?[] { "A+_+B+_+C", "+_+", new string[] { "A", "B", "C" } },
		new object?[] { "A,,C", ",", new string[] { "A", string.Empty, "C" }, },
		new object?[] { "A,C", ",", new string?[] { "A", "C" } },
		new object?[] { "A", ":-:", new string[] { "A" } },
		new object?[] { string.Empty, ",", new string[] { string.Empty } },
		new object?[] { null, ",", Array.Empty<string>() },
		new object?[] { "ABC", null, new string[] { "ABC" } },
	};

	[Theory]
	[MemberData(nameof(ListData))]
	public void StringToListConverter(string? value, object? parameter, object? expectedResult)
	{
		var stringToListConverter = new StringToListConverter();

		var convertFromResult = stringToListConverter.ConvertFrom(value, typeof(IEnumerable<string>), parameter, null);
		var convertResult = (IEnumerable<string>?)((ICommunityToolkitValueConverter)stringToListConverter).Convert(value, typeof(IEnumerable<string>), parameter, null);

		Assert.Equal(expectedResult, convertFromResult);
		Assert.Equal(expectedResult, convertResult);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(5.5)]
	[InlineData('c')]
	[InlineData(true)]
	[InlineData("abc")]
	public void InvalidConverterValuesThrowArgumenException(object value)
	{
		var listToStringConverter = new ListToStringConverter();

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)listToStringConverter).Convert(value, typeof(IEnumerable<string>), null, null));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(5.5)]
	[InlineData('c')]
	[InlineData(true)]
	[InlineData("abc")]
	public void InvalidConverterParametersThrowArgumenException(object parameter)
	{
		var listToStringConverter = new ListToStringConverter();

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)listToStringConverter).Convert(Array.Empty<object>(), typeof(IEnumerable<string>), parameter, null));
	}
}