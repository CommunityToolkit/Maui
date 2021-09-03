#nullable enable
using System;
using System.Globalization;
using CommunityToolkit.Maui.Converters;
using NUnit.Framework;

namespace CommunityToolkit.Maui.UnitTests.Converters
{
    public class DoubleToIntConverter_Tests
	{
		[TestCase(2.5, 2)]
		[TestCase(2.55, 3)]
		[TestCase(2.555, 3)]
		[TestCase(2.555, 652, 255)]
		public void DoubleToIntConverter(double value, int expectedResult, object? ratio = null)
		{
			var doubleToIntConverter = new DoubleToIntConverter();

			var result = doubleToIntConverter.Convert(value, typeof(DoubleToIntConverter_Tests), ratio, CultureInfo.CurrentCulture);

			Assert.AreEqual(result, expectedResult);
		}

		[TestCase(2, 2)]
		public void DoubleToIntConverterBack(int value, double expectedResult, object? ratio = null)
		{
			var doubleToIntConverter = new DoubleToIntConverter();

			var result = doubleToIntConverter.ConvertBack(value, typeof(DoubleToIntConverter_Tests), ratio, CultureInfo.CurrentCulture);

			Assert.AreEqual(result, expectedResult);
		}

		[TestCase("")]
		public void DoubleToIntInValidValuesThrowArgumenException(object value)
		{
			var doubleToIntConverter = new DoubleToIntConverter();
			Assert.Throws<ArgumentException>(() => doubleToIntConverter.Convert(value, typeof(BoolToObjectConverter_Tests), null, CultureInfo.CurrentCulture));
			Assert.Throws<ArgumentException>(() => doubleToIntConverter.ConvertBack(value, typeof(BoolToObjectConverter_Tests), null, CultureInfo.CurrentCulture));
		}
	}
}