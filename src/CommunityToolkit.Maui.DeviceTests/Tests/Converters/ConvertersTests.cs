using System.Collections;
using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests.Tests.Converters;

public class InvertedBoolConverterTests
{
	[Theory]
	[InlineData(true, false)]
	[InlineData(false, true)]
	public void ConvertFrom_InvertsValue(bool input, bool expected)
	{
		var converter = new InvertedBoolConverter();

		var result = converter.ConvertFrom(input);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(true, false)]
	[InlineData(false, true)]
	public void ConvertBackTo_InvertsValue(bool input, bool expected)
	{
		var converter = new InvertedBoolConverter();

		var result = converter.ConvertBackTo(input);

		Assert.Equal(expected, result);
	}
}

public class IntToBoolConverterTests
{
	[Theory]
	[InlineData(0, false)]
	[InlineData(1, true)]
	[InlineData(-1, true)]
	[InlineData(42, true)]
	public void ConvertFrom_IntToBool(int input, bool expected)
	{
		var converter = new IntToBoolConverter();

		var result = converter.ConvertFrom(input);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(true, 1)]
	[InlineData(false, 0)]
	public void ConvertBackTo_BoolToInt(bool input, int expected)
	{
		var converter = new IntToBoolConverter();

		var result = converter.ConvertBackTo(input);

		Assert.Equal(expected, result);
	}
}

public class IsNullConverterTests
{
	[Fact]
	public void ConvertFrom_Null_ReturnsTrue()
	{
		var converter = new IsNullConverter();

		var result = converter.ConvertFrom(null);

		Assert.True(result);
	}

	[Fact]
	public void ConvertFrom_NonNull_ReturnsFalse()
	{
		var converter = new IsNullConverter();

		var result = converter.ConvertFrom("hello");

		Assert.False(result);
	}
}

public class IsNotNullConverterTests
{
	[Fact]
	public void ConvertFrom_Null_ReturnsFalse()
	{
		var converter = new IsNotNullConverter();

		var result = converter.ConvertFrom(null);

		Assert.False(result);
	}

	[Fact]
	public void ConvertFrom_NonNull_ReturnsTrue()
	{
		var converter = new IsNotNullConverter();

		var result = converter.ConvertFrom(42);

		Assert.True(result);
	}
}

public class IsEqualConverterTests
{
	[Fact]
	public void ConvertFrom_EqualValues_ReturnsTrue()
	{
		var converter = new IsEqualConverter();

		var result = converter.ConvertFrom("test", "test");

		Assert.True(result);
	}

	[Fact]
	public void ConvertFrom_DifferentValues_ReturnsFalse()
	{
		var converter = new IsEqualConverter();

		var result = converter.ConvertFrom("test", "other");

		Assert.False(result);
	}

	[Fact]
	public void ConvertFrom_BothNull_ReturnsTrue()
	{
		var converter = new IsEqualConverter();

		var result = converter.ConvertFrom(null, null);

		Assert.True(result);
	}

	[Fact]
	public void ConvertFrom_OneNull_ReturnsFalse()
	{
		var converter = new IsEqualConverter();

		var result = converter.ConvertFrom("test", null);

		Assert.False(result);
	}
}

public class IsNotEqualConverterTests
{
	[Fact]
	public void ConvertFrom_EqualValues_ReturnsFalse()
	{
		var converter = new IsNotEqualConverter();

		var result = converter.ConvertFrom(5, 5);

		Assert.False(result);
	}

	[Fact]
	public void ConvertFrom_DifferentValues_ReturnsTrue()
	{
		var converter = new IsNotEqualConverter();

		var result = converter.ConvertFrom(5, 10);

		Assert.True(result);
	}
}

public class IsStringNullOrEmptyConverterTests
{
	[Theory]
	[InlineData(null, true)]
	[InlineData("", true)]
	[InlineData("hello", false)]
	[InlineData(" ", false)]
	public void ConvertFrom_ChecksNullOrEmpty(string? input, bool expected)
	{
		var converter = new IsStringNullOrEmptyConverter();

		var result = converter.ConvertFrom(input);

		Assert.Equal(expected, result);
	}
}

