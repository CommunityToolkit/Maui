using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public abstract class BaseOneWayConverterTest<TConverter> : ConverterTest<TConverter> where TConverter : ICommunityToolkitValueConverter, new()
{
	[Fact]
	public void ConvertBack_ShouldThrowNotSupportedException()
	{
		var options = new Options();
		options.SetShouldSuppressExceptionsInConverters(true);

		var converter = InitializeConverterForInvalidConverterTests();

		Assert.ThrowsAny<NotSupportedException>(() => converter.ConvertBack(GetInvalidConvertBackValue(), converter.FromType, null, CultureInfo.CurrentCulture));
	}
}

public abstract class BaseConverterTest<TConverter> : ConverterTest<TConverter> where TConverter : ICommunityToolkitValueConverter, new()
{
	[Fact]
	public void InvalidConvertBackValue_ShouldThrowException()
	{
		var options = new Options();
		options.SetShouldSuppressExceptionsInConverters(false);

		var converter = InitializeConverterForInvalidConverterTests();

		Assert.ThrowsAny<ArgumentException>(() => converter.ConvertBack(GetInvalidConvertBackValue(), converter.FromType, null, CultureInfo.CurrentCulture));
	}

	[Fact]
	public void InvalidConvertBackValue_ShouldSuppressExceptionsInConverters_ShouldReturnDefaultConvertValue()
	{
		var options = new Options();
		options.SetShouldSuppressExceptionsInConverters(true);

		var converter = InitializeConverterForInvalidConverterTests();

		var result = converter.ConvertBack(GetInvalidConvertBackValue(), converter.FromType, null, CultureInfo.CurrentCulture);

		Assert.Equal(converter.DefaultConvertBackReturnValue, result);
	}
}

public abstract class ConverterTest<TConverter> : BaseHandlerTest where TConverter : ICommunityToolkitValueConverter, new()
{
	[Fact]
	public void InvalidConvertValue_ShouldThrowException()
	{
		var options = new Options();
		options.SetShouldSuppressExceptionsInConverters(false);

		var converter = InitializeConverterForInvalidConverterTests();

		Assert.ThrowsAny<ArgumentException>(() => converter.Convert(GetInvalidConvertFromValue(), converter.ToType, null, CultureInfo.CurrentCulture));
	}

	[Fact]
	public void InvalidConverterValue_ShouldSuppressExceptionsInConverters_ShouldReturnDefaultConvertValue()
	{
		var options = new Options();
		options.SetShouldSuppressExceptionsInConverters(true);

		var converter = InitializeConverterForInvalidConverterTests();

		var result = converter.Convert(GetInvalidConvertFromValue(), converter.ToType, null, CultureInfo.CurrentCulture);

		Assert.Equal(converter.DefaultConvertReturnValue, result);
	}

	protected virtual object? GetInvalidConvertBackValue() => GetInvalidValue(InitializeConverterForInvalidConverterTests().ToType);
	protected virtual object? GetInvalidConvertFromValue() => GetInvalidValue(InitializeConverterForInvalidConverterTests().FromType);
	protected virtual TConverter InitializeConverterForInvalidConverterTests() => new();

	static object GetInvalidValue(Type type)
	{
		if (type != typeof(string))
		{
			return string.Empty;
		}

		if (type != typeof(bool))
		{
			return true;
		}

		throw new NotImplementedException($"Invalid value not valid for {typeof(TConverter).Name}. If {nameof(InvalidConvertValue_ShouldThrowException)} is failing, please override {nameof(GetInvalidConvertFromValue)} and provide an invalid value. Otherwise, override {nameof(GetInvalidConvertBackValue)} and provide an invalid value");
	}
}