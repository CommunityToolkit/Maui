using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class EnumToBoolConverter_Tests : BaseTest
{
	enum TestEnumForEnumToBoolConverter
	{
		None = 0,
		One = 1,
		Two = 2,
		Three = 3,
		Four = 4,
		Five = 5,
		Six = 6
	}

	[Flags]
	enum TestFlaggedEnumForEnumToBoolConverter
	{
		None = 0b0000,
		One = 0b0001,
		Two = 0b0010,
		Three = 0b0100,
		Four = 0b1000
	}

	[Fact]
	public void EnumToBoolConvertBack_ThrowsNotSupportedException()
	{
		var enumToBoolConverter = new EnumToBoolConverter();

		Assert.Throws<NotSupportedException>(() =>
			enumToBoolConverter.ConvertBack(TestEnumForEnumToBoolConverter.Five, typeof(bool), null, CultureInfo.InvariantCulture));
	}

	[Theory]
	[InlineData("a string")]
	[InlineData(42)]
	[InlineData(null)]
	[InlineData(false)]
	public void EnumToBoolConvert_ValueNotEnum_ThrowsArgumentException(object value)
	{
		var enumToBoolConverter = new EnumToBoolConverter();

		Assert.Throws<ArgumentException>(() =>
			enumToBoolConverter.Convert(value, typeof(bool), TestEnumForEnumToBoolConverter.Five, CultureInfo.InvariantCulture));
	}

	[Theory]
	[InlineData("a string")]
	[InlineData(42)]
	[InlineData(null)]
	[InlineData(false)]
	[InlineData(TestFlaggedEnumForEnumToBoolConverter.Four)]
	public void EnumToBoolConvert_ParameterNotSameEnum_ReturnsFalse(object parameter)
	{
		var enumToBoolConverter = new EnumToBoolConverter();

		var result = (bool)enumToBoolConverter.Convert(TestEnumForEnumToBoolConverter.Five, typeof(bool), parameter, CultureInfo.InvariantCulture);

		Assert.False(result);
	}

	[Theory]
	[InlineData(null, TestEnumForEnumToBoolConverter.Five, TestEnumForEnumToBoolConverter.Five, true)]
	[InlineData(null, TestEnumForEnumToBoolConverter.Five, TestEnumForEnumToBoolConverter.Six, false)]
	[InlineData(new object?[] { TestEnumForEnumToBoolConverter.Five, TestEnumForEnumToBoolConverter.Six }, TestEnumForEnumToBoolConverter.Five, TestEnumForEnumToBoolConverter.Six, true)]
	[InlineData(new object?[] { TestEnumForEnumToBoolConverter.Five, TestEnumForEnumToBoolConverter.Six }, TestEnumForEnumToBoolConverter.Six, null, true)]
	[InlineData(new object?[] { TestEnumForEnumToBoolConverter.Five, TestEnumForEnumToBoolConverter.Six }, TestEnumForEnumToBoolConverter.One, TestEnumForEnumToBoolConverter.Five, false)]
	[InlineData(new object?[] { TestEnumForEnumToBoolConverter.Five, TestEnumForEnumToBoolConverter.Six }, TestEnumForEnumToBoolConverter.Two, null, false)]
	[InlineData(new object?[] { (TestFlaggedEnumForEnumToBoolConverter.One | TestFlaggedEnumForEnumToBoolConverter.Three), TestFlaggedEnumForEnumToBoolConverter.Two }, TestFlaggedEnumForEnumToBoolConverter.One, null, true)]
	[InlineData(new object?[] { (TestFlaggedEnumForEnumToBoolConverter.One | TestFlaggedEnumForEnumToBoolConverter.Three), TestFlaggedEnumForEnumToBoolConverter.Two }, TestFlaggedEnumForEnumToBoolConverter.Two, null, true)]
	[InlineData(new object?[] { (TestFlaggedEnumForEnumToBoolConverter.One | TestFlaggedEnumForEnumToBoolConverter.Three), TestFlaggedEnumForEnumToBoolConverter.Two }, TestFlaggedEnumForEnumToBoolConverter.Three, null, true)]
	[InlineData(new object?[] { TestFlaggedEnumForEnumToBoolConverter.One | TestFlaggedEnumForEnumToBoolConverter.Three, TestFlaggedEnumForEnumToBoolConverter.Two }, TestFlaggedEnumForEnumToBoolConverter.Four, null, false)]
	[InlineData(null, TestFlaggedEnumForEnumToBoolConverter.One, TestFlaggedEnumForEnumToBoolConverter.One | TestFlaggedEnumForEnumToBoolConverter.Three, true)]
	[InlineData(null, TestFlaggedEnumForEnumToBoolConverter.Three, TestFlaggedEnumForEnumToBoolConverter.One | TestFlaggedEnumForEnumToBoolConverter.Three, true)]
	[InlineData(null, TestFlaggedEnumForEnumToBoolConverter.Two, TestFlaggedEnumForEnumToBoolConverter.One | TestFlaggedEnumForEnumToBoolConverter.Three, false)]
	[InlineData(null, TestFlaggedEnumForEnumToBoolConverter.One | TestFlaggedEnumForEnumToBoolConverter.Three, TestFlaggedEnumForEnumToBoolConverter.One | TestFlaggedEnumForEnumToBoolConverter.Three, true)]
	[InlineData(null, TestFlaggedEnumForEnumToBoolConverter.One | TestFlaggedEnumForEnumToBoolConverter.Two | TestFlaggedEnumForEnumToBoolConverter.Three, TestFlaggedEnumForEnumToBoolConverter.One | TestFlaggedEnumForEnumToBoolConverter.Three, false)]
	public void EnumToBoolConvert_Validation(object?[]? trueValues, object? value, object parameter, bool expectedResult)
	{
		var enumToBoolConverter = new EnumToBoolConverter();
		trueValues?.OfType<Enum>().ToList().ForEach(fe => enumToBoolConverter.TrueValues.Add(fe));

		var result = (bool)enumToBoolConverter.Convert(value, typeof(bool), parameter, CultureInfo.InvariantCulture);
		Assert.Equal(expectedResult, result);
	}
}