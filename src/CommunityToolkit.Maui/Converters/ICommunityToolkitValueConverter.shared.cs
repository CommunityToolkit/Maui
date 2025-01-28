using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <inheritdoc />
public interface ICommunityToolkitValueConverter : IValueConverter
{
	/// <summary>
	/// Default value to return when <see cref="Convert(object?, Type, object?, CultureInfo?)"/> throws an <see cref="Exception"/>.
	/// This value is used when <see cref="Maui.Options.ShouldSuppressExceptionsInConverters"/> is set to <see langword="true"/>.
	/// </summary>
	object? DefaultConvertReturnValue { get; }

	/// <summary>
	/// Default value to return when <see cref="ConvertBack(object?, Type, object?, CultureInfo?)"/> throws an <see cref="Exception"/>.
	/// This value is used when <see cref="Maui.Options.ShouldSuppressExceptionsInConverters"/> is set to <see langword="true"/>.
	/// </summary>
	object? DefaultConvertBackReturnValue { get; }

	/// <summary>
	/// Gets the <see cref="Type" /> this converter expects to <see cref="Convert" /> from or <see cref="ConvertBack" /> to.
	/// </summary>
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
	Type FromType { get; }

	/// <summary>
	/// Gets the <see cref="Type" /> this converter expects to <see cref="Convert" /> to or <see cref="ConvertBack" /> from.
	/// </summary>
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
	Type ToType { get; }

	/// <summary>
	/// Converts the incoming values to a different object
	/// </summary>
	/// <param name="value">The object to convert</param>
	/// <param name="targetType">Target Type</param>
	/// <param name="parameter">Optional Parameters</param>
	/// <param name="culture">Culture Info</param>
	/// <returns>The converted object</returns>
	new object? Convert(object? value, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type targetType, object? parameter, CultureInfo? culture);

	/// <summary>
	/// Converts the object back to the outgoing values
	/// </summary>
	/// <param name="value">The object to convert back</param>
	/// <param name="targetType">Target Type</param>
	/// <param name="parameter">Optional Parameters</param>
	/// <param name="culture">Culture Info</param>
	/// <returns>The object converted back</returns>
	new object? ConvertBack(object? value, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type targetType, object? parameter, CultureInfo? culture);


	/// <inheritdoc />
	[UnconditionalSuppressMessage("TrimAnalysis", "IL2092", Justification = "The ToType and FromType properties are attributed with the appropriate DynamicallyAccessedMembers attributes.")]
	object? IValueConverter.Convert(object? value, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type targetType, object? parameter, CultureInfo culture) =>
		Convert(value, targetType, parameter, culture);

	/// <inheritdoc />
	[UnconditionalSuppressMessage("TrimAnalysis", "IL2092", Justification = "The ToType and FromType properties are attributed with the appropriate DynamicallyAccessedMembers attributes.")]
	object? IValueConverter.ConvertBack(object? value, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type targetType, object? parameter, CultureInfo culture) =>
		ConvertBack(value, targetType, parameter, culture);
}