public class IsStringNotNullOrEmptyConverterTests
{
	[Theory]
	[InlineData(null, false)]
	[InlineData("", false)]
	[InlineData("hello", true)]
	[InlineData(" ", true)]
	public void ConvertFrom_ChecksNotNullOrEmpty(string? input, bool expected)
	{
		var converter = new IsStringNotNullOrEmptyConverter();

		var result = converter.ConvertFrom(input);

		Assert.Equal(expected, result);
	}
}

public class IsStringNullOrWhiteSpaceConverterTests
{
	[Theory]
	[InlineData(null, true)]
	[InlineData("", true)]
	[InlineData("   ", true)]
	[InlineData("hello", false)]
	public void ConvertFrom_ChecksNullOrWhiteSpace(string? input, bool expected)
	{
		var converter = new IsStringNullOrWhiteSpaceConverter();

		var result = converter.ConvertFrom(input);

		Assert.Equal(expected, result);
	}
}

public class IsStringNotNullOrWhiteSpaceConverterTests
{
	[Theory]
	[InlineData(null, false)]
	[InlineData("", false)]
	[InlineData("   ", false)]
	[InlineData("hello", true)]
	public void ConvertFrom_ChecksNotNullOrWhiteSpace(string? input, bool expected)
	{
		var converter = new IsStringNotNullOrWhiteSpaceConverter();

		var result = converter.ConvertFrom(input);

		Assert.Equal(expected, result);
	}
}

public class IsListNullOrEmptyConverterTests
{
	[Fact]
	public void ConvertFrom_Null_ReturnsTrue()
	{
		var converter = new IsListNullOrEmptyConverter();

		var result = converter.ConvertFrom(null);

		Assert.True(result);
	}

	[Fact]
	public void ConvertFrom_EmptyList_ReturnsTrue()
	{
		var converter = new IsListNullOrEmptyConverter();

		var result = converter.ConvertFrom(new List<int>());

		Assert.True(result);
	}

	[Fact]
	public void ConvertFrom_NonEmptyList_ReturnsFalse()
	{
		var converter = new IsListNullOrEmptyConverter();

		var result = converter.ConvertFrom(new List<int> { 1, 2, 3 });

		Assert.False(result);
	}
}

public class IsListNotNullOrEmptyConverterTests
{
	[Fact]
	public void ConvertFrom_Null_ReturnsFalse()
	{
		var converter = new IsListNotNullOrEmptyConverter();

		var result = converter.ConvertFrom(null);

		Assert.False(result);
	}

	[Fact]
	public void ConvertFrom_EmptyList_ReturnsFalse()
	{
		var converter = new IsListNotNullOrEmptyConverter();

		var result = converter.ConvertFrom(new List<int>());

		Assert.False(result);
	}

	[Fact]
	public void ConvertFrom_NonEmptyList_ReturnsTrue()
	{
		var converter = new IsListNotNullOrEmptyConverter();

		var result = converter.ConvertFrom(new List<int> { 1 });

		Assert.True(result);
	}
}

public class BoolToObjectConverterTests
{
	[Fact]
	public void ConvertFrom_True_ReturnsTrueObject()
	{
		var converter = new BoolToObjectConverter<string>
		{
			TrueObject = "Yes",
			FalseObject = "No"
		};

		var result = converter.ConvertFrom(true);

		Assert.Equal("Yes", result);
	}

	[Fact]
	public void ConvertFrom_False_ReturnsFalseObject()
	{
		var converter = new BoolToObjectConverter<string>
		{
			TrueObject = "Yes",
			FalseObject = "No"
		};

		var result = converter.ConvertFrom(false);

		Assert.Equal("No", result);
	}

	[Fact]
	public void ConvertBackTo_TrueObject_ReturnsTrue()
	{
		var converter = new BoolToObjectConverter<string>
		{
			TrueObject = "Yes",
			FalseObject = "No"
		};

		var result = converter.ConvertBackTo("Yes");

		Assert.True(result);
	}

