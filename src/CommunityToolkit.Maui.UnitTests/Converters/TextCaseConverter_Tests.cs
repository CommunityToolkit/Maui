using System.Collections.Generic;
using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class TextCaseConverter_Tests : BaseTest
{
	const string test = nameof(test);
	const string t = nameof(t);

	public static IReadOnlyList<object?[]> Data { get; } = new[]
	{
			new object?[] { test, TextCaseType.Lower, test },
			new object?[] { test, TextCaseType.Upper, "TEST" },
			new object?[] { test, TextCaseType.None, test },
			new object?[] { test, TextCaseType.FirstUpperRestLower, "Test" },
			new object?[] { t, TextCaseType.Upper, "T" },
			new object?[] { t, TextCaseType.Lower, t },
			new object?[] { t, TextCaseType.None, t },
			new object?[] { t, TextCaseType.FirstUpperRestLower, "T" },
			new object?[] { string.Empty, TextCaseType.FirstUpperRestLower, string.Empty },
			new object?[] { null, TextCaseType.None, null },
			new object?[] { MockEnum.Foo, TextCaseType.Lower, "foo" },
			new object?[] { MockEnum.Bar, TextCaseType.None, "Bar" },
			new object?[] { MockEnum.Baz, TextCaseType.Upper, "BAZ" },
			new object?[] { new MockItem { Title = "Test Item", Completed = true }, TextCaseType.Upper, "TEST ITEM IS COMPLETED" },
		};

	enum MockEnum { Foo, Bar, Baz }

	[Theory]
	[MemberData(nameof(Data))]
	[InlineData(null, null, null)]
	public void TextCaseConverterWithParameter(object? value, object? comparedValue, object? expectedResult)
	{
		var textCaseConverter = new TextCaseConverter();

		var result = textCaseConverter.Convert(value, typeof(string), comparedValue, CultureInfo.CurrentCulture);

		Assert.Equal(result, expectedResult);
	}

	[Theory]
	[MemberData(nameof(Data))]
	public void TextCaseConverterWithExplicitType(object? value, TextCaseType textCaseType, object? expectedResult)
	{
		var textCaseConverter = new TextCaseConverter
		{
			Type = textCaseType
		};

		var result = textCaseConverter.Convert(value, typeof(string), null, CultureInfo.CurrentCulture);

		Assert.Equal(result, expectedResult);
	}

	class MockItem
	{
		public string? Title { get; set; }

		public bool Completed { get; set; }

		public override string ToString() => Completed ? $"{Title} is completed" : $"{Title} has yet to be completed";
	}
}