using System.Collections;
using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Concatenates the members of a collection, using the specified separator between each member.
/// </summary>
[AcceptEmptyServiceProvider]
public partial class ListToStringConverter : BaseConverterOneWay<IEnumerable, string, string?>
{
	/// <inheritdoc/>
	public override string DefaultConvertReturnValue { get; set; } = string.Empty;

	/// <summary>
	/// The value that separates each item in the collection
	/// This value is superseded by the ConverterParameter, if provided
	/// If ConverterParameter is null, this Separator property will be used for the <see cref="ListToStringConverter"/>
	/// </summary>
	public string Separator
	{
		get;
		set => field = value ?? throw new ArgumentNullException(nameof(value));
	} = string.Empty;

	/// <summary>
	/// Concatenates the items of a collection, using the specified <see cref="Separator"/> between each item. Items that implement <see cref="IFormattable"/> are formatted with the culture; other items use ToString().
	/// </summary>
	/// <param name="value">The collection to convert.</param>
	/// <param name="parameter">The separator that should be between each collection item. This overrides the value in <see cref="Separator"/>.</param>
	/// <param name="culture">The culture used to format items that implement <see cref="IFormattable"/>. <see langword="null"/> uses <see cref="CultureInfo.CurrentCulture"/>.</param>
	/// <returns>Concatenated members string separated by <see cref="Separator"/> or, if set, <paramref name="parameter"/>.</returns>
	public override string ConvertFrom(IEnumerable value, string? parameter = null, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(value);

		var collection = value
			.OfType<object>()
			.Select(x => x is IFormattable formattable ? formattable.ToString(null, culture) : x.ToString())
			.Where(x => !string.IsNullOrWhiteSpace(x));

		return string.Join(parameter ?? Separator, collection); // if the ConverterParameter is not assigned (aka parameter is null), we will default to the Separator property. 
	}
}