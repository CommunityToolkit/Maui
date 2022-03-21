using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ListToStringConverter_Tests : BaseTest
{
	public static IReadOnlyList<object?[]> TestData { get; } = new[]
	{
		new object[] { new string[] { "A", "B", "C" }, "+_+", "A+_+B+_+C" },
		new object[] { new string[] { "A", string.Empty, "C" }, ",", "A,C" },
		new object?[] { new string?[] { "A", null, "C" }, ",", "A,C" },
		new object[] { new string[] { "A" }, ":-:", "A" },
		new object[] { Array.Empty<string>(), ",", string.Empty },
		new object?[] { new string[] { "A", "B", "C" }, null, "ABC" },
	};

	[Theory]
	[MemberData(nameof(TestData))]
	public void ListToStringConverter(string[] value, string? parameter, string expectedResult)
	{
		var listToStringConverter = new ListToStringConverter();

		var convertResult = (string?)((ICommunityToolkitValueConverter)listToStringConverter).Convert(value, typeof(string), parameter, null);
		var convertFromResult = listToStringConverter.ConvertFrom(value, typeof(string), parameter, null);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
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

	[Fact]
	public void NullConverterValuesThrowArgumentNullException()
	{
		var listToStringConverter = new ListToStringConverter();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)listToStringConverter).Convert(null, typeof(string), null, null));
		Assert.Throws<ArgumentNullException>(() => listToStringConverter.ConvertFrom(null, typeof(string), null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
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
}