	[Fact]
	public void ConvertBackTo_FalseObject_ReturnsFalse()
	{
		var converter = new BoolToObjectConverter<string>
		{
			TrueObject = "Yes",
			FalseObject = "No"
		};

		var result = converter.ConvertBackTo("No");

		Assert.False(result);
	}
}

public class DoubleToIntConverterTests
{
	[Theory]
	[InlineData(3.7, 4)]
	[InlineData(3.2, 3)]
	[InlineData(0.0, 0)]
	[InlineData(-2.5, -2)]
	public void ConvertFrom_DoubleToInt(double input, int expected)
	{
		var converter = new DoubleToIntConverter();

		var result = converter.ConvertFrom(input);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(5, 5.0)]
	[InlineData(0, 0.0)]
	[InlineData(-3, -3.0)]
	public void ConvertBackTo_IntToDouble(int input, double expected)
	{
		var converter = new DoubleToIntConverter();

		var result = converter.ConvertBackTo(input);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void ConvertFrom_WithRatio_MultipliesBeforeRounding()
	{
		var converter = new DoubleToIntConverter
		{
			Ratio = 2.0
		};

		var result = converter.ConvertFrom(3.0);

		Assert.Equal(6, result);
	}
}

public class DateTimeOffsetConverterTests
{
	[Fact]
	public void ConvertFrom_DateTimeOffsetToDateTime()
	{
		var converter = new DateTimeOffsetConverter();
		var dateTimeOffset = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);

		var result = converter.ConvertFrom(dateTimeOffset);

		Assert.Equal(new DateTime(2024, 6, 15, 10, 30, 0), result);
	}

	[Fact]
	public void ConvertBackTo_DateTimeToDateTimeOffset()
	{
		var converter = new DateTimeOffsetConverter();
		var dateTime = new DateTime(2024, 6, 15, 10, 30, 0);

		var result = converter.ConvertBackTo(dateTime);

		Assert.Equal(dateTime, result.DateTime);
	}
}

public class TimeSpanToSecondsConverterTests
{
	[Fact]
	public void ConvertFrom_TimeSpanToSeconds()
	{
		var converter = new TimeSpanToSecondsConverter();
		var timeSpan = TimeSpan.FromMinutes(2.5);

		var result = converter.ConvertFrom(timeSpan);

		Assert.Equal(150.0, result);
	}

	[Fact]
	public void ConvertBackTo_SecondsToTimeSpan()
	{
		var converter = new TimeSpanToSecondsConverter();

		var result = converter.ConvertBackTo(90.0);

		Assert.Equal(TimeSpan.FromSeconds(90), result);
	}
}

public class EnumToBoolConverterTests
{
	[Fact]
	public void ConvertFrom_MatchingEnum_ReturnsTrue()
	{
		var converter = new EnumToBoolConverter();

		var result = converter.ConvertFrom(DayOfWeek.Monday, DayOfWeek.Monday);

		Assert.True(result);
	}

	[Fact]
	public void ConvertFrom_DifferentEnum_ReturnsFalse()
	{
		var converter = new EnumToBoolConverter();

		var result = converter.ConvertFrom(DayOfWeek.Monday, DayOfWeek.Tuesday);

		Assert.False(result);
	}

	[Fact]
	public void ConvertFrom_FlaggedEnum_WithMultipleFlags_ReturnsTrue()
	{
		var converter = new EnumToBoolConverter();

		// DayOfWeek is not a [Flags] enum, so use a real flags enum (StringSplitOptions).
		// The converter checks referenceEnumValue.HasFlag(valueToCheck) for flags enums.
		var result = converter.ConvertFrom(
			StringSplitOptions.RemoveEmptyEntries,
			StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		Assert.True(result);
	}
}

public class EnumToIntConverterTests
{
	[Fact]
	public void ConvertFrom_EnumToInt()
	{
		var converter = new EnumToIntConverter();

		var result = converter.ConvertFrom(DayOfWeek.Wednesday, typeof(DayOfWeek));

		Assert.Equal(3, result);
	}

