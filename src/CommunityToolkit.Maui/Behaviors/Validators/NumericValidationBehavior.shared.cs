using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// The <see cref="NumericValidationBehavior"/> is a behavior that allows the user to determine if text input is a valid numeric value. For example, an <see cref="Entry"/> control can be styled differently depending on whether a valid or an invalid numeric input is provided. Additional properties handling validation are inherited from <see cref="ValidationBehavior"/>.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
[RequiresUnreferencedCode($"{nameof(NumericValidationBehavior)} is not trim safe because it uses bindings with string paths.")]
public partial class NumericValidationBehavior : ValidationBehavior<string>
{
	/// <summary>
	/// The minimum numeric value that will be allowed. This is a bindable property.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnValidationPropertyChanged))]
	public partial double MinimumValue { get; set; } = NumericValidationBehaviorDefaults.MinimumValue;

	/// <summary>
	/// The maximum numeric value that will be allowed. This is a bindable property.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnValidationPropertyChanged))]
	public partial double MaximumValue { get; set; } = NumericValidationBehaviorDefaults.MaximumValue;

	/// <summary>
	/// The minimum number of decimal places that will be allowed. This is a bindable property.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnValidationPropertyChanged))]
	public partial int MinimumDecimalPlaces { get; set; } = NumericValidationBehaviorDefaults.MinimumDecimalPlaces;

	/// <summary>
	/// The maximum number of decimal places that will be allowed. This is a bindable property.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnValidationPropertyChanged))]
	public partial int MaximumDecimalPlaces { get; set; } = NumericValidationBehaviorDefaults.MaximumDecimalPlaces;

	/// <inheritdoc/>
	protected override string? Decorate(string? value)
		=> base.Decorate(value)?.Trim();

	/// <inheritdoc/>
	protected override ValueTask<bool> ValidateAsync(string? value, CancellationToken token)
	{
		if (!(double.TryParse(value, out var numeric)
			&& numeric >= MinimumValue
			&& numeric <= MaximumValue))
		{
			return new ValueTask<bool>(false);
		}

		var decimalDelimiterIndex = value.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, StringComparison.Ordinal);
		var hasDecimalDelimiter = decimalDelimiterIndex >= 0;

		// If MaximumDecimalPlaces equals zero, ".5" or "14." should be considered as invalid inputs.
		if (hasDecimalDelimiter && MaximumDecimalPlaces == 0)
		{
			return new ValueTask<bool>(false);
		}

		var decimalPlaces = hasDecimalDelimiter
			? value.Substring(decimalDelimiterIndex + 1).Length
			: 0;

		return new ValueTask<bool>(decimalPlaces >= MinimumDecimalPlaces && decimalPlaces <= MaximumDecimalPlaces);
	}
}