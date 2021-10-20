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
        var equalConverter = new EqualConverter();

        var result = equalConverter.Convert(value, typeof(EqualConverter_Tests), comparedValue, CultureInfo.CurrentCulture);

        Assert.IsType<bool>(result);
        Assert.True((bool)result);
    }

    [Theory]
    [InlineData(200, 400)]
    [InlineData(TestValue, "")]
    public void IsNotEqual(object value, object comparedValue)
    {
        var equalConverter = new EqualConverter();

        var result = equalConverter.Convert(value, typeof(EqualConverter_Tests), comparedValue, CultureInfo.CurrentCulture);

        Assert.IsType<bool>(result);
        Assert.False((bool)result);
    }
}