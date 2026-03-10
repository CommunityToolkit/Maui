using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts an <see cref="Enum"/> value to its display string using <see cref="DisplayAttribute"/> or <see cref="DescriptionAttribute"/>.
/// </summary>
[AcceptEmptyServiceProvider]
public partial class EnumDescriptionConverter : BaseConverterOneWay<Enum, string>
{
	/// <inheritdoc/>
	public override string DefaultConvertReturnValue { get; set; } = string.Empty;

	/// <summary>
	/// Converts an <see cref="Enum"/> value to its display string.
	/// </summary>
	/// <param name="value">The enum value to convert.</param>
	/// <param name="culture">The culture to use for the conversion (not used).</param>
	/// <returns>
	/// The value of the <see cref="DisplayAttribute.Name"/> if defined;
	/// otherwise the value of the <see cref="DescriptionAttribute.Description"/> if defined;
	/// otherwise the enum name as a string.
	/// </returns>
	public override string ConvertFrom(Enum value, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(value);
		var fieldInfo = value.GetType().GetField(value.ToString());

		// Check for DisplayAttribute first (common in data annotations)
		var displayAttr = fieldInfo?.GetCustomAttribute<DisplayAttribute>();
		if (displayAttr is not null)
		{
			string? displayName = null;
			try
			{
				displayName = displayAttr.GetName();
			}
			catch
			{
				// If GetName throws, fallback to Name
				displayName = null;
			}
			if (!string.IsNullOrWhiteSpace(displayName))
			{
				return displayName;
			}
			if (!string.IsNullOrWhiteSpace(displayAttr.Name))
			{
				return displayAttr.Name;
			}
		}

		// Fallback to DescriptionAttribute
		var descriptionAttr = fieldInfo?.GetCustomAttribute<DescriptionAttribute>();
		if (descriptionAttr is not null && !string.IsNullOrWhiteSpace(descriptionAttr.Description))
		{
			return descriptionAttr.Description;
		}

		return value.ToString(); // Fallback to enum name if no attribute found
	}

	// ConvertBackTo is sealed in BaseConverterOneWay and cannot be overridden.
}