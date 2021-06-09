#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Microsoft.Maui.Controls;
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