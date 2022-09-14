using System.Globalization;
using System.Runtime.InteropServices;
using AutoFixture.Xunit2;
using CommunityToolkit.Maui.Converters;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests;

public abstract class BaseConverterTest<TConverter> : BaseTest where TConverter : ICommunityToolkitValueConverter, new()
{
	[SkippableFact]
	public void InvalidConvertBackValue_ShouldThrowException()
	{
		var options = new Options();
		options.SetShouldSuppressExceptionsInConverters(false);

		var converter = InitializeConverterForInvalidConverterTests();
		Skip.If(IsOneWayConverter(), $"{typeof(TConverter).Name} is a One Way Converter");

		Assert.ThrowsAny<Exception>(() => converter.ConvertBack(GetInvalidConvertBackValue(), converter.FromType, null, CultureInfo.CurrentCulture));
	}

	[SkippableFact]
	public void InvalidConvertBackValue_ShouldSuppressExceptionsInConverters_ShouldReturnDefaultConvertValue()
	{
		var options = new Options();
		options.SetShouldSuppressExceptionsInConverters(true);

		var converter = InitializeConverterForInvalidConverterTests();
		Skip.If(IsOneWayConverter(), $"{typeof(TConverter).Name} is a One Way Converter");

		var result = converter.ConvertBack(GetInvalidConvertBackValue(), converter.FromType, null, CultureInfo.CurrentCulture);

		Assert.Equal(converter.DefaultConvertBackReturnValue, result);
	}

	[Fact]
	public void InvalidConvertValue_ShouldThrowException()
	{
		var options = new Options();
		options.SetShouldSuppressExceptionsInConverters(false);

		var converter = InitializeConverterForInvalidConverterTests();

		Assert.ThrowsAny<Exception>(() => converter.Convert(GetInvalidConvertFromValue(), converter.ToType, null, CultureInfo.CurrentCulture));
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

	static bool IsOneWayConverter()
	{
		try
		{
			var defaultConvertBackValue = new TConverter().DefaultConvertBackReturnValue;
			return false;
		}
		catch (NotSupportedException)
		{
			return true;
		}
	}

	static object? GetInvalidValue(Type type)
	{
		if (type != typeof(string))
		{
			return string.Empty;
		}
		else if (type != typeof(bool))
		{
			return true;
		}

		throw new NotImplementedException($"Invalid value not valid for {typeof(TConverter).Name}. If {nameof(InvalidConvertValue_ShouldThrowException)} is failing, please override {nameof(GetInvalidConvertFromValue)} and provide an invalid value. Otherwise, override {nameof(GetInvalidConvertBackValue)} and provide an invalid value");
	}
}