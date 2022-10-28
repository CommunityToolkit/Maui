using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class IsNotNullConverterTests : BaseTest
{
	[Theory]
	[ClassData(typeof(FooDataGenerator))]
	public void ObjectToBoolConverter(object? value, bool expectedResult)
	{
		var isNotNullConverter = new IsNotNullConverter();

		var result = ((ICommunityToolkitValueConverter)isNotNullConverter).Convert(value, typeof(bool), null, CultureInfo.CurrentCulture);
		var typedResult = isNotNullConverter.ConvertFrom(value);

		Assert.Equal(expectedResult, result);
		Assert.Equal(expectedResult, typedResult);
	}

	public class FooDataGenerator : TheoryData<object?, bool>
	{
		public FooDataGenerator()
		{
			Add(123, true);
			Add((int?)null, false);
			Add(123d, true);
			Add((double?)null, false);
			Add(new List<string>(), true);
			Add((List<string>?)null, false);
			Add("test", true);
			Add((string?)null, false);
			Add(new IsNotNullConverterTests(), true);
			Add((IsNotNullConverterTests?)null, false);
		}
	}
}