	[Fact]
	public void ConvertBackTo_IntToEnum()
	{
		var converter = new EnumToIntConverter();

		var result = converter.ConvertBackTo(3, typeof(DayOfWeek));

		Assert.Equal(DayOfWeek.Wednesday, result);
	}
}

public class IndexToArrayItemConverterTests
{
	[Fact]
	public void ConvertFrom_ValidIndex_ReturnsItem()
	{
		var converter = new IndexToArrayItemConverter();
		var array = new[] { "a", "b", "c" };

		var result = converter.ConvertFrom(1, array);

		Assert.Equal("b", result);
	}

	[Fact]
	public void ConvertFrom_FirstIndex_ReturnsFirstItem()
	{
		var converter = new IndexToArrayItemConverter();
		var array = new[] { 10, 20, 30 };

		var result = converter.ConvertFrom(0, array);

		Assert.Equal(10, result);
	}

	[Fact]
	public void ConvertBackTo_FindsIndex()
	{
		var converter = new IndexToArrayItemConverter();
		var array = new[] { "x", "y", "z" };

		var result = converter.ConvertBackTo("y", array);

		Assert.Equal(1, result);
	}
}

public class ListToStringConverterTests
{
	[Fact]
	public void ConvertFrom_JoinsWithDefaultSeparator()
	{
		var converter = new ListToStringConverter();
		var list = new List<string> { "a", "b", "c" };

		// The default Separator is string.Empty, so items are concatenated with no delimiter.
		var result = converter.ConvertFrom(list);

		Assert.Equal("abc", result);
	}

	[Fact]
	public void ConvertFrom_JoinsWithCustomSeparator()
	{
		var converter = new ListToStringConverter();
		var list = new List<string> { "a", "b", "c" };

		var result = converter.ConvertFrom(list, " | ");

		Assert.Equal("a | b | c", result);
	}

	[Fact]
	public void ConvertFrom_EmptyList_ReturnsEmpty()
	{
		var converter = new ListToStringConverter();
		var list = new List<string>();

		var result = converter.ConvertFrom(list);

		Assert.Equal(string.Empty, result);
	}
}

public class StringToListConverterTests
{
	[Fact]
	public void ConvertFrom_SplitsWithDefaultSeparator()
	{
		var converter = new StringToListConverter();

		// The default Separator is a single space, so split on spaces.
		var result = converter.ConvertFrom("a b c").ToList();

		Assert.Equal(new[] { "a", "b", "c" }, result);
	}

	[Fact]
	public void ConvertFrom_SplitsWithCustomSeparator()
	{
		var converter = new StringToListConverter();

		var result = converter.ConvertFrom("a | b | c", " | ");

		Assert.Equal(new[] { "a", "b", "c" }, result);
	}

	[Fact]
	public void ConvertFrom_NullInput_ReturnsEmpty()
	{
		var converter = new StringToListConverter();

		var result = converter.ConvertFrom(null);

		Assert.Empty(result);
	}
}

public class TextCaseConverterTests
{
	[Theory]
	[InlineData("hello", TextCaseType.Upper, "HELLO")]
	[InlineData("HELLO", TextCaseType.Lower, "hello")]
	[InlineData("hello world", TextCaseType.Upper, "HELLO WORLD")]
	public void ConvertFrom_ChangesCase(string input, TextCaseType caseType, string expected)
	{
		var converter = new TextCaseConverter();

		var result = converter.ConvertFrom(input, caseType);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void ConvertFrom_NullInput_ReturnsNull()
	{
		var converter = new TextCaseConverter();

		var result = converter.ConvertFrom(null, TextCaseType.Upper);

		Assert.Null(result);
	}
}

public class CompareConverterTests
{
	[Fact]
	public void ConvertFrom_Greater_ReturnsTrue()
	{
		var converter = new CompareConverter
		{
			ComparingValue = 5,
			ComparisonOperator = CompareConverter.OperatorType.Greater
		};

		var result = converter.ConvertFrom(10);

		Assert.Equal(true, result);
	}

