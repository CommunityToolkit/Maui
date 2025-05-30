namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// A XAML markup extension that enables using <see cref="AppThemeColor"/> and <see cref="AppThemeObject"/> from XAML.
/// </summary>
[ContentProperty(nameof(Key)), RequireService([typeof(IServiceProvider), typeof(IProvideValueTarget), typeof(IRootObjectProvider)])]
public sealed class AppThemeResourceExtension : IMarkupExtension<BindingBase>
{
	/// <summary>
	/// Gets or sets the key used to access the <see cref="AppThemeColor"/> or <see cref="AppThemeObject"/> from the <see cref="ResourceDictionary"/>.
	/// </summary>
	public string? Key { get; set; }

	/// <inheritdoc/>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
	/// <exception cref="XamlParseException">Thrown if <see cref="Key"/> is <see langword="null"/> or if a resource with <see cref="Key"/> cannot be found.</exception>
	/// <exception cref="ArgumentException">Thrown if <paramref name="serviceProvider"/> does no implement <see cref="IProvideParentValues"/>.</exception>
	public BindingBase ProvideValue(IServiceProvider serviceProvider)
	{
		ArgumentNullException.ThrowIfNull(serviceProvider);

		if (Key is null)
		{
			throw new XamlParseException($"{nameof(AppThemeResourceExtension)}.{nameof(Key)} cannot be null.", serviceProvider);
		}

		var valueTarget = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
		var targetObject = valueTarget?.TargetObject;
		if (targetObject is null)
		{
			var info = (serviceProvider.GetService(typeof(IXmlLineInfoProvider)) as IXmlLineInfoProvider)?.XmlLineInfo;
			throw new XamlParseException($"Cannot determine target for {nameof(AppThemeResourceExtension)}.", info);
		}

		if (TryFindResourceInVisualElement(targetObject, Key, out var resource))
		{
			if (resource is AppThemeColor color)
			{
				return color.GetBinding();
			}
			if (resource is AppThemeObject theme)
			{
				return theme.GetBinding();
			}

			var info = (serviceProvider.GetService(typeof(IXmlLineInfoProvider)) as IXmlLineInfoProvider)?.XmlLineInfo;
			throw new XamlParseException($"Resource found for key {Key} is not a valid AppTheme resource.", info);
		}

		// Fallback to root object ResourceDictionary (e.g. page-level resources)
		var rootProvider = serviceProvider.GetService(typeof(IRootObjectProvider)) as IRootObjectProvider;
		var root = rootProvider?.RootObject;
		if (root is IResourcesProvider rootResources && rootResources.IsResourcesCreated
			&& rootResources.Resources.TryGetValue(Key, out resource))
		{
			if (resource is AppThemeColor rootColor)
			{
				return rootColor.GetBinding();
			}
			if (resource is AppThemeObject rootTheme)
			{
				return rootTheme.GetBinding();
			}
			var info = (serviceProvider.GetService(typeof(IXmlLineInfoProvider)) as IXmlLineInfoProvider)?.XmlLineInfo;
			throw new XamlParseException($"Resource found for key {Key} is not a valid AppTheme resource.", info);
		}

		if (Application.Current?.Resources.TryGetValueAndSource(Key, out resource, out _) == true)
		{
			if (resource is AppThemeColor color)
			{
				return color.GetBinding();
			}
			if (resource is AppThemeObject theme)
			{
				return theme.GetBinding();
			}
			var info = (serviceProvider.GetService(typeof(IXmlLineInfoProvider)) as IXmlLineInfoProvider)?.XmlLineInfo;
			throw new XamlParseException($"Resource found for key {Key} is not a valid AppTheme resource.", info);
		}

		var xmlInfo = (serviceProvider.GetService(typeof(IXmlLineInfoProvider)) as IXmlLineInfoProvider)?.XmlLineInfo;
		throw new XamlParseException($"Resource not found for key {Key}.", xmlInfo);
	}

	/// <summary>
	/// Attempts to locate a resource by walking up the visual tree from a target object.
	/// </summary>
	static bool TryFindResourceInVisualElement(object element, string key, out object resource)
	{
		resource = default!;

		// If the element has a Resources property via IResourcesProvider
		if (element is IResourcesProvider provider && provider.IsResourcesCreated)
		{
			if (provider.Resources.TryGetValue(key, out resource))
			{
				return true;
			}
		}

		// Walk up the element tree
		if (element is Element elementObj)
		{
			var parent = elementObj.Parent;
			while (parent is not null)
			{
				if (parent is IResourcesProvider parentProvider && parentProvider.IsResourcesCreated
					&& parentProvider.Resources.TryGetValue(key, out resource))
				{
					return true;
				}
				parent = parent.Parent;
			}
		}

		// If it's a ResourceDictionary, check it directly
		if (element is ResourceDictionary dict && dict.TryGetValue(key, out resource))
		{
			return true;
		}

		return false;
	}

	object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}