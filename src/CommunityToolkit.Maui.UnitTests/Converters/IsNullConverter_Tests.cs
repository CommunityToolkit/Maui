using System.Globalization;
using CommunityToolkit.Maui.Converters;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class IsNullConverter_Tests : BaseTest
{
	[Theory]
	[ClassData(typeof(FooDataGenerator))]
	public void ObjectToBoolConverter(object? value, bool expectedResult)
	{
		var isNullConverter = new IsNullConverter();

		var result = ((ICommunityToolkitValueConverter)isNullConverter).Convert(value, typeof(bool), null, CultureInfo.CurrentCulture);
		var typedResult = isNullConverter.ConvertFrom(value);

		Assert.Equal(expectedResult, result);
		Assert.Equal(expectedResult, typedResult);
	}

	public class FooDataGenerator : TheoryData<object?, bool>
	{
		public FooDataGenerator()
		{
			Add(123, false);
			Add((int?)null, true);
			Add(123d, false);			
			Add((double?)null, true);
			Add(new List<string>(), false);
			Add((List<string>?)null, true);
			Add("test", false);
			Add((string?)null, true);
			Add(new IsNotNullConverter_Tests(), false);
			Add((IsNotNullConverter_Tests?)null, true);
		}
	}
}