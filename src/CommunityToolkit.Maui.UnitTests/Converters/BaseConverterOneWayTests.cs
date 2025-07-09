using System.Globalization;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public abstract class BaseConverterOneWayTests
{
	[Theory]
	[InlineData(4.123, typeof(double))]
	[InlineData(4, typeof(int))]
	public abstract void Convert_WithMismatchedTargetType(object? inputValue, Type targetType);

	[Theory]
	[InlineData(4.123, typeof(string))]
	[InlineData(true, typeof(string))]
	public abstract void Convert_WithInvalidValueType(object? inputValue, Type targetType);

	[Theory]
	[InlineData(1, typeof(string))]
	public void Convert_ShouldCorrectlyReturnValueWithMatchingTargetType(object? inputValue, Type targetType)
	{
		ICommunityToolkitValueConverter converter = CreateConverter();

		Assert.Equal("Two", converter.Convert(inputValue, targetType, null, CultureInfo.CurrentCulture));
	}

	[Fact]
	public void Setting_DefaultConvertBackReturnValue_WillThrowNotSupportedException()
	{
		ICommunityToolkitValueConverter converter = CreateConverter();

		Assert.Throws<NotSupportedException>(() => new MockOneWayConverter(["One", "Two", "Three"])
		{
			DefaultConvertReturnValue = "Three",
			DefaultConvertBackReturnValue = 1
		});
	}

	protected static BaseConverter<int, string> CreateConverter() => new MockOneWayConverter(["One", "Two", "Three"])
	{
		DefaultConvertReturnValue = "Three"
	};

	protected BaseConverterOneWayTests(bool suppressExceptions)
	{
		new Options().SetShouldSuppressExceptionsInConverters(suppressExceptions);
	}
}

/// <summary>
/// Unit tests that target <see cref="BaseConverter{TFrom,TTo}"/> and their public APIs with <see cref="Options.SetShouldSuppressExceptionsInConverters"/> == false.
/// </summary>
public class BaseConverterOneWayTestsWithExceptionsEnabled : BaseConverterOneWayTests
{
	public BaseConverterOneWayTestsWithExceptionsEnabled() : base(false)
	{
	}

	public override void Convert_WithMismatchedTargetType(object? inputValue, Type targetType)
	{
		ICommunityToolkitValueConverter converter = CreateConverter();

		var exception = Assert.Throws<ArgumentException>(() => converter.Convert(inputValue, targetType, null, CultureInfo.CurrentCulture));

		exception.Message.Should().Be($"targetType needs to be assignable from {converter.ToType}. (Parameter 'targetType')");
	}

	public override void Convert_WithInvalidValueType(object? inputValue, Type targetType)
	{
		ICommunityToolkitValueConverter converter = CreateConverter();

		var exception = Assert.Throws<ArgumentException>(() => converter.Convert(inputValue, targetType, null, CultureInfo.CurrentCulture));

		exception.Message.Should().Be($"Value needs to be of type {converter.FromType} (Parameter 'value')");
	}
}

/// <summary>
/// Unit tests that target <see cref="BaseConverter{TFrom,TTo}"/> and their public APIs with <see cref="Options.SetShouldSuppressExceptionsInConverters"/> == true.
/// </summary>
public class BaseConverterOneWayTestsWithExceptionsSuppressed : BaseConverterOneWayTests
{
	public BaseConverterOneWayTestsWithExceptionsSuppressed() : base(true)
	{
	}

	public override void Convert_WithMismatchedTargetType(object? inputValue, Type targetType)
	{
		ICommunityToolkitValueConverter converter = CreateConverter();

		converter.Convert(inputValue, targetType, null, CultureInfo.CurrentCulture).Should().Be(converter.DefaultConvertReturnValue);
	}

	public override void Convert_WithInvalidValueType(object? inputValue, Type targetType)
	{
		ICommunityToolkitValueConverter converter = CreateConverter();

		converter.Convert(inputValue, targetType, null, CultureInfo.CurrentCulture).Should().Be(converter.DefaultConvertReturnValue);
	}
}