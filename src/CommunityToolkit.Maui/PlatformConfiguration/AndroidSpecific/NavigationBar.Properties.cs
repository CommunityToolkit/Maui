#if ANDROID
using Android.App;
using Android.Views;
#endif
using MAndroid = Microsoft.Maui.Controls.PlatformConfiguration.Android;
using MauiElement = Microsoft.Maui.Controls.Page;

namespace CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific;
/// <summary>
/// 
/// </summary>
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public static partial class NavigationBar
{
	public static readonly BindableProperty ColorProperty = BindableProperty.CreateAttached("Color", typeof(Color), typeof(NavigationBar), Colors.Transparent);

	public static Color GetColor(BindableObject element)
	{
		return (Color)element.GetValue(ColorProperty);
	}

	public static void SetColor(BindableObject element, Color value)
	{
		element.SetValue(ColorProperty, value);
	}

	public static IPlatformElementConfiguration<MAndroid, MauiElement> SetColor(this IPlatformElementConfiguration<MAndroid, MauiElement> config, Color value)
	{
		SetColor(config.Element, value);
		return config;
	}

	public static Color GetColor(this IPlatformElementConfiguration<MAndroid, MauiElement> config)
	{
		return GetColor(config.Element);
	}

	public static readonly BindableProperty StyleProperty = BindableProperty.CreateAttached("Style", typeof(NavigationBarStyle), typeof(NavigationBar), NavigationBarStyle.Default);

	public static void SetStyle(BindableObject element, NavigationBarStyle value)
	{
		element.SetValue(StyleProperty, value);
	}

	public static NavigationBarStyle GetStyle(BindableObject element)
	{
		return (NavigationBarStyle)element.GetValue(StyleProperty);
	}

	public static IPlatformElementConfiguration<MAndroid, MauiElement> SetStyle(this IPlatformElementConfiguration<MAndroid, MauiElement> config, NavigationBarStyle value)
	{
		SetStyle(config.Element, value);
		return config;
	}

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


#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member