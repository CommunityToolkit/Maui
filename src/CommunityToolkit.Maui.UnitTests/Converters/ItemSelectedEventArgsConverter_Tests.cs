﻿using System;
using System.Globalization;
using CommunityToolkit.Maui.Converters;
using NUnit.Framework;

namespace CommunityToolkit.Maui.UnitTests.Converters
{
    public class ItemSelectedEventArgsConverter_Tests
	{
		[TestCase("Random String")]
		public void InvalidConverterValuesThrowsArgumenException(object value)
		{
			var itemSelectedEventArgsConverter = new ItemSelectedEventArgsConverter();
			Assert.Throws<ArgumentException>(() => itemSelectedEventArgsConverter.Convert(value, typeof(ItemSelectedEventArgsConverter), null, CultureInfo.CurrentCulture));
		}
	}
}