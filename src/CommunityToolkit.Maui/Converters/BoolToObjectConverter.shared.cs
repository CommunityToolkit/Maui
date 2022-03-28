using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts boolean to object and vice versa.
/// </summary>
public class BoolToObjectConverter : BoolToObjectConverter<object>
{
}

/// <summary>
/// Converts <see cref="bool"/> to object and vice versa.
/// </summary>
public class BoolToObjectConverter<TObject> : BaseConverter<bool, TObject?>
{
	/// <summary>
	/// The object that corresponds to True value.
	/// </summary>
	public TObject? TrueObject { get; set; }

	/// <summary>
	/// The object that corresponds to False value.
	/// </summary>
	public TObject? FalseObject { get; set; }

	/// <summary>
	/// Converts <see cref="bool"/> to object.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
	/// <param name="culture">The culture to use in the converter.  This is not implemented.</param>
	/// <returns>The object assigned to <see cref="TrueObject"/> if value equals True, otherwise the value assigned to <see cref="FalseObject"/>.</returns>
	public override TObject? ConvertFrom(bool value, Type targetType, object? parameter, CultureInfo? culture) =>
		value ? TrueObject : FalseObject;

	/// <summary>
	/// Converts back object to <see cref="bool"/>.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
	/// <param name="culture">The culture to use in the converter.  This is not implemented.</param>
	/// <returns>True if value equals <see cref="TrueObject"/>, otherwise False.</returns>
	public override bool ConvertBackTo(TObject? value, Type targetType, object? parameter, CultureInfo? culture)
	{
		if (value is TObject result)
		{
			return result.Equals(TrueObject);
		}

		if (default(TObject) is null && value is null && TrueObject is null)
		{
			return true;
		}

		throw new ArgumentException($"Value is not a valid {typeof(TObject).Name}", nameof(value));
	}
}