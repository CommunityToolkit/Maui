using System.Globalization;
using CommunityToolkit.Maui.Converters;
using NUnit.Framework;

namespace CommunityToolkit.Maui.UnitTests.Converters
{
    public class NotEqualConverter_Tests
	{
		[TestCase(true, true, false)]
		[TestCase(int.MaxValue, int.MinValue, true)]
		[TestCase("Test", true, true)]
		[TestCase(null, null, false)]
		public void NotEqualConverter(object value, object comparedValue, bool expectedResult)
		{
			var notEqualConverter = new NotEqualConverter();

			var result = notEqualConverter.Convert(value, typeof(NotEqualConverter_Tests), comparedValue, CultureInfo.CurrentCulture);

			Assert.AreEqual(result, expectedResult);
		}
	}
}