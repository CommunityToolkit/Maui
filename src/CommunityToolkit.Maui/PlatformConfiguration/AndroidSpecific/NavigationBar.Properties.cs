using MAndroid = Microsoft.Maui.Controls.PlatformConfiguration.Android;
using MauiElement = Microsoft.Maui.Controls.Page;

namespace CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific;

/// <summary>
/// Provides platform-specific configuration properties for the Android navigation bar.
/// </summary>
public static partial class NavigationBar
{
	/// <summary>
	/// Identifies the Color bindable property.
	/// </summary>
	public static readonly BindableProperty ColorProperty = BindableProperty.CreateAttached("Color", typeof(Color), typeof(NavigationBar), Colors.Transparent);

	/// <summary>
	/// Gets the color of the navigation bar.
	/// </summary>
	/// <param name="element">The bindable object.</param>
	/// <returns>The color of the navigation bar.</returns>
	public static Color GetColor(BindableObject element)
	{
		return (Color)element.GetValue(ColorProperty);
	}

	/// <summary>
	/// Sets the color of the navigation bar.
	/// </summary>
	/// <param name="element">The bindable object.</param>
	/// <param name="value">The color to set.</param>
	public static void SetColor(BindableObject element, Color value)
	{
		element.SetValue(ColorProperty, value);
	}

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
	/// Identifies the Style bindable property.
	/// </summary>
	public static readonly BindableProperty StyleProperty = BindableProperty.CreateAttached("Style", typeof(NavigationBarStyle), typeof(NavigationBar), NavigationBarStyle.Default);

	/// <summary>
	/// Sets the style of the navigation bar.
	/// </summary>
	/// <param name="element">The bindable object.</param>
	/// <param name="value">The style to set.</param>
	public static void SetStyle(BindableObject element, NavigationBarStyle value)
	{
		element.SetValue(StyleProperty, value);
	}

	/// <summary>
	/// Gets the style of the navigation bar.
	/// </summary>
	/// <param name="element">The bindable object.</param>
	/// <returns>The style of the navigation bar.</returns>
	public static NavigationBarStyle GetStyle(BindableObject element)
	{
		return (NavigationBarStyle)element.GetValue(StyleProperty);
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

#if !ANDROID
	internal static partial void RemapForControls()
	{

	}
#endif
}