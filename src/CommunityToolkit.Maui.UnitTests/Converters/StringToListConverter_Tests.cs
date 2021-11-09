﻿using System;
using System.Collections.Generic;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class StringToListConverter_Tests : BaseTest
{
    public static IReadOnlyList<object?[]> ListData { get; } = new[]
    {
        new object?[] { "A,B.C;D", new string[] { ",", ".", ";" }, new string[] { "A", "B", "C", "D" } },
        new object?[] { "A+_+B+_+C", "+_+", new string[] { "A", "B", "C" } },
        new object?[] { "A,,C", ",", new string[] { "A", string.Empty, "C" }, },
        new object?[] { "A,C", ",", new string?[] { "A", "C" } },
        new object?[] { "A", ":-:", new string[] { "A" } },
        new object?[] { string.Empty, ",", new string[] { string.Empty } },
        new object?[] { null, ",", Array.Empty<string>() },
        new object?[] { "ABC", null, new string[] { "ABC" } },
    };

    [Theory]
    [MemberData(nameof(ListData))]
    public void StringToListConverter(object? value, object? parameter, object? expectedResult)
    {
        var stringToListConverter = new StringToListConverter();

        var result = stringToListConverter.Convert(value, null, parameter, null);

        Assert.Equal(result, expectedResult);
    }

    [Theory]
    [InlineData(0)]
    public void InValidConverterValuesThrowArgumenException(object value)
    {
        var listToStringConverter = new ListToStringConverter();

        Assert.Throws<ArgumentException>(() => listToStringConverter.Convert(value, null, null, null));
    }

    [Theory]
    [InlineData(0)]
    public void InValidConverterParametersThrowArgumenException(object parameter)
    {
        var listToStringConverter = new ListToStringConverter();

        Assert.Throws<ArgumentException>(() => listToStringConverter.Convert(Array.Empty<object>(), null, parameter, null));
    }
}