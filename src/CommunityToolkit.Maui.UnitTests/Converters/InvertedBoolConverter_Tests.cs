﻿using System;
using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters
{
    public class InvertedBoolConverter_Tests : BaseTest
	{
		[Theory]
		[InlineData(true, false)]
		[InlineData(false, true)]
		public void InverterBoolConverter(bool value, bool expectedResult)
		{
			var inverterBoolConverter = new InvertedBoolConverter();

			var result = inverterBoolConverter.Convert(value, typeof(InvertedBoolConverter_Tests), null, CultureInfo.CurrentCulture);

			Assert.Equal(result, expectedResult);
		}

		[Theory]
		[InlineData(2)]
		[InlineData("")]
		[InlineData(null)]
		public void InValidConverterValuesThrowArgumenException(object? value)
		{
			var inverterBoolConverter = new InvertedBoolConverter();
			Assert.Throws<ArgumentException>(() => inverterBoolConverter.Convert(value, typeof(IndexToArrayItemConverter), null, CultureInfo.CurrentCulture));
		}
	}
}