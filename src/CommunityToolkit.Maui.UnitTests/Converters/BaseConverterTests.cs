using System.Globalization;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

/// <summary>
/// Unit tests that target <see cref="BaseConverter{TFrom,TTo}"/> and their public APIs.
/// </summary>
public abstract class BaseConverterTests
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

	[Theory]
	[InlineData("4.123", typeof(Color))]
	[InlineData("4", typeof(Color))]
	public abstract void ConvertBack_WithMismatchedTargetType(object? inputValue, Type targetType);

	[Theory]
	[InlineData(4.123, typeof(string))]
	[InlineData(true, typeof(string))]
	public abstract void ConvertBack_WithInvalidValueType(object? inputValue, Type targetType);

	[Theory]
	[InlineData("One", typeof(int))]
	public void ConvertBack_ShouldCorrectlyReturnValueWithMatchingTargetType(object? inputValue, Type targetType)
	{
		ICommunityToolkitValueConverter converter = CreateConverter();

		Assert.Equal(0, converter.ConvertBack(inputValue, targetType, null, CultureInfo.CurrentCulture));
	}

	protected static BaseConverter<int, string> CreateConverter() => new MockConverter(["One", "Two", "Three"])
	{
		DefaultConvertReturnValue = "Three",
		DefaultConvertBackReturnValue = 42
	};

	protected BaseConverterTests(bool suppressExceptions)
	{
		new Options().SetShouldSuppressExceptionsInConverters(suppressExceptions);
	}
}

/// <summary>
/// Unit tests that target <see cref="BaseConverter{TFrom,TTo}"/> and their public APIs with <see cref="Options.SetShouldSuppressExceptionsInConverters"/> == false.
/// </summary>
public class BaseConverterTestsWithExceptionsEnabled : BaseConverterTests
{
	public BaseConverterTestsWithExceptionsEnabled() : base(false)
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

	public override void ConvertBack_WithMismatchedTargetType(object? inputValue, Type targetType)
	{
		ICommunityToolkitValueConverter converter = CreateConverter();

		var exception = Assert.Throws<ArgumentException>(() => converter.ConvertBack(inputValue, targetType, null, CultureInfo.CurrentCulture));

		exception.Message.Should().Be($"targetType needs to be assignable from {converter.FromType}. (Parameter 'targetType')");
	}

	public override void ConvertBack_WithInvalidValueType(object? inputValue, Type targetType)
	{
		ICommunityToolkitValueConverter converter = CreateConverter();

		var exception = Assert.Throws<ArgumentException>(() => converter.ConvertBack(inputValue, targetType, null, CultureInfo.CurrentCulture));

		exception.Message.Should().Be($"Value needs to be of type {converter.ToType} (Parameter 'value')");
	}
}

/// <summary>
/// Unit tests that target <see cref="BaseConverter{TFrom,TTo}"/> and their public APIs with <see cref="Options.SetShouldSuppressExceptionsInConverters"/> == true.
/// </summary>
public class BaseConverterTestsWithExceptionsSuppressed : BaseConverterTests
{
	public BaseConverterTestsWithExceptionsSuppressed() : base(true)
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

	public override void ConvertBack_WithMismatchedTargetType(object? inputValue, Type targetType)
	{
		ICommunityToolkitValueConverter converter = CreateConverter();

		converter.ConvertBack(inputValue, targetType, null, CultureInfo.CurrentCulture).Should().Be(converter.DefaultConvertBackReturnValue);
	}

	public override void ConvertBack_WithInvalidValueType(object? inputValue, Type targetType)
	{
		ICommunityToolkitValueConverter converter = CreateConverter();

		converter.ConvertBack(inputValue, targetType, null, CultureInfo.CurrentCulture).Should().Be(converter.DefaultConvertBackReturnValue);
	}
}