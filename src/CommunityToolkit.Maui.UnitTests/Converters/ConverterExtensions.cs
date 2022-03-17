using System.Globalization;
using CommunityToolkit.Maui.Converters;
using FluentAssertions;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public static class ConverterExtensions
{
	/// <summary>
	/// Asserts that the <see cref="ICommunityToolkitValueConverter.Convert(object?, Type?, object?, CultureInfo?)"/> method
	/// will throw the supplied <typeparamref name="TException"/> when supplied with the specified parameters.
	/// </summary>
	/// <typeparam name="TException">The type of <see cref="Exception"/> expected to be thrown.</typeparam>
	/// <param name="converter">The <see cref="ICommunityToolkitValueConverter"/> to call Convert on.</param>
	/// <param name="value">The incoming value to be converted.</param>
	/// <param name="targetType">The expected target type.</param>
	/// <param name="parameter">An optional parameter to assist with the conversion.</param>
	/// <param name="cultureInfo">The current culture to assist with the conversion.</param>
	public static void ConvertShouldThrow<TException>(
		this ICommunityToolkitValueConverter converter,
		object? value,
		Type? targetType,
		object? parameter,
		CultureInfo? cultureInfo) where TException : Exception
	{
		ActionShouldThrow<TException>(new Action(() => converter.Convert(value, targetType, parameter, cultureInfo)));
	}

	/// <summary>
	/// Asserts that the <see cref="ICommunityToolkitValueConverter.ConvertBack(object?, Type?, object?, CultureInfo?)"/> method
	/// will throw the supplied <typeparamref name="TException"/> when supplied with the specified parameters.
	/// </summary>
	/// <typeparam name="TException">The type of <see cref="Exception"/> expected to be thrown.</typeparam>
	/// <param name="converter">The <see cref="ICommunityToolkitValueConverter"/> to call ConvertBack on.</param>
	/// <param name="value">The incoming value to be converted.</param>
	/// <param name="targetType">The expected target type.</param>
	/// <param name="parameter">An optional parameter to assist with the conversion.</param>
	/// <param name="cultureInfo">The current culture to assist with the conversion.</param>
	public static void ConvertBackShouldThrow<TException>(
		this ICommunityToolkitValueConverter converter,
		object? value,
		Type? targetType,
		object? parameter,
		CultureInfo? cultureInfo) where TException : Exception
	{
		ActionShouldThrow<TException>(new Action(() => converter.ConvertBack(value, targetType, parameter, cultureInfo)));
	}

	static void ActionShouldThrow<TException>(Action throwingAction) where TException : Exception
	{
		throwingAction.Should().Throw<TException>();
	}
}
