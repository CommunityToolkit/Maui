using System.Diagnostics.CodeAnalysis;

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
			var info = (serviceProvider.GetService(typeof(IXmlLineInfoProvider)) as IXmlLineInfoProvider)?.XmlLineInfo;
			throw new XamlParseException($"{nameof(AppThemeResourceExtension)}.{nameof(Key)} cannot be null.", info);
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
			switch (resource)
			{
				case AppThemeColor color:
					return GetBinding(color, GetTargetProperty(valueTarget));
				case AppThemeObject theme:
					return GetBinding(theme, GetTargetProperty(valueTarget));
				default:
					var info = (serviceProvider.GetService(typeof(IXmlLineInfoProvider)) as IXmlLineInfoProvider)?.XmlLineInfo;
					throw new XamlParseException($"Resource found for key {Key} is not a valid AppTheme resource.", info);
			}
		}

		// Fallback to root object ResourceDictionary (e.g. page-level or application-level resources)
		var rootProvider = serviceProvider.GetService(typeof(IRootObjectProvider)) as IRootObjectProvider;
		var root = rootProvider?.RootObject;

		switch (root)
		{
			case VisualElement rootElement when rootElement.Resources.TryGetValue(Key, out resource):
				// page level?
				switch (resource)
				{
					case AppThemeColor rootColor:
						return GetBinding(rootColor, GetTargetProperty(valueTarget));
					case AppThemeObject rootTheme:
						return GetBinding(rootTheme, GetTargetProperty(valueTarget));
				}
				break;
			case Application rootApplication when rootApplication.Resources.TryGetValue(Key, out resource):
				// application level?
				switch (resource)
				{
					case AppThemeColor rootColor:
						return GetBinding(rootColor, GetTargetProperty(valueTarget));
					case AppThemeObject rootTheme:
						return GetBinding(rootTheme, GetTargetProperty(valueTarget));
				}
				break;
			case ResourceDictionary rootDictionary1 when rootDictionary1.TryGetValue(Key, out resource):
				// application level?
				switch (resource)
				{
					case AppThemeColor rootColor:
						return GetBinding(rootColor, GetTargetProperty(valueTarget));
					case AppThemeObject rootTheme:
						return GetBinding(rootTheme, GetTargetProperty(valueTarget));
				}
				break;
		}

		if (Application.Current?.Resources.TryGetValue(Key, out resource) is true)
		{
			switch (resource)
			{
				case AppThemeColor color:
					return GetBinding(color, GetTargetProperty(valueTarget));
				case AppThemeObject theme:
					return GetBinding(theme, GetTargetProperty(valueTarget));
				default:
					var info = (serviceProvider.GetService(typeof(IXmlLineInfoProvider)) as IXmlLineInfoProvider)?.XmlLineInfo;
					throw new XamlParseException($"Resource found for key {Key} is not a valid AppTheme resource.", info);
			}
		}

		var xmlInfo = (serviceProvider.GetService(typeof(IXmlLineInfoProvider)) as IXmlLineInfoProvider)?.XmlLineInfo;
		throw new XamlParseException($"Resource not found for key {Key}.", xmlInfo);
	}

	/// <summary>
	/// Attempts to locate a resource by walking up the visual tree from a target object.
	/// </summary>
	static bool TryFindResourceInVisualElement(object element, string key, [NotNullWhen(true)] out object? resource)
	{
		resource = null;

		// If the element has a public Resources property, check it directly.
		if (element is VisualElement { Resources: { } resources }
			&& resources.TryGetValue(key, out resource))
		{
			return true;
		}

		switch (element)
		{
			// Walk up the element tree to try to find the resource
			case Element elementObj:
				{
					var parent = elementObj.Parent;
					while (parent is not null)
					{
						if (parent is VisualElement { Resources: { } parentResources }
							&& parentResources.TryGetValue(key, out resource))
						{
							return true;
						}

						parent = parent.Parent;
					}

					break;
				}
			// If it's a ResourceDictionary, check it directly
			case ResourceDictionary dict when dict.TryGetValue(key, out resource):
				return true;
		}

		return false;
	}

	static BindableProperty? GetTargetProperty(IProvideValueTarget? valueTarget)
	{
		return valueTarget?.TargetObject switch
		{
			Setter { Property: { } setterProperty } => setterProperty,
			_ => valueTarget?.TargetProperty as BindableProperty
		};
	}

	static BindingBase GetBinding<T>(AppThemeObject<T> theme, BindableProperty? targetProperty) =>
		targetProperty is null ? theme.GetBinding() : theme.GetBinding(targetProperty);

	object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}