using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class EqualConverter_Tests : BaseTest
{
	public const string TestValue = nameof(TestValue);

	[Theory]
	[InlineData(200, 200)]
	[InlineData(TestValue, TestValue)]
	public void IsEqual(object value, object comparedValue)
	{
		var isEqualConverter = new IsEqualConverter();

		var convertResult = (bool?)((ICommunityToolkitValueConverter)isEqualConverter).Convert(value, typeof(bool), comparedValue, CultureInfo.CurrentCulture);
		var covertFromResult = isEqualConverter.ConvertFrom(value, typeof(bool), comparedValue, CultureInfo.CurrentCulture);

		Assert.True(convertResult);
		Assert.True(covertFromResult);
	}

	[Theory]
	[InlineData(200, 400)]
	[InlineData(TestValue, "")]
	public void IsNotEqual(object value, object comparedValue)
	{
		var isEqualConverter = new IsEqualConverter();

		var convertResult = (bool?)((ICommunityToolkitValueConverter)isEqualConverter).Convert(value, typeof(bool), comparedValue, CultureInfo.CurrentCulture);
		var covertFromResult = isEqualConverter.ConvertFrom(value, typeof(bool), comparedValue, CultureInfo.CurrentCulture);

		Assert.False(convertResult);
		Assert.False(covertFromResult);
	}
}