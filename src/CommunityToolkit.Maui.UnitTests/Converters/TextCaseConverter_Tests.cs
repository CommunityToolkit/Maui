#nullable enable
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.UnitTests.Mocks;
using NUnit.Framework;
using System.Globalization;

namespace CommunityToolkit.Maui.UnitTests.Converters
{
    public class TextCaseConverter_Tests
	{
		const string test = nameof(test);
		const string t = nameof(t);

		static IEnumerable<object?[]> GetTestData()
		{
			yield return new object?[] { test, TextCaseType.Lower, test };
			yield return new object?[] { test, TextCaseType.Upper, "TEST" };
			yield return new object?[] { test, TextCaseType.None, test };
			yield return new object?[] { test, TextCaseType.FirstUpperRestLower, "Test" };
			yield return new object?[] { t, TextCaseType.Upper, "T" };
			yield return new object?[] { t, TextCaseType.Lower, t };
			yield return new object?[] { t, TextCaseType.None, t };
			yield return new object?[] { t, TextCaseType.FirstUpperRestLower, "T" };
			yield return new object?[] { string.Empty, TextCaseType.FirstUpperRestLower, string.Empty };
			yield return new object?[] { null, TextCaseType.None, null };
			yield return new object?[] { MockEnum.Foo, TextCaseType.Lower, "foo" };
			yield return new object?[] { MockEnum.Bar, TextCaseType.None, "Bar" };
			yield return new object?[] { MockEnum.Baz, TextCaseType.Upper, "BAZ" };
			yield return new object?[] { new MockItem { Title = "Test Item", Completed = true }, TextCaseType.Upper, "TEST ITEM IS COMPLETED" };
		}

		[TestCaseSource(nameof(GetTestData))]
		[TestCase(null, null, null)]
		public void TextCaseConverterWithParameter(object? value, object? comparedValue, object? expectedResult)
		{
			var textCaseConverter = new TextCaseConverter();

			var result = textCaseConverter.Convert(value, typeof(string), comparedValue, CultureInfo.CurrentCulture);

			Assert.AreEqual(result, expectedResult);
		}

		[TestCaseSource(nameof(GetTestData))]
		public void TextCaseConverterWithExplicitType(object? value, TextCaseType textCaseType, object? expectedResult)
		{
			var textCaseConverter = new TextCaseConverter
			{
				Type = textCaseType
			};

			var result = textCaseConverter.Convert(value, typeof(string), null, CultureInfo.CurrentCulture);

			Assert.AreEqual(result, expectedResult);
		}
	}
}