using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class StringToListConverterTests : BaseOneWayConverterTest<StringToListConverter>
{
	public static IReadOnlyList<object?[]> ListData { get; } =
	[
		["A,B.C;D", new[] { ",", ".", ";" }, new[] { "A", "B", "C", "D" }],
		["A+_+B+_+C", "+_+", new[] { "A", "B", "C" }],
		["A,,C", ",", new[] { "A", string.Empty, "C" }],
		["A,C", ",", new[] { "A", "C" }],
		["A", ":-:", new[] { "A" }],
		[string.Empty, ",", new[] { string.Empty }],
		[null, ",", Array.Empty<string>()],
		["ABC", null, new[] { "ABC" }],
	];

	[Theory]
	[MemberData(nameof(ListData))]
	public void StringToListConverterConverterParameterTest(string? value, object? parameter, object? expectedResult)
	{
		var stringToListConverter = new StringToListConverter
		{
			Separator = "~",
			Separators = new[] { "@", "*" }
		};

		var convertFromResult = stringToListConverter.ConvertFrom(value, parameter);
		var convertResult = (IEnumerable<string>?)((ICommunityToolkitValueConverter)stringToListConverter).Convert(value, typeof(IList<string>), parameter, null);

		Assert.Equal(expectedResult, convertFromResult);
		Assert.Equal(expectedResult, convertResult);
	}

	[Fact]
	public void StringToListConverter_EnsureParameterDoesNotOverrideProperty()
	{
		var converter = new StringToListConverter { Separators = new[] { ",", " " } };

		var parameterResult = converter.ConvertFrom("maui/toolkit tests", new[] { "/", " " });
		Assert.Equal(new[] { "maui", "toolkit", "tests" }, parameterResult);

		var propertyResult = converter.ConvertFrom("maui,toolkit tests");
		Assert.Equal(new[] { "maui", "toolkit", "tests" }, propertyResult);
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

		var convertFromResult = stringToListConverter.ConvertFrom(valueToConvert);
		var convertResult = (string[]?)((ICommunityToolkitValueConverter)stringToListConverter).Convert(valueToConvert, typeof(IList<string>), null, null);

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

		var convertFromResult = stringToListConverter.ConvertFrom(valueToConvert);
		var convertResult = (string[]?)((ICommunityToolkitValueConverter)stringToListConverter).Convert(valueToConvert, typeof(IList<string>), null, null);

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
	public void StringToListConverterNullStringsInListTest()
	{
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
		Assert.Throws<ArgumentException>(() => new StringToListConverter { Separators = new List<string?> { ",", null } });
		Assert.Throws<ArgumentException>(() => new StringToListConverter().Separators = new List<string?> { ",", null });
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
	}

	[Fact]
	public void StringToListConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new StringToListConverter { Separator = null });
		Assert.Throws<ArgumentNullException>(() => new StringToListConverter().Separator = null);
		Assert.Throws<ArgumentNullException>(() => new StringToListConverter { Separators = null });
		Assert.Throws<ArgumentNullException>(() => new StringToListConverter().Separators = null);
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new StringToListConverter()).Convert(string.Empty, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new StringToListConverter()).ConvertBack(new List<string>(), null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact]
	public void StringToListConverterEmptyStringInputTest()
	{
		Assert.Throws<ArgumentException>(() => new StringToListConverter { Separator = string.Empty });
		Assert.Throws<ArgumentException>(() => new StringToListConverter().Separator = string.Empty);
		Assert.Throws<ArgumentException>(() => new StringToListConverter { Separators = new List<string> { ",", string.Empty } });
		Assert.Throws<ArgumentException>(() => new StringToListConverter().Separators = new List<string> { ",", string.Empty });
		Assert.Throws<ArgumentException>(() => new StringToListConverter().ConvertFrom(string.Empty, string.Empty));
		Assert.Throws<ArgumentException>(() => new StringToListConverter().ConvertFrom(string.Empty, new[] { ",", "" }));
		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)new StringToListConverter()).Convert(string.Empty, typeof(IList<string>), string.Empty, null));
		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)new StringToListConverter()).Convert(string.Empty, typeof(IList<string>), new[] { ",", "" }, null));
	}
}