	[Fact]
	public void ConvertFrom_NotGreater_ReturnsFalse()
	{
		var converter = new CompareConverter
		{
			ComparingValue = 10,
			ComparisonOperator = CompareConverter.OperatorType.Greater
		};

		var result = converter.ConvertFrom(5);

		Assert.Equal(false, result);
	}

	[Fact]
	public void ConvertFrom_Equal_ReturnsTrue()
	{
		var converter = new CompareConverter
		{
			ComparingValue = 5,
			ComparisonOperator = CompareConverter.OperatorType.Equal
		};

		var result = converter.ConvertFrom(5);

		Assert.Equal(true, result);
	}

	[Fact]
	public void ConvertFrom_Smaller_ReturnsTrue()
	{
		var converter = new CompareConverter
		{
			ComparingValue = 10,
			ComparisonOperator = CompareConverter.OperatorType.Smaller
		};

		var result = converter.ConvertFrom(5);

		Assert.Equal(true, result);
	}

	[Fact]
	public void ConvertFrom_WithTrueFalseObjects_ReturnsObjects()
	{
		var converter = new CompareConverter
		{
			ComparingValue = 5,
			ComparisonOperator = CompareConverter.OperatorType.Greater,
			TrueObject = "Big",
			FalseObject = "Small"
		};

		var result = converter.ConvertFrom(10);

		Assert.Equal("Big", result);
	}

	[Fact]
	public void ConvertFrom_GreaterOrEqual_EqualValues_ReturnsTrue()
	{
		var converter = new CompareConverter
		{
			ComparingValue = 5,
			ComparisonOperator = CompareConverter.OperatorType.GreaterOrEqual
		};

		var result = converter.ConvertFrom(5);

		Assert.Equal(true, result);
	}

	[Fact]
	public void ConvertFrom_SmallerOrEqual_EqualValues_ReturnsTrue()
	{
		var converter = new CompareConverter
		{
			ComparingValue = 5,
			ComparisonOperator = CompareConverter.OperatorType.SmallerOrEqual
		};

		var result = converter.ConvertFrom(5);

		Assert.Equal(true, result);
	}

	[Fact]
	public void ConvertFrom_NotEqual_DifferentValues_ReturnsTrue()
	{
		var converter = new CompareConverter
		{
			ComparingValue = 5,
			ComparisonOperator = CompareConverter.OperatorType.NotEqual
		};

		var result = converter.ConvertFrom(10);

		Assert.Equal(true, result);
	}
}

public class IsInRangeConverterTests
{
	[Fact]
	public void ConvertFrom_ValueInRange_ReturnsTrue()
	{
		var converter = new IsInRangeConverter
		{
			MinValue = 1,
			MaxValue = 10
		};

		var result = converter.ConvertFrom(5, null);

		Assert.Equal(true, result);
	}

	[Fact]
	public void ConvertFrom_ValueOutOfRange_ReturnsFalse()
	{
		var converter = new IsInRangeConverter
		{
			MinValue = 1,
			MaxValue = 10
		};

		var result = converter.ConvertFrom(15, null);

		Assert.Equal(false, result);
	}

	[Fact]
	public void ConvertFrom_ValueAtMinBoundary_ReturnsTrue()
	{
		var converter = new IsInRangeConverter
		{
			MinValue = 1,
			MaxValue = 10
		};

		var result = converter.ConvertFrom(1, null);

		Assert.Equal(true, result);
	}

	[Fact]
	public void ConvertFrom_ValueAtMaxBoundary_ReturnsTrue()
	{
		var converter = new IsInRangeConverter
		{
			MinValue = 1,
			MaxValue = 10
		};

		var result = converter.ConvertFrom(10, null);

		Assert.Equal(true, result);
	}

	[Fact]
	public void ConvertFrom_OnlyMinValue_SetAboveMin_ReturnsTrue()
	{
		var converter = new IsInRangeConverter
		{
			MinValue = 5
		};

		var result = converter.ConvertFrom(10, null);

		Assert.Equal(true, result);
	}

