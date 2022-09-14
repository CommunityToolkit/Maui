using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class EnumToBoolConverterTests : BaseOneWayConverterTest<EnumToBoolConverter>
{
	[Fact]
	public void EnumToBoolConvertBack_ThrowsNotSupportedException()
	{
		var enumToBoolConverter = new EnumToBoolConverter();

		Assert.Throws<NotSupportedException>(() =>
		 ((ICommunityToolkitValueConverter)enumToBoolConverter).ConvertBack(true, typeof(Enum), null, CultureInfo.InvariantCulture));
	}

	[Theory]
	[InlineData("a string")]
	[InlineData(42)]
	[InlineData(false)]
	public void EnumToBoolConvert_ValueNotEnum_ThrowsArgumentException(object value)
	{
		var enumToBoolConverter = new EnumToBoolConverter();

		Assert.Throws<ArgumentException>(() =>
			((ICommunityToolkitValueConverter)enumToBoolConverter).Convert(value, typeof(bool), TestEnumForEnumToBoolConverter.Five, CultureInfo.InvariantCulture));
	}

	[Fact]
	public void EnumToBoolConvert_NullValue_ThrowsArgumentNullException()
	{
		var enumToBoolConverter = new EnumToBoolConverter();

		Assert.Throws<ArgumentNullException>(() =>
			((ICommunityToolkitValueConverter)enumToBoolConverter).Convert(null, typeof(bool), TestEnumForEnumToBoolConverter.Five, CultureInfo.InvariantCulture));
	}

	[Theory]
	[InlineData(TextCaseType.Lower)]
	[InlineData(null)]
	[InlineData(TestFlaggedEnumForEnumToBoolConverter.Four)]
	public void EnumToBoolConvert_ParameterNotSameEnum_ReturnsFalse(Enum? parameter)
	{
		var enumToBoolConverter = new EnumToBoolConverter();

		var convertResult = (bool?)((ICommunityToolkitValueConverter)enumToBoolConverter).Convert(TestEnumForEnumToBoolConverter.Five, typeof(bool), parameter, CultureInfo.InvariantCulture);
		var convertFromResult = enumToBoolConverter.ConvertFrom(TestEnumForEnumToBoolConverter.Five, parameter);

		Assert.False(convertResult);
		Assert.False(convertFromResult);
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
	public void EnumToBoolConvert_Validation(object?[]? trueValues, Enum value, Enum parameter, bool expectedResult)
	{
		var enumToBoolConverter = new EnumToBoolConverter();
		trueValues?.OfType<Enum>().ToList().ForEach(fe => enumToBoolConverter.TrueValues.Add(fe));

		var convertResult = (bool?)((ICommunityToolkitValueConverter)enumToBoolConverter).Convert(value, typeof(bool), parameter, CultureInfo.InvariantCulture);
		var convertFromResult = enumToBoolConverter.ConvertFrom(value, parameter);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Fact]
	public void EnumToBoolConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new EnumToBoolConverter()).Convert(TestEnumForEnumToBoolConverter.Five, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new EnumToBoolConverter()).Convert(null, typeof(bool), null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

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
}