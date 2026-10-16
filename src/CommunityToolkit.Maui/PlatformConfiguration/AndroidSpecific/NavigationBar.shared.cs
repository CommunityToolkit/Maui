using System.Runtime.Versioning;
using CommunityToolkit.Maui.Core;
using MAndroid = Microsoft.Maui.Controls.PlatformConfiguration.Android;
using MauiElement = Microsoft.Maui.Controls.Page;

namespace CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific;

/// <summary>
/// Provides platform-specific configuration properties for the Android navigation bar.
/// </summary>
/// <remarks>
/// When a page clears its <see cref="ColorProperty"/> or <see cref="StyleProperty"/> value (<see cref="BindableObject.ClearValue(BindableProperty)"/>),
/// every window that still shows that value gets back the navigation bar color or appearance it had before <see cref="NavigationBar"/> first changed it.
/// </remarks>
[SupportedOSPlatform("Android23.0")]
[AttachedBindableProperty<NavigationBarStyle>("Style", DefaultValue = NavigationBarStyle.Default, CoerceValueMethodName = nameof(CoerceStyle))]
public static partial class NavigationBar
{
	// Color is declared here instead of with [AttachedBindableProperty<Color>]: the attribute only takes a constant DefaultValue, and a
	// DefaultValueCreator makes .NET MAUI's BindableObject.IsSet report the color as set as soon as it was read, so the Android page
	// handler would apply a navigation bar color that the app never set.

	/// <summary>
	/// Attached BindableProperty for the Color property.
	/// </summary>
	public static readonly BindableProperty ColorProperty = BindableProperty.CreateAttached("Color", typeof(Color), typeof(NavigationBar), Color.FromUint(0u), coerceValue: CoerceColor);

	/// <summary>
	/// Gets Color for the <paramref name = "bindable"/> child element.
	/// </summary>
	public static Color GetColor(BindableObject bindable) => (Color)bindable.GetValue(ColorProperty);

	/// <summary>
	/// Sets Color for the <paramref name = "bindable"/> child element.
	/// </summary>
	public static void SetColor(BindableObject bindable, Color value) => bindable.SetValue(ColorProperty, value);

	/// <summary>
	/// Sets the color of the navigation bar.
	/// </summary>
	/// <param name="config">The platform element configuration.</param>
	/// <param name="value">The color to set.</param>
	/// <returns>The platform element configuration.</returns>
	public static IPlatformElementConfiguration<MAndroid, MauiElement> SetColor(this IPlatformElementConfiguration<MAndroid, MauiElement> config, Color value)
	{
		SetColor(config.Element, value);
		return config;
	}

	/// <summary>
	/// Gets the color of the navigation bar.
	/// </summary>
	/// <param name="config">The platform element configuration.</param>
	/// <returns>The color of the navigation bar.</returns>
	public static Color GetColor(this IPlatformElementConfiguration<MAndroid, MauiElement> config)
	{
		return GetColor(config.Element);
	}

	/// <summary>
	/// Sets the style of the navigation bar.
	/// </summary>
	/// <param name="config">The platform element configuration.</param>
	/// <param name="value">The style to set.</param>
	/// <returns>The platform element configuration.</returns>
	public static IPlatformElementConfiguration<MAndroid, MauiElement> SetStyle(this IPlatformElementConfiguration<MAndroid, MauiElement> config, NavigationBarStyle value)
	{
		SetStyle(config.Element, value);
		return config;
	}

	/// <summary>
	/// Gets the style of the navigation bar.
	/// </summary>
	/// <param name="config">The platform element configuration.</param>
	/// <returns>The style of the navigation bar.</returns>
	public static NavigationBarStyle GetStyle(this IPlatformElementConfiguration<MAndroid, MauiElement> config)
	{
		return GetStyle(config.Element);
	}

	internal static partial void RemapForControls();

	// .NET MAUI coerces the value on every SetValue and ClearValue, also when clearing it changes no value (a value equal to the default) and so
	// raises no PropertyChanged for the page handler's mappings. Once the value of a page is cleared, the Android navigation bar gives the windows
	// back what they had before NavigationBar changed them; a page whose value was never set changed nothing.
	static object CoerceColor(BindableObject bindable, object value)
	{
		RestoreNavigationBarColorIfCleared(bindable);
		return value;
	}

	static object CoerceStyle(BindableObject bindable, object value)
	{
		RestoreNavigationBarAppearanceIfCleared(bindable);
		return value;
	}

	static partial void RestoreNavigationBarColorIfCleared(BindableObject bindable);

	static partial void RestoreNavigationBarAppearanceIfCleared(BindableObject bindable);

#if !ANDROID
	internal static partial void RemapForControls()
	{

	}
#endif
}