using CommunityToolkit.Maui.Converters;
using NUnit.Framework;
using System.Globalization;

namespace CommunityToolkit.Maui.UnitTests.Converters
{
    public class InvertedBoolConverter_Tests
	{
		[TestCase(true, false)]
		[TestCase(false, true)]
		public void InverterBoolConverter(bool value, bool expectedResult)
		{
			var inverterBoolConverter = new InvertedBoolConverter();

			var result = inverterBoolConverter.Convert(value, typeof(InvertedBoolConverter_Tests), null, CultureInfo.CurrentCulture);

			Assert.AreEqual(result, expectedResult);
		}

		[TestCase(2)]
		[TestCase("")]
		[TestCase(null)]
		public void InValidConverterValuesThrowArgumenException(object value)
		{
			var inverterBoolConverter = new InvertedBoolConverter();
			Assert.Throws<ArgumentException>(() => inverterBoolConverter.Convert(value, typeof(IndexToArrayItemConverter), null, CultureInfo.CurrentCulture));
		}
	}
}