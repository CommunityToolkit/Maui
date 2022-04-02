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
	public void StringToListConverterConverterParameterTest(string? value, object? parameter, object? expectedResult)
	{
		var stringToListConverter = new StringToListConverter
		{
			Separator = "~",
			Separators = new[] { "@", "*" }
		};

		var convertFromResult = stringToListConverter.ConvertFrom(value, typeof(IList<string>), parameter, null);
		var convertResult = (IEnumerable<string>?)((ICommunityToolkitValueConverter)stringToListConverter).Convert(value, typeof(IList<string>), parameter, null);

		Assert.Equal(expectedResult, convertFromResult);
		Assert.Equal(expectedResult, convertResult);
	}

	[Fact]
	public void StringToListConverterSeparatorsTest()
	{
		const string valueToConvert = "MAUI@Toolkit*Converter@Test";
		var expectedResult = new[] { "MAUI", "Toolkit", "Converter", "Test" };

		var stringToListConverter = new StringToListConverter
		{
			Separator = "~",
			Separators = new[] { "@", "*" }
		};

		var convertFromResult = stringToListConverter.ConvertFrom(valueToConvert, typeof(IList<string>), null, null);
		var convertResult = (IEnumerable<string>?)((ICommunityToolkitValueConverter)stringToListConverter).Convert(valueToConvert, typeof(IList<string>), null, null);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Fact]
	public void StringToListConverterSeparatorTest()
	{
		const string valueToConvert = "MAUI~Toolkit~Converter~Test";
		var expectedResult = new[] { "MAUI", "Toolkit", "Converter", "Test" };

		var stringToListConverter = new StringToListConverter
		{
			Separator = "~",
		};

		var convertFromResult = stringToListConverter.ConvertFrom(valueToConvert, typeof(IList<string>), null, null);
		var convertResult = (IEnumerable<string>?)((ICommunityToolkitValueConverter)stringToListConverter).Convert(valueToConvert, typeof(IList<string>), null, null);

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
		var stringToListConverter = new StringToListConverter();

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)stringToListConverter).Convert(value, typeof(IList<string>), null, null));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(5.5)]
	[InlineData('c')]
	[InlineData(true)]
	[InlineData("abc")]
	public void InvalidConverterParametersThrowArgumentException(object parameter)
	{
		var stringToListConverter = new StringToListConverter();

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)stringToListConverter).Convert(Array.Empty<object>(), typeof(IList<string>), parameter, null));
	}

	[Fact]
	public void StringToListConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
		Assert.Throws<ArgumentNullException>(() => new StringToListConverter { Separator = null });
		Assert.Throws<ArgumentNullException>(() => new StringToListConverter().Separator = null);
		Assert.Throws<ArgumentNullException>(() => new StringToListConverter { Separators = null });
		Assert.Throws<ArgumentNullException>(() => new StringToListConverter().Separators = null);
		Assert.Throws<ArgumentNullException>(() => new StringToListConverter { Separators = new List<string?> { ",", null } });
		Assert.Throws<ArgumentNullException>(() => new StringToListConverter().Separators = new List<string?> { ",", null });
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new StringToListConverter()).Convert(string.Empty, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new StringToListConverter()).ConvertBack(new List<string>(), null, null, null));
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}