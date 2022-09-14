using System.Globalization;
using AutoFixture.Xunit2;
using CommunityToolkit.Maui.Converters;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class BoolToObjectConverterTests : BaseConverterTest<BoolToObjectConverter>
{
	public const string TrueTestObject = nameof(TrueTestObject);
	public const string FalseTestObject = nameof(FalseTestObject);

	[Theory]
	[InlineData(true, TrueTestObject, FalseTestObject, TrueTestObject)]
	[InlineData(false, TrueTestObject, FalseTestObject, FalseTestObject)]
	public void BoolToObjectConvert(bool value, object trueObject, object falseObject, object expectedResult)
	{
		var boolObjectConverter = new BoolToObjectConverter
		{
			TrueObject = trueObject,
			FalseObject = falseObject
		};

		var convertResult = ((ICommunityToolkitValueConverter)boolObjectConverter).Convert(value, typeof(object), null, CultureInfo.CurrentCulture);
		var convertFromResult = boolObjectConverter.ConvertFrom(value);

		Assert.NotNull(convertResult);
		Assert.NotNull(convertFromResult);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[InlineData(true, TrueTestObject, FalseTestObject, TrueTestObject)]
	[InlineData(false, TrueTestObject, FalseTestObject, FalseTestObject)]
	public void BoolToObjectTConvert(bool value, string trueObject, string falseObject, object expectedResult)
	{
		var boolObjectConverter = new BoolToObjectConverter<string>
		{
			TrueObject = trueObject,
			FalseObject = falseObject
		};

		var convertResult = ((ICommunityToolkitValueConverter)boolObjectConverter).Convert(value, typeof(string), null, CultureInfo.CurrentCulture);
		var convertFromResult = boolObjectConverter.ConvertFrom(value);

		Assert.NotNull(convertResult);
		Assert.NotNull(convertFromResult);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[InlineData(TrueTestObject, TrueTestObject, FalseTestObject, true)]
	[InlineData(FalseTestObject, TrueTestObject, FalseTestObject, false)]
	public void BoolToObjectConvertBack(object value, object trueObject, object falseObject, bool expectedResult)
	{
		var boolObjectConverter = new BoolToObjectConverter
		{
			TrueObject = trueObject,
			FalseObject = falseObject
		};

		var convertBackResult = (bool?)((ICommunityToolkitValueConverter)boolObjectConverter).ConvertBack(value, typeof(bool), null, CultureInfo.CurrentCulture);
		var convertBackToResult = boolObjectConverter.ConvertBackTo(value);

		Assert.Equal(expectedResult, convertBackResult);
		Assert.Equal(expectedResult, convertBackToResult);
	}

	[Theory]
	[InlineData(TrueTestObject, TrueTestObject, FalseTestObject, true)]
	[InlineData(FalseTestObject, TrueTestObject, FalseTestObject, false)]
	public void BoolToObjectTConvertBack(string value, string trueObject, string falseObject, bool expectedResult)
	{
		var boolObjectConverter = new BoolToObjectConverter<string>
		{
			TrueObject = trueObject,
			FalseObject = falseObject
		};

		var convertBackResult = (bool?)((ICommunityToolkitValueConverter)boolObjectConverter).ConvertBack(value, typeof(bool), null, CultureInfo.CurrentCulture);
		var convertBackToResult = boolObjectConverter.ConvertBackTo(value);

		Assert.Equal(expectedResult, convertBackResult);
		Assert.Equal(expectedResult, convertBackToResult);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(5.5)]
	[InlineData('c')]
	[InlineData("abc")]
	public void BoolToObjectInvalidValuesThrowArgumentException(object value)
	{
		var boolObjectConverter = new BoolToObjectConverter();
		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)boolObjectConverter).Convert(value, typeof(object), null, CultureInfo.CurrentCulture));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(5.5)]
	[InlineData('c')]
	[InlineData("abc")]
	public void BoolToObjectTInvalidValuesThrowArgumentException(object value)
	{
		var boolObjectConverter = new BoolToObjectConverter<DateTime>();
		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)boolObjectConverter).ConvertBack(value, typeof(object), null, CultureInfo.CurrentCulture));
	}

	[Fact]
	public void BoolToObjectConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new BoolToObjectConverter()).Convert(null, typeof(object), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new BoolToObjectConverter()).Convert(true, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new BoolToObjectConverter()).ConvertBack(true, null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact]
	public void BoolToObjectTConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)new BoolToObjectConverter<string>()).Convert(null, typeof(object), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new BoolToObjectConverter<string>()).Convert(true, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new BoolToObjectConverter<string>()).ConvertBack(true, null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[InlineData(0)]
	[InlineData(5.5)]
	[InlineData('c')]
	[InlineData("abc")]
	public void BoolToObjectInvalidValuesShouldNotThrowArgumentException(object value)
	{
		var options = new Options();
		options.SetShouldSuppressExceptionsInConverters(true);

		var boolObjectConverter = new BoolToObjectConverter();
		var action = () => ((ICommunityToolkitValueConverter)boolObjectConverter).Convert(value, typeof(object), null, CultureInfo.CurrentCulture);
		action.Should().NotThrow<ArgumentException>();

		options.SetShouldSuppressExceptionsInConverters(false);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(5.5)]
	[InlineData('c')]
	[InlineData("abc")]
	public void BoolToObjectTInvalidValuesShouldNotThrowArgumentException(object value)
	{
		var options = new Options();
		options.SetShouldSuppressExceptionsInConverters(true);

		var boolObjectConverter = new BoolToObjectConverter<DateTime>();
		var action = () => ((ICommunityToolkitValueConverter)boolObjectConverter).ConvertBack(value, typeof(object), null, CultureInfo.CurrentCulture);
		action.Should().NotThrow<ArgumentException>();

		options.SetShouldSuppressExceptionsInConverters(false);
	}

	[Fact]
	public void BoolToObjectConverterShouldNotThrowNullInputTest()
	{
		var options = new Options();
		options.SetShouldSuppressExceptionsInConverters(true);

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var action = () => ((ICommunityToolkitValueConverter)new BoolToObjectConverter()).Convert(null, typeof(object), null, null);
		action.Should().NotThrow<ArgumentNullException>();
		action = () => ((ICommunityToolkitValueConverter)new BoolToObjectConverter()).Convert(true, null, null, null);
		action.Should().NotThrow<ArgumentNullException>();
		action = () => ((ICommunityToolkitValueConverter)new BoolToObjectConverter()).ConvertBack(true, null, null, null);
		action.Should().NotThrow<ArgumentNullException>();
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		options.SetShouldSuppressExceptionsInConverters(false);
	}

	[Fact]
	public void BoolToObjectTConverterShouldNotThrowNullInputTest()
	{
		var options = new Options();
		options.SetShouldSuppressExceptionsInConverters(true);

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var action = () => ((ICommunityToolkitValueConverter)new BoolToObjectConverter<string>()).Convert(null, typeof(object), null, null);
		action.Should().NotThrow<ArgumentException>();
		action = () => ((ICommunityToolkitValueConverter)new BoolToObjectConverter<string>()).Convert(true, null, null, null);
		action.Should().NotThrow<ArgumentNullException>();
		action = () => ((ICommunityToolkitValueConverter)new BoolToObjectConverter<string>()).ConvertBack(true, null, null, null);
		action.Should().NotThrow<ArgumentNullException>();
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		options.SetShouldSuppressExceptionsInConverters(false);
	}

	[Theory]
	[AutoData]
	public void BoolToObjectTConverterShouldReturnDefaultValue(string defaultReturnValue)
	{
		var options = new Options();
		options.SetShouldSuppressExceptionsInConverters(true);

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var converter = new BoolToObjectConverter<string>
		{
			DefaultConvertReturnValue = defaultReturnValue
		};
		var convertResult = ((ICommunityToolkitValueConverter)converter).Convert(null, typeof(object), null, null);
		convertResult.Should().Be(defaultReturnValue);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		options.SetShouldSuppressExceptionsInConverters(false);
	}

	[Theory]
	[AutoData]
	public void BoolToObjectTConverterShouldReturnBackDefaultValue(bool backReturnData)
	{
		var options = new Options();
		options.SetShouldSuppressExceptionsInConverters(true);

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var converter = new BoolToObjectConverter<string>
		{
			DefaultConvertBackReturnValue = backReturnData
		};
		var convertBackResult = ((ICommunityToolkitValueConverter)converter).ConvertBack(null, typeof(object), null, null);
		convertBackResult.Should().Be(backReturnData);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		options.SetShouldSuppressExceptionsInConverters(false);
	}

	protected override object? GetInvalidConvertBackValue() => null;
	protected override BoolToObjectConverter InitializeConverterForInvalidConverterTests() => new() { TrueObject = false };
}