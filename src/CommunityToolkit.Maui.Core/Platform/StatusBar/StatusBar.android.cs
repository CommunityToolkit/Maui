using System.Runtime.Versioning;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Maui.Platform;
using Activity = Android.App.Activity;
using PlatformColor = Android.Graphics.Color;

namespace CommunityToolkit.Maui.Core.Platform;

[SupportedOSPlatform("Android23.0")] // StatusBar is only supported on Android 23.0+
static partial class StatusBar
{
	static readonly Lazy<bool> isSupportedHolder = new(() =>
	{
		if (OperatingSystem.IsAndroidVersionAtLeast((int)BuildVersionCodes.M))
		{
			return true;
		}

		System.Diagnostics.Trace.WriteLine($"{nameof(StatusBar)} Color + Style functionality is not supported on this version of the Android operating system. Minimum supported Android API is {BuildVersionCodes.M}");

		return false;
	});

	static Activity Activity => Microsoft.Maui.ApplicationModel.Platform.CurrentActivity ?? throw new InvalidOperationException("Android Activity can't be null.");

	static bool IsSupported => isSupportedHolder.Value;

	static void PlatformSetColor(Color color)
	{
		if (!IsSupported)
		{
			return;
		}

		if (Activity.GetCurrentWindow() is not Window { DecorView.RootView: not null } window)
		{
			return;
		}

		var platformColor = color.ToPlatform();
		var decorGroup = (ViewGroup)window.DecorView.RootView;

		void ApplyWindowFlags()
		{
			bool isTransparent = platformColor == PlatformColor.Transparent;

			if (isTransparent)
			{
				window.ClearFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
				window.SetFlags(WindowManagerFlags.LayoutNoLimits, WindowManagerFlags.LayoutNoLimits);
			}
			else
			{
				window.ClearFlags(WindowManagerFlags.LayoutNoLimits);
				window.SetFlags(
					WindowManagerFlags.DrawsSystemBarBackgrounds,
					WindowManagerFlags.DrawsSystemBarBackgrounds);
			}

			WindowCompat.SetDecorFitsSystemWindows(window, !isTransparent);
		}

		if (!OperatingSystem.IsAndroidVersionAtLeast(35))
		{
			window.SetStatusBarColor(platformColor);
			ApplyWindowFlags();
			return;
		}

		const string tag = "StatusBarOverlay";
		var statusBarOverlay = decorGroup.FindViewWithTag(tag);

		if (statusBarOverlay is null)
		{
			void CreateOverlay(int height)
			{
				statusBarOverlay = new View(Activity)
				{
					LayoutParameters = new FrameLayout.LayoutParams(
						ViewGroup.LayoutParams.MatchParent,
						height + 3)
					{
						Gravity = GravityFlags.Top
					},
					Tag = tag
				};

				decorGroup.AddView(statusBarOverlay);
				statusBarOverlay.SetZ(0);
				statusBarOverlay.SetBackgroundColor(platformColor);
			}

			if (OperatingSystem.IsAndroidVersionAtLeast(36))
			{
				window.DecorView.Post(() =>
				{
					var insets = window.DecorView.RootWindowInsets;
					var height = insets?.GetInsets(WindowInsets.Type.StatusBars()).Top ?? 0;

					CreateOverlay(height);
					ApplyWindowFlags();
				});
			}
			else
			{
				var resId = Activity.Resources?.GetIdentifier("status_bar_height", "dimen", "android") ?? 0;
				var height = resId > 0
					? Activity.Resources?.GetDimensionPixelSize(resId) ?? 0
					: 0;

				CreateOverlay(height);
				ApplyWindowFlags();
			}
		}
		else
		{
			statusBarOverlay.SetBackgroundColor(platformColor);
			ApplyWindowFlags();
		}
	}

	static void PlatformSetStyle(StatusBarStyle style)
	{
		if (!IsSupported)
		{
			return;
		}

		switch (style)
		{
			case StatusBarStyle.DarkContent:
				SetStatusBarAppearance(true);
				break;

			case StatusBarStyle.Default:
			case StatusBarStyle.LightContent:
				SetStatusBarAppearance(false);
				break;

			default:
				throw new NotSupportedException($"{nameof(StatusBarStyle)} {style} is not yet supported on Android");
		}
	}

	static void SetStatusBarAppearance(bool isLightStatusBars)
	{
		if (Activity.GetCurrentWindow() is Window window
			&& WindowCompat.GetInsetsController(window, window.DecorView) is WindowInsetsControllerCompat windowController)
		{
			windowController.AppearanceLightStatusBars = isLightStatusBars;
		}
	}
}