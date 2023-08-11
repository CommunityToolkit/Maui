namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// A XAML markup extension that enables using <see cref="AppThemeColor"/> and <see cref="AppThemeObject"/> from XAML.
/// </summary>
[ContentProperty(nameof(Key))]
public sealed class AppThemeResourceExtension : IMarkupExtension<BindingBase>
{
	/// <summary>
	/// Gets or sets the key that is used to access the <see cref="AppThemeColor"/> or <see cref="AppThemeObject"/> from the <see cref="ResourceDictionary"/>.
	/// </summary>
	public string? Key { get; set; }

	/// <inheritdoc/>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
	/// <exception cref="XamlParseException">Thrown if <see cref="Key"/> is <see langword="null"/> or if a resource with <see cref="Key"/> cannot be found.</exception>
	/// <exception cref="ArgumentException">Thrown if <paramref name="serviceProvider"/> does no implement <see cref="IProvideParentValues"/>.</exception>
	public BindingBase ProvideValue(IServiceProvider serviceProvider)
	{
		ArgumentNullException.ThrowIfNull(serviceProvider, nameof(serviceProvider));

		if (Key is null)
		{
			throw new XamlParseException($"{nameof(AppThemeResourceExtension)}.{nameof(Key)} Cannot be null. You must set a {nameof(Key)} that specifies the AppTheme resource to use", serviceProvider);
		}

		if (serviceProvider.GetService(typeof(IProvideValueTarget)) is not IProvideParentValues valueProvider)
		{
			throw new ArgumentException(null, nameof(serviceProvider));
		}

		if (!TryGetResource(Key, valueProvider.ParentObjects, out var resource, out var resourceDictionary)
			&& !TryGetApplicationLevelResource(Key, out resource, out resourceDictionary))
		{
			var xmlLineInfo = serviceProvider.GetService(typeof(IXmlLineInfoProvider)) is IXmlLineInfoProvider xmlLineInfoProvider ? xmlLineInfoProvider.XmlLineInfo : null;
			throw new XamlParseException($"Resource not found for key {Key}", xmlLineInfo);
		}
		else if (resource is AppThemeColor color)
		{
			return color.GetBinding();
		}
		else if (resource is AppThemeObject themeResource)
		{
			return themeResource.GetBinding();
		}
		else
		{
			var xmlLineInfo = serviceProvider.GetService(typeof(IXmlLineInfoProvider)) is IXmlLineInfoProvider xmlLineInfoProvider ? xmlLineInfoProvider.XmlLineInfo : null;
			throw new XamlParseException($"Resource found for key {Key} is not of type {nameof(AppThemeColor)} or {nameof(AppThemeObject)}", xmlLineInfo);
		}
	}

	static bool TryGetResource(string key, IEnumerable<object> parentObjects, out object? resource, out ResourceDictionary? resourceDictionary)
	{
		resource = null;
		resourceDictionary = null;

		foreach (var parentObject in parentObjects)
		{
			ResourceDictionary? resDict = parentObject is IResourcesProvider resoiurceProvider && resoiurceProvider.IsResourcesCreated
											? resoiurceProvider.Resources
											: parentObject as ResourceDictionary;
			if (resDict is null)
			{
				continue;
			}

			if (resDict.TryGetValueAndSource(key, out resource, out resourceDictionary))
			{
				return true;
			}
		}

		return false;
	}

	static bool TryGetApplicationLevelResource(string key, out object? resource, out ResourceDictionary? resourceDictionary)
	{
		resource = null;
		resourceDictionary = null;

		return Application.Current is not null
			&& ((IResourcesProvider)Application.Current).IsResourcesCreated
			&& Application.Current.Resources.TryGetValueAndSource(key, out resource, out resourceDictionary);
	}

	object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}