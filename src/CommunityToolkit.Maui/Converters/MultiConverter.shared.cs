using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts an incoming value using all the incoming converters in sequence.
/// </summary>
[AcceptEmptyServiceProvider]
public partial class MultiConverter : List<ICommunityToolkitValueConverter>, ICommunityToolkitValueConverter
{
	object? ICommunityToolkitValueConverter.DefaultConvertReturnValue => throw new NotSupportedException($"{nameof(ICommunityToolkitMultiValueConverter)} does not implement {nameof(ICommunityToolkitValueConverter.DefaultConvertReturnValue)}");

	object? ICommunityToolkitValueConverter.DefaultConvertBackReturnValue => throw new NotSupportedException($"{nameof(ICommunityToolkitMultiValueConverter)} does not implement {nameof(ICommunityToolkitValueConverter.DefaultConvertBackReturnValue)}");

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
	Type ICommunityToolkitValueConverter.FromType => throw new NotSupportedException($"{nameof(ICommunityToolkitMultiValueConverter)} does not implement {nameof(ICommunityToolkitValueConverter.FromType)}");

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
	Type ICommunityToolkitValueConverter.ToType => throw new NotSupportedException($"{nameof(ICommunityToolkitMultiValueConverter)} does not implement {nameof(ICommunityToolkitValueConverter.ToType)}");


#pragma warning disable IL2092
	/// <summary>
	/// Uses the incoming converters to convert the value.
	/// </summary>
	/// <param name="value">Value to convert.</param>
	/// <param name="targetType">The type of the binding target property.</param>
	/// <param name="parameter">Parameter to pass into all converters.</param>
	/// <param name="culture">The culture to use in the converter.</param>
	/// <returns>The converted value.</returns>
	public object? Convert(object? value, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type targetType, object? parameter, System.Globalization.CultureInfo? culture)
		=> parameter is IList<MultiConverterParameter> parameters
		? this.Aggregate(value, (current, converter) => converter.Convert(current, converter.ToType,
				 parameters.FirstOrDefault(x => x.ConverterType == converter.GetType())?.Value, culture))
		: this.Aggregate(value, (current, converter) => converter.Convert(current, converter.ToType, parameter, culture));

	/// <summary>
	/// This method is not supported and will throw a <see cref="NotSupportedException"/>.
	/// </summary>
	/// <param name="value">N/A</param>
	/// <param name="targetType">N/A</param>
	/// <param name="parameter">N/A</param>
	/// <param name="culture">N/A</param>
	/// <returns>N/A</returns>
	public object? ConvertBack(object? value, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type targetType, object? parameter, System.Globalization.CultureInfo? culture)
		=> throw new NotSupportedException("Impossible to revert to original value. Consider setting BindingMode to OneWay.");
#pragma warning restore IL2092
}