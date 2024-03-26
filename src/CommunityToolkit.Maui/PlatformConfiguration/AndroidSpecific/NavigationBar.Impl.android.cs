using Android.App;
using Android.OS;
using AndroidX.Core.View;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific;

static partial class NavigationBar
{
	static readonly Lazy<bool> isSupportedHandler = new(() =>
	{
		if (OperatingSystem.IsAndroidVersionAtLeast(23))
		{
			return true;
		}

		System.Diagnostics.Trace.WriteLine($"{nameof(NavigationBar)} {nameof(Style)} + {nameof(Color)} functionality is not supported on this version of the Android operating system. Minimum supported Android API is {BuildVersionCodes.M}");

		return false;
	});

	static bool IsSupported => isSupportedHandler.Value;

	/// <summary>
	/// Map Navigation Style Property
	/// </summary>
	/// <param name="handler"></param>
	/// <param name="view"></param>
	/// <exception cref="NullReferenceException"></exception>
	/// <exception cref="NotSupportedException"></exception>
	public static void MapNavigationStyleProperty(IPageHandler handler, IContentView view)
	{
		if (view is not Page page)
		{
			return;
		}

		if (!page.IsSet(StyleProperty) || !IsSupported)
		{
			return;
		}

		var activity = (Activity?)handler.PlatformView.Context ?? throw new InvalidOperationException("Activity cannot be null.");
		var style = GetStyle(page);

		switch (style)
		{
			case NavigationBarStyle.DarkContent:
				SetSystemNavigationBarAppearance(activity, true);
				break;

			case NavigationBarStyle.Default:
			case NavigationBarStyle.LightContent:
				SetSystemNavigationBarAppearance(activity, false);
				break;

			default:
				throw new NotSupportedException($"{nameof(NavigationBar)} {style} is not yet supported");
		}

		static void SetSystemNavigationBarAppearance(Activity activity, bool isLightSystemNavigationBars)
		{
			var window = activity.GetCurrentWindow();
			var windowController = WindowCompat.GetInsetsController(window, window.DecorView);
			windowController.AppearanceLightNavigationBars = isLightSystemNavigationBars;
		}
	}

	/// <summary>
	/// Map Navigation Color Property
	/// </summary>
	/// <param name="handler"></param>
	/// <param name="view"></param>
	/// <exception cref="NullReferenceException"></exception>
	/// <exception cref="NotSupportedException"></exception>
	public static void MapNavigationColorProperty(IPageHandler handler, IContentView view)
	{
		if (view is not Page page)
		{
			return;
		}

		if (!page.IsSet(ColorProperty) || !IsSupported)
		{
			return;
		}

		var activity = (Activity?)handler.PlatformView.Context ?? throw new InvalidOperationException("Activity cannot be null.");
		var window = activity.GetCurrentWindow();

		var color = GetColor(page).ToPlatform();

		window.SetNavigationBarColor(color);
	}

	internal static partial void RemapForControls()
	{
		PageHandler.Mapper.Add(ColorProperty.PropertyName, MapNavigationColorProperty);
		PageHandler.Mapper.Add(StyleProperty.PropertyName, MapNavigationStyleProperty);
	}
}