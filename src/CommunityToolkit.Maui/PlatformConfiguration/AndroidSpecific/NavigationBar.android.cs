using System.Runtime.Versioning;
using Android.App;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.Core.View;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using AView = Android.Views.View;
using AWindow = Android.Views.Window;

namespace CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific;

static partial class NavigationBar
{
	const string navigationBarOverlayTag = "NavigationBarOverlay";

	// Any elevation draws the overlay above the children of the decor view that have none (all of them, unless an app adds its own)
	const float navigationBarOverlayElevation = 1;

	// WindowInsetsController.APPEARANCE_FORCE_LIGHT_NAVIGATION_BARS, a hidden flag: from API 35 on, when Android enforces edge-to-edge, the decor view
	// sets it on a window that has a light background color and no navigation bar color of its own, and the system then shows light navigation bars
	// (dark icons) whatever APPEARANCE_LIGHT_NAVIGATION_BARS says. Window.SetNavigationBarColor clears it, and the decor view does not set it again
	// once the window has a navigation bar color of its own.
	const int forceLightNavigationBarsAppearance = 1 << 9;

	static readonly List<WindowChange<int>> colorChanges = [];
	static readonly List<WindowChange<NavigationBarAppearance>> styleChanges = [];

	static readonly Lazy<bool> isSupportedHandler = new(() =>
	{
		if (OperatingSystem.IsAndroidVersionAtLeast(23))
		{
			return true;
		}

		System.Diagnostics.Trace.WriteLine($"{nameof(NavigationBar)} {nameof(Style)} + {nameof(Color)} functionality is not supported on this version of the Android operating system. Minimum supported Android API is 23.0");

		return false;
	});

	static bool IsSupported => isSupportedHandler.Value;

	/// <summary>
	/// Map Navigation Style Property
	/// </summary>
	/// <remarks>
	/// <see cref="NavigationBarStyle.DarkContent"/> asks for light navigation bars (dark icons); <see cref="NavigationBarStyle.Default"/> and
	/// <see cref="NavigationBarStyle.LightContent"/> ask for dark navigation bars (light icons). From Android 15 (API 35) on, when Android enforces
	/// edge-to-edge (Android 16 does for every app that targets it), it forces light navigation bars on a window that has a light background and
	/// no navigation bar color of its own, whatever the app asks for. The style then gives the window a transparent navigation bar color, which
	/// stops that: with three-button navigation, Android then draws a scrim behind the navigation bar that matches the style. A page that clears
	/// its style gives the window back the light navigation bars that Android forced, and its navigation bar color. With gesture navigation,
	/// Android may tint the gesture handle to contrast with what is behind it, whatever the style.
	/// </remarks>
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

		var activity = handler.PlatformView.Context?.GetActivity() ?? throw new InvalidOperationException("Activity cannot be null.");
		var style = GetStyle(page);

		switch (style)
		{
			case NavigationBarStyle.DarkContent:
				SetSystemNavigationBarAppearance(activity, page, true);
				break;

			case NavigationBarStyle.Default:
			case NavigationBarStyle.LightContent:
				SetSystemNavigationBarAppearance(activity, page, false);
				break;

			default:
				throw new NotSupportedException($"{nameof(NavigationBar)} {style} is not yet supported");
		}

		static void SetSystemNavigationBarAppearance(Activity activity, Page page, bool isLightSystemNavigationBars)
		{
			var window = activity.GetCurrentWindow();
			if (WindowCompat.GetInsetsController(window, window.DecorView) is WindowInsetsControllerCompat insetsController)
			{
				// When the decor view forces light navigation bars, it gives the window the color of its background as navigation bar color, which
				// Window.NavigationBarColor does not report for an edge-to-edge window
				var isForcedLight = IsForcedLightNavigationBars(window);
				var navigationBarColor = isForcedLight ? (window.DecorView.Background as Android.Graphics.Drawables.ColorDrawable)?.Color.ToArgb() : null;
				RecordChange(styleChanges, window, page, new NavigationBarAppearance(insetsController.AppearanceLightNavigationBars, isForcedLight, navigationBarColor));

				if (isForcedLight && OperatingSystem.IsAndroidVersionAtLeast(35))
				{
					// A navigation bar color of its own stops the system from forcing light navigation bars on the window, now and later. Behind a
					// transparent one, the scrim that the system draws for three-button navigation follows the appearance set below.
#pragma warning disable CA1422 // Window.SetNavigationBarColor is obsolete from API 35, but it still gives the window a navigation bar color of its own
					window.SetNavigationBarColor(Android.Graphics.Color.Transparent);
#pragma warning restore CA1422

					// Window.SetNavigationBarColor does nothing when the window already has that navigation bar color of its own: a window whose
					// background is not a color gets the forced light navigation bars back without a color when a page clears its style
					window.InsetsController?.SetSystemBarsAppearance(0, forceLightNavigationBarsAppearance);
				}

				insetsController.AppearanceLightNavigationBars = isLightSystemNavigationBars;
			}
		}
	}

	/// <summary>
	/// Map Navigation Color Property
	/// </summary>
	/// <remarks>
	/// Before Android 15 (API 35) the color is the navigation bar color of the window. From Android 15 on, Android draws the window behind
	/// the navigation bar and ignores that color, so the color is a view on top of the window that covers the window's navigation bar: at
	/// the bottom, or with three-button navigation in landscape at the right or left edge. It follows the navigation bar whenever the window
	/// dispatches its insets again (the window rotates, the navigation bar is hidden or shown), and it is drawn above the navigation bar
	/// background that Android adds to the window itself.
	/// </remarks>
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

		var activity = handler.PlatformView.Context?.GetActivity() ?? throw new InvalidOperationException("Activity cannot be null.");
		var window = activity.GetCurrentWindow();

		var color = GetColor(page).ToPlatform();

		if (OperatingSystem.IsAndroidVersionAtLeast(35))
		{
			// No window color to remember: from API 35 on, the overlay is the navigation bar color, and removing it gives the window back its own
			RecordChange(colorChanges, window, page, 0);

			WindowCompat.SetDecorFitsSystemWindows(window, false);

			var decorGroup = (ViewGroup)window.DecorView;
			var navigationBarOverlay = decorGroup.FindChildWithTag(navigationBarOverlayTag) ?? AddNavigationBarOverlay(activity, decorGroup);

			navigationBarOverlay.SetBackgroundColor(color);
		}
		else if (OperatingSystem.IsAndroidVersionAtLeast(23))
		{
			RecordChange(colorChanges, window, page, window.NavigationBarColor);
			window.SetNavigationBarColor(color);
		}
		else
		{
			System.Diagnostics.Trace.WriteLine($"{nameof(NavigationBar)} {nameof(Style)} + {nameof(Color)} functionality is not supported on this version of the Android operating system. Minimum supported Android API is 23");
		}
	}

	[SupportedOSPlatform("android35.0")]
	static AView AddNavigationBarOverlay(Activity activity, ViewGroup decorGroup)
	{
		var navigationBarOverlay = new AView(activity)
		{
			Tag = navigationBarOverlayTag,

			// Without an outline, the elevation below casts no shadow
			OutlineProvider = null
		};

		// The decor view adds its own navigation bar background (with three-button navigation, a scrim of the window's background color) the first
		// time the window dispatches its insets: after the overlay when a page applies its color before that, as the first page of an app or of a
		// recreated activity does. The decor view draws its children in child order unless they have an elevation, so the overlay has one.
		navigationBarOverlay.SetZ(navigationBarOverlayElevation);

		navigationBarOverlay.SetOnApplyWindowInsetsListener(new NavigationBarOverlayInsetsListener());
		UpdateNavigationBarOverlayLayout(navigationBarOverlay, decorGroup.RootWindowInsets);
		decorGroup.AddView(navigationBarOverlay);

		return navigationBarOverlay;
	}

	// Laid out like the decor view's own navigation bar background: at the bottom, or, with three-button navigation in landscape, at the right or the left edge
	[SupportedOSPlatform("android35.0")]
	internal static void UpdateNavigationBarOverlayLayout(AView navigationBarOverlay, WindowInsets? windowInsets)
	{
		var navigationBars = windowInsets?.GetInsets(WindowInsets.Type.NavigationBars());

		var (width, height, gravity) = navigationBars switch
		{
			{ Bottom: 0, Right: > 0 } => (navigationBars.Right, ViewGroup.LayoutParams.MatchParent, GravityFlags.Right),
			{ Bottom: 0, Left: > 0 } => (navigationBars.Left, ViewGroup.LayoutParams.MatchParent, GravityFlags.Left),
			_ => (ViewGroup.LayoutParams.MatchParent, navigationBars?.Bottom ?? 0, GravityFlags.Bottom)
		};

		if (navigationBarOverlay.LayoutParameters is FrameLayout.LayoutParams layoutParameters
			&& layoutParameters.Width == width
			&& layoutParameters.Height == height
			&& layoutParameters.Gravity == gravity)
		{
			return;
		}

		navigationBarOverlay.LayoutParameters = new FrameLayout.LayoutParams(width, height)
		{
			Gravity = gravity
		};
	}

	internal static partial void RemapForControls()
	{
		PageHandler.Mapper.Add(ColorProperty.PropertyName, MapNavigationColorProperty);
		PageHandler.Mapper.Add(StyleProperty.PropertyName, MapNavigationStyleProperty);
	}

	static partial void RestoreNavigationBarColorIfCleared(BindableObject bindable)
	{
		if (bindable is not Page page || page.IsSet(ColorProperty))
		{
			return;
		}

		foreach (var (window, originalColor) in TakeChanges(colorChanges, page))
		{
			if (OperatingSystem.IsAndroidVersionAtLeast(35))
			{
				// The window keeps drawing behind the system bars: Android has no getter for its earlier setting, and the window of a
				// .NET MAUI activity draws behind them anyway
				var decorGroup = (ViewGroup)window.DecorView;
				if (decorGroup.FindChildWithTag(navigationBarOverlayTag) is AView navigationBarOverlay)
				{
					decorGroup.RemoveView(navigationBarOverlay);
				}
			}
			else
			{
				window.SetNavigationBarColor(new Android.Graphics.Color(originalColor));
			}
		}
	}

	static partial void RestoreNavigationBarAppearanceIfCleared(BindableObject bindable)
	{
		if (bindable is not Page page || page.IsSet(StyleProperty))
		{
			return;
		}

		foreach (var (window, original) in TakeChanges(styleChanges, page))
		{
			if (original.IsForcedLight && OperatingSystem.IsAndroidVersionAtLeast(35))
			{
				// The navigation bar color that the decor view had given the window goes first: Window.SetNavigationBarColor stops the forced light
				// navigation bars, so they are forced again after it
				if (original.NavigationBarColor is int navigationBarColor)
				{
#pragma warning disable CA1422 // Window.SetNavigationBarColor is obsolete from API 35, but it still gives the window a navigation bar color of its own
					window.SetNavigationBarColor(new Android.Graphics.Color(navigationBarColor));
#pragma warning restore CA1422
				}

				window.InsetsController?.SetSystemBarsAppearance(forceLightNavigationBarsAppearance, forceLightNavigationBarsAppearance);
			}

			if (WindowCompat.GetInsetsController(window, window.DecorView) is WindowInsetsControllerCompat insetsController)
			{
				insetsController.AppearanceLightNavigationBars = original.IsLight;
			}
		}
	}

	static bool IsForcedLightNavigationBars(AWindow window) =>
		OperatingSystem.IsAndroidVersionAtLeast(35)
		&& window.InsetsController is IWindowInsetsController insetsController
		&& (insetsController.SystemBarsAppearance & forceLightNavigationBarsAppearance) != 0;

	static void RecordChange<T>(List<WindowChange<T>> changes, AWindow window, Page page, T currentValue)
	{
		lock (changes)
		{
			WindowChange<T>? windowChange = null;

			for (var i = changes.Count - 1; i >= 0; i--)
			{
				if (changes[i].Window is not AWindow changedWindow)
				{
					changes[i].Dispose();
					changes.RemoveAt(i);
				}
				else if (JNIEnv.IsSameObject(changedWindow.Handle, window.Handle))
				{
					windowChange = changes[i];
				}
			}

			if (windowChange is null)
			{
				changes.Add(new(window, currentValue, page));
			}
			else
			{
				windowChange.SetOwner(page);
			}
		}
	}

	static List<(AWindow Window, T Original)> TakeChanges<T>(List<WindowChange<T>> changes, Page page)
	{
		var takenChanges = new List<(AWindow Window, T Original)>();

		lock (changes)
		{
			for (var i = changes.Count - 1; i >= 0; i--)
			{
				var change = changes[i];
				if (change.Window is AWindow changedWindow)
				{
					if (!change.IsOwnedBy(page))
					{
						continue;
					}

					takenChanges.Add((changedWindow, change.Original));
				}

				change.Dispose();
				changes.RemoveAt(i);
			}
		}

		return takenChanges;
	}

	// The window is held with a Java weak reference, because the .NET object of a Java window can be collected and created again while the window lives on
	sealed class WindowChange<T>(AWindow changedWindow, T original, Page owner) : IDisposable
	{
		readonly Java.Lang.Ref.WeakReference windowReference = new(changedWindow);
		readonly WeakReference<Page> ownerReference = new(owner);

		public T Original { get; } = original;

		public AWindow? Window => windowReference.Get() as AWindow;

		public bool IsOwnedBy(Page page) => ownerReference.TryGetTarget(out var currentOwner) && ReferenceEquals(currentOwner, page);

		public void SetOwner(Page page) => ownerReference.SetTarget(page);

		public void Dispose() => windowReference.Dispose();
	}

	readonly record struct NavigationBarAppearance(bool IsLight, bool IsForcedLight, int? NavigationBarColor);

	[SupportedOSPlatform("android35.0")]
	sealed class NavigationBarOverlayInsetsListener : Java.Lang.Object, AView.IOnApplyWindowInsetsListener
	{
		public WindowInsets OnApplyWindowInsets(AView view, WindowInsets insets)
		{
			// The insets of the window itself: the decor view takes the navigation bar out of the insets of its children when it fits the system
			// windows, and the overlay is still over the navigation bar then
			UpdateNavigationBarOverlayLayout(view, view.RootWindowInsets ?? insets);
			return insets;
		}
	}
}