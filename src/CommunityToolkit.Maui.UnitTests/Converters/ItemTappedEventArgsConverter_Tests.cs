using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ItemTappedEventArgsConverter_Tests : BaseTest
{
	[Theory]
	[InlineData("Random String")]
	public void InValidConverterValuesThrowArgumenException(object value)
	{
		var itemTappedEventArgsConverter = new ItemTappedEventArgsConverter();
		Assert.Throws<ArgumentException>(() => itemTappedEventArgsConverter.Convert(value, typeof(object), null, CultureInfo.CurrentCulture));
	}
}