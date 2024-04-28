using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class TextCaseConverterTests : BaseOneWayConverterTest<TextCaseConverter>
{
	const string test = nameof(test);
	const string t = nameof(t);

	public static IReadOnlyList<object?[]> Data { get; } =
	[
		[test, TextCaseType.Lower, test],
		[test, TextCaseType.Upper, "TEST"],
		[test, TextCaseType.None, test],
		[test, TextCaseType.FirstUpperRestLower, "Test"],
		[t, TextCaseType.Upper, "T"],
		[t, TextCaseType.Lower, t],
		[t, TextCaseType.None, t],
		[t, TextCaseType.FirstUpperRestLower, "T"],
		[string.Empty, TextCaseType.FirstUpperRestLower, string.Empty],
		[null, TextCaseType.None, null],
		[MockEnum.Foo, TextCaseType.Lower, "foo"],
		[MockEnum.Bar, TextCaseType.None, "Bar"],
		[MockEnum.Baz, TextCaseType.Upper, "BAZ"],
		[new MockItem("Test Item", true), TextCaseType.Upper, "TEST ITEM IS COMPLETED"],
	];

	enum MockEnum { Foo, Bar, Baz }

	[Theory]
	[InlineData((TextCaseType)int.MinValue)]
	[InlineData((TextCaseType)(-1))]
	[InlineData((TextCaseType)4)]
	[InlineData((TextCaseType)int.MaxValue)]
	public void InvalidTextCaseEnumThrowsNotSupportedException(TextCaseType textCaseType)
	{
		var textCaseConverter = new TextCaseConverter();

		Assert.Throws<InvalidEnumArgumentException>(() => new TextCaseConverter { Type = textCaseType });
		Assert.Throws<InvalidEnumArgumentException>(() => textCaseConverter.Type = textCaseType);
		Assert.Throws<InvalidEnumArgumentException>(() => ((ICommunityToolkitValueConverter)textCaseConverter).Convert("Hello World", typeof(string), textCaseType, null));
		Assert.Throws<InvalidEnumArgumentException>(() => textCaseConverter.ConvertFrom("Hello World", textCaseType));
		Assert.Throws<InvalidEnumArgumentException>(() => ((ICommunityToolkitValueConverter)textCaseConverter).Convert("Hello World", typeof(string), textCaseType, null));
	}

	[Fact]
	public void TextCaseConverterDefaultValues()
	{
		var textCaseConverter = new TextCaseConverter();

		Assert.Equal(TextCaseType.None, textCaseConverter.Type);
	}

	[Theory]
	[MemberData(nameof(Data))]
	[InlineData(null, null, null)]
	public void TextCaseConverterWithParameter(object? value, TextCaseType? comparedValue, object? expectedResult)
	{
		var textCaseConverter = new TextCaseConverter();

		var convertResult = ((ICommunityToolkitValueConverter)textCaseConverter).Convert(value?.ToString(), typeof(string), comparedValue, CultureInfo.CurrentCulture);
		var convertFromResult = textCaseConverter.ConvertFrom(value?.ToString(), comparedValue);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[MemberData(nameof(Data))]
	public void TextCaseConverterWithExplicitType(object? value, TextCaseType textCaseType, object? expectedResult)
	{
		var textCaseConverter = new TextCaseConverter
		{
			Type = textCaseType
		};

		var convertResult = ((ICommunityToolkitValueConverter)textCaseConverter).Convert(value?.ToString(), typeof(string), null, CultureInfo.CurrentCulture);
		var convertFromResult = textCaseConverter.ConvertFrom(value?.ToString());

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Fact]
	public void TextCaseConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new TextCaseConverter()).Convert(string.Empty, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new TextCaseConverter()).ConvertBack(string.Empty, null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	record MockItem(string Title, bool Completed)
	{
		public override string ToString() => Completed ? $"{Title} is completed" : $"{Title} has yet to be completed";
	}
}