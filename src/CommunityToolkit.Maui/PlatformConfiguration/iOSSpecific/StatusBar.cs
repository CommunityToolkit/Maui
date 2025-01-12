using System.Runtime.Versioning;
using MiOS = Microsoft.Maui.Controls.PlatformConfiguration.iOS;
using MauiElement = Microsoft.Maui.Controls.Page;

namespace CommunityToolkit.Maui.PlatformConfiguration.iOSSpecific;

/// <summary>
/// Provides platform-specific configuration properties for the iOS StatusBar bar.
/// </summary>
[SupportedOSPlatform("ios")]
public static class StatusBar
{
	/// <summary>
	/// Identifies the UseSafeArea bindable property.
	/// </summary>
	public static readonly BindableProperty UseSafeAreaProperty = BindableProperty.CreateAttached("UseSafeArea", typeof(bool), typeof(StatusBar), true);

	/// <summary>
	/// Gets the UseSafeArea of the navigation bar.
	/// </summary>
	/// <param name="element">The bindable object.</param>
	/// <returns>The UseSafeArea value.</returns>
	public static bool GetUseSafeArea(BindableObject element)
	{
		return (bool)element.GetValue(UseSafeAreaProperty);
	}

	/// <summary>
	/// Sets the UseSafeArea of the navigation bar.
	/// </summary>
	/// <param name="element">The bindable object.</param>
	/// <param name="value">The UseSafeArea to set.</param>
	public static void SetUseSafeArea(BindableObject element, bool value)
	{
		element.SetValue(UseSafeAreaProperty, value);
	}

	/// <summary>
	/// Sets the UseSafeArea of the navigation bar.
	/// </summary>
	/// <param name="config">The platform element configuration.</param>
	/// <param name="value">The UseSafeArea to set.</param>
	/// <returns>The platform element configuration.</returns>
	public static IPlatformElementConfiguration<MiOS, MauiElement> SetUseSafeArea(this IPlatformElementConfiguration<MiOS, MauiElement> config, bool value)
	{
		SetUseSafeArea(config.Element, value);
		return config;
	}

	/// <summary>
	/// Gets the UseSafeArea of the navigation bar.
	/// </summary>
	/// <param name="config">The platform element configuration.</param>
	/// <returns>The UseSafeArea value</returns>
	public static bool GetUseSafeArea(this IPlatformElementConfiguration<MiOS, MauiElement> config)
	{
		return GetUseSafeArea(config.Element);
	}
}