	[Fact]
	public void ConvertFrom_OnlyMaxValue_SetBelowMax_ReturnsTrue()
	{
		var converter = new IsInRangeConverter
		{
			MaxValue = 10
		};

		var result = converter.ConvertFrom(5, null);

		Assert.Equal(true, result);
	}

	[Fact]
	public void ConvertFrom_NoMinMax_ThrowsArgumentException()
	{
		var converter = new IsInRangeConverter();
		var thrown = false;

		try
		{
			converter.ConvertFrom(5, null);
		}
		catch (ArgumentException)
		{
			thrown = true;
		}

		Assert.True(thrown);
	}

	[Fact]
	public void ConvertFrom_WithTrueFalseObjects_ReturnsObjects()
	{
		var converter = new IsInRangeConverter
		{
			MinValue = 1,
			MaxValue = 10,
			TrueObject = "In Range",
			FalseObject = "Out of Range"
		};

		var result = converter.ConvertFrom(5, null);

		Assert.Equal("In Range", result);
	}
}

public class MathExpressionConverterTests
{
	[Theory]
	[InlineData(10.0, "x*2", 20.0)]
	[InlineData(5.0, "x+3", 8.0)]
	[InlineData(10.0, "x/2", 5.0)]
	[InlineData(3.0, "x^2", 9.0)]
	public void ConvertFrom_EvaluatesExpression(double input, string expression, double expected)
	{
		var converter = new MathExpressionConverter();

		var result = converter.ConvertFrom(input, expression);

		Assert.NotNull(result);
		Assert.Equal(expected, Convert.ToDouble(result));
	}
}

public class VariableMultiValueConverterTests
{
	[Fact]
	public void Convert_AllTrue_ReturnsTrue()
	{
		var converter = new VariableMultiValueConverter
		{
			ConditionType = MultiBindingCondition.All
		};

		var result = converter.Convert([true, true, true], typeof(bool));

		Assert.Equal(true, result);
	}

	[Fact]
	public void Convert_All_OneFalse_ReturnsFalse()
	{
		var converter = new VariableMultiValueConverter
		{
			ConditionType = MultiBindingCondition.All
		};

		var result = converter.Convert([true, false, true], typeof(bool));

		Assert.Equal(false, result);
	}

	[Fact]
	public void Convert_Any_OneTrue_ReturnsTrue()
	{
		var converter = new VariableMultiValueConverter
		{
			ConditionType = MultiBindingCondition.Any
		};

		var result = converter.Convert([false, true, false], typeof(bool));

		Assert.Equal(true, result);
	}

	[Fact]
	public void Convert_Any_AllFalse_ReturnsFalse()
	{
		var converter = new VariableMultiValueConverter
		{
			ConditionType = MultiBindingCondition.Any
		};

		var result = converter.Convert([false, false, false], typeof(bool));

		Assert.Equal(false, result);
	}

	[Fact]
	public void Convert_None_AllFalse_ReturnsTrue()
	{
		var converter = new VariableMultiValueConverter
		{
			ConditionType = MultiBindingCondition.None
		};

		var result = converter.Convert([false, false], typeof(bool));

		Assert.Equal(true, result);
	}

	[Fact]
	public void Convert_None_OneTrue_ReturnsFalse()
	{
		var converter = new VariableMultiValueConverter
		{
			ConditionType = MultiBindingCondition.None
		};

		var result = converter.Convert([false, true], typeof(bool));

		Assert.Equal(false, result);
	}

	[Fact]
	public void Convert_ExactCount_MatchesCount_ReturnsTrue()
	{
		var converter = new VariableMultiValueConverter
		{
			ConditionType = MultiBindingCondition.Exact,
			Count = 2
		};

		var result = converter.Convert([true, true, false], typeof(bool));

		Assert.Equal(true, result);
	}

	[Fact]
	public void Convert_GreaterThan_MoreTrue_ReturnsTrue()
	{
		var converter = new VariableMultiValueConverter
		{
			ConditionType = MultiBindingCondition.GreaterThan,
			Count = 1
		};

		var result = converter.Convert([true, true, false], typeof(bool));

		Assert.Equal(true, result);
	}
}

public class StateToBooleanConverterTests
{
	[Fact]
	public void ConvertFrom_MatchingState_ReturnsTrue()
	{
		var converter = new StateToBooleanConverter();

		var result = converter.ConvertFrom(LayoutState.Loading, LayoutState.Loading);

		Assert.True(result);
	}

