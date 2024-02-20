using Android.App;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific;

static partial class NavigationBar
{
	static bool? isSupported;

	static bool IsSupported => isSupported ??= AndroidSystemExtensions.IsSupported(BuildVersionCodes.M);

	internal static partial void RemapForControls()
	{
		PageHandler.Mapper.Add(ColorProperty.PropertyName, MapNavigationColorProperty);
		PageHandler.Mapper.Add(StyleProperty.PropertyName, MapNavigationStyleProperty);
	}

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

		var activity = (Activity?)handler.PlatformView.Context ?? throw new NullReferenceException("Activity cannot be null.");
		var style = GetStyle(page);

		switch (style)
		{
			case NavigationBarStyle.DarkContent:
				SetSystemNavigationBarAppearance(activity, true);
				break;

			default:
				SetSystemNavigationBarAppearance(activity, false);
				break;
		}

		static void SetSystemNavigationBarAppearance(Activity activity, bool isLightSystemNavigationBars)
		{
			var window = activity.GetCurrentWindow();
			var windowController = WindowCompat.GetInsetsController(window, window.DecorView);
			windowController.AppearanceLightNavigationBars = isLightSystemNavigationBars;
		}
	}

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

		var activity = (Activity?)handler.PlatformView.Context ?? throw new NullReferenceException("Activity cannot be null.");
		var window = activity.GetCurrentWindow();

		var color = GetColor(page).ToPlatform();

		window.SetNavigationBarColor(color);
	}

}