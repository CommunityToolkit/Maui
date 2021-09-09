using System.Globalization;
using CommunityToolkit.Maui.Converters;
using NUnit.Framework;

namespace CommunityToolkit.Maui.UnitTests.Converters
{
    public class ItemTappedEventArgsConverter_Tests
	{
		[TestCase("Random String")]
		public void InValidConverterValuesThrowArgumenException(object value)
		{
			var itemTappedEventArgsConverter = new ItemTappedEventArgsConverter();
			Assert.Throws<ArgumentException>(() => itemTappedEventArgsConverter.Convert(value, typeof(ItemTappedEventArgsConverter), null, CultureInfo.CurrentCulture));
		}
	}
}