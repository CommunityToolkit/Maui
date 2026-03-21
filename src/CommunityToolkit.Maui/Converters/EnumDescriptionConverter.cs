using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts an enum to its description using either:
/// - [Display(Name = "text")]
/// - [Description("text")]
/// </summary>
[AcceptEmptyServiceProvider]
public partial class EnumDescriptionConverter : BaseConverterOneWay<Enum, string>
{
  /// <inheritdoc />
	public override string DefaultConvertReturnValue { get; set; } = string.Empty;

 /// <inheritdoc />
	public override string ConvertFrom(Enum value, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(value);

		// Zero reflection - pure dictionary lookup
       return EnumDescriptionRegistry.GetDescription(value, culture) ?? value.ToString();
	}

	// ConvertBackTo is sealed in BaseConverterOneWay and cannot be overridden.
}



/// <summary>
/// Stores pre-computed enum descriptions from source generation
/// </summary>
public static class EnumDescriptionRegistry
{
	// Dictionary<EnumType, Dictionary<EnumValueName, Description>>
   static readonly ConcurrentDictionary<System.Type, Dictionary<string, string>> descriptionCache = new();
	static readonly ConcurrentDictionary<System.Type, Dictionary<string, Func<CultureInfo?, string>>> descriptionResolvers = new();

	// Backward compatibility for previously generated code that assigned directly into the dictionary.
	internal static ConcurrentDictionary<System.Type, Dictionary<string, string>> Descriptions => descriptionCache;

	/// <summary>
	/// Registers descriptions for an enum type.
	/// </summary>
	/// <param name="enumType">Enum type to register the descriptions for.</param>
   /// <param name="descriptions">Dictionary mapping enum member names to their description.</param>
    public static void Register(System.Type enumType, IReadOnlyDictionary<string, string> descriptions)
	{
		ArgumentNullException.ThrowIfNull(enumType);
		ArgumentNullException.ThrowIfNull(descriptions);

      descriptionCache[enumType] = new Dictionary<string, string>(descriptions);
	}

	/// <summary>
	/// Registers culture-aware description resolvers for an enum type.
	/// </summary>
	/// <param name="enumType">Enum type to register the description resolvers for.</param>
	/// <param name="resolvers">Dictionary mapping enum member names to a culture-aware resolver.</param>
   public static void Register(System.Type enumType, IReadOnlyDictionary<string, Func<CultureInfo?, string>> resolvers)
	{
		ArgumentNullException.ThrowIfNull(enumType);
		ArgumentNullException.ThrowIfNull(resolvers);

		descriptionResolvers[enumType] = new Dictionary<string, Func<CultureInfo?, string>>(resolvers);
	}

	/// <summary>
	/// Gets the description for an enum value
	/// </summary>
    public static string? GetDescription(Enum value)
		=> GetDescription(value, culture: null);

	/// <summary>
	/// Gets the description for an enum value.
	/// </summary>
	/// <param name="value">Enum value.</param>
	/// <param name="culture">Culture to use when resolving localized descriptions.</param>
	/// <returns>Description if found; otherwise <see langword="null"/>.</returns>
	public static string? GetDescription(Enum value, CultureInfo? culture)
	{
		var type = value.GetType();
		var valueName = value.ToString();

		if (descriptionResolvers.TryGetValue(type, out var resolverDict)
			&& resolverDict.TryGetValue(valueName, out var resolver))
		{
			return resolver(culture);
		}

        if (descriptionCache.TryGetValue(type, out var enumDict) &&
			enumDict.TryGetValue(valueName, out var description))
		{
			return description;
		}

		return null;
	}
}