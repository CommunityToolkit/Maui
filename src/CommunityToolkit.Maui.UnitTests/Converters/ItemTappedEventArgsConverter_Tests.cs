﻿using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ItemTappedEventArgsConverter_Tests : BaseTest
{
	public static IReadOnlyList<object[]> Data { get; } = new[]
	{
		new object[] { new ItemTappedEventArgs("", 1, 1), 1},
		new object[] { new ItemTappedEventArgs("", 'c', 1), 'c'},
		new object[] { new ItemTappedEventArgs("", Colors.Black, 1), Colors.Black},
	};

	[Theory]
	[MemberData(nameof(Data))]
	public void ItemTappedEventArgsConverter(ItemTappedEventArgs value, object? expectedResult)
	{
		var itemTappedEventArgsConverter = new ItemTappedEventArgsConverter();

		var convertResult = ((ICommunityToolkitValueConverter)itemTappedEventArgsConverter).Convert(value, typeof(object), null, CultureInfo.CurrentCulture);
		var convertFromResult = itemTappedEventArgsConverter.ConvertFrom(value, typeof(object), null, CultureInfo.CurrentCulture);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[InlineData(5.5)]
	[InlineData('c')]
	[InlineData(true)]
	[InlineData("abc")]
	public void InvalidConverterValuesThrowArgumentException(object value)
	{
		var itemTappedEventArgsConverter = new ItemTappedEventArgsConverter();
		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)itemTappedEventArgsConverter).Convert(value, typeof(object), null, CultureInfo.CurrentCulture));
	}
}