	[Fact]
	public void ConvertFrom_DifferentState_ReturnsFalse()
	{
		var converter = new StateToBooleanConverter();

		var result = converter.ConvertFrom(LayoutState.Loading, LayoutState.Error);

		Assert.False(result);
	}

	[Fact]
	public void ConvertFrom_DefaultParameter_UsesNone()
	{
		var converter = new StateToBooleanConverter();

		var result = converter.ConvertFrom(LayoutState.None);

		Assert.True(result);
	}
}

public class SelectedItemEventArgsConverterTests
{
	[Fact]
	public void ConvertFrom_ExtractsSelectedItem()
	{
		var converter = new SelectedItemEventArgsConverter();
		var args = new SelectedItemChangedEventArgs("TestItem", 0);

		var result = converter.ConvertFrom(args);

		Assert.Equal("TestItem", result);
	}

	[Fact]
	public void ConvertFrom_Null_ReturnsNull()
	{
		var converter = new SelectedItemEventArgsConverter();

		var result = converter.ConvertFrom(null);

		Assert.Null(result);
	}
}

public class ColorConverterTests
{
	[Fact]
	public void ColorToBlackOrWhiteConverter_BlackInput_ReturnsBlack()
	{
		var converter = new ColorToBlackOrWhiteConverter();

		var result = converter.ConvertFrom(Colors.Black);

		Assert.Equal(Colors.Black, result);
	}

	[Fact]
	public void ColorToBlackOrWhiteConverter_WhiteInput_ReturnsWhite()
	{
		var converter = new ColorToBlackOrWhiteConverter();

		var result = converter.ConvertFrom(Colors.White);

		Assert.Equal(Colors.White, result);
	}

	[Fact]
	public void ColorToInverseColorConverter_InvertsColor()
	{
		var converter = new ColorToInverseColorConverter();

		var result = converter.ConvertFrom(new Color(0.2f, 0.4f, 0.6f));

		Assert.NotNull(result);
		Assert.Equal(0.8f, result.Red, 0.01f);
		Assert.Equal(0.6f, result.Green, 0.01f);
		Assert.Equal(0.4f, result.Blue, 0.01f);
	}

	[Fact]
	public void ColorToGrayScaleColorConverter_ConvertsToGrayScale()
	{
		var converter = new ColorToGrayScaleColorConverter();

		var result = converter.ConvertFrom(new Color(1.0f, 0.0f, 0.0f));

		Assert.NotNull(result);
		Assert.Equal(result.Red, result.Green, 0.01f);
		Assert.Equal(result.Green, result.Blue, 0.01f);
	}
}

public class ColorToComponentConverterTests
{
	[Fact]
	public void ColorToByteRedConverter_ExtractsRed()
	{
		var converter = new ColorToByteRedConverter();

		var result = converter.ConvertFrom(new Color(1.0f, 0.0f, 0.0f));

		Assert.Equal((byte)255, result);
	}

	[Fact]
	public void ColorToByteGreenConverter_ExtractsGreen()
	{
		var converter = new ColorToByteGreenConverter();

		var result = converter.ConvertFrom(new Color(0.0f, 1.0f, 0.0f));

		Assert.Equal((byte)255, result);
	}

	[Fact]
	public void ColorToByteBlueConverter_ExtractsBlue()
	{
		var converter = new ColorToByteBlueConverter();

		var result = converter.ConvertFrom(new Color(0.0f, 0.0f, 1.0f));

		Assert.Equal((byte)255, result);
	}

	[Fact]
	public void ColorToByteAlphaConverter_ExtractsAlpha()
	{
		var converter = new ColorToByteAlphaConverter();

		var result = converter.ConvertFrom(new Color(0.0f, 0.0f, 0.0f, 0.5f));

		Assert.Equal((byte)128, result);
	}
}