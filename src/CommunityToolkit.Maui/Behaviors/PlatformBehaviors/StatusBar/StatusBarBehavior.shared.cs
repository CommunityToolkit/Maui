using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Platform;
using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// When to apply the status bar color and style.
/// </summary>
public enum StatusBarApplyOn
{
	/// <summary>
	/// Apply color and style when the behavior has been attached to the page.
	/// </summary>
	OnBehaviorAttachedTo,

	/// <summary>
	/// Apply color and style when the page has been navigated to.
	/// </summary>
	/// <remarks>
	/// A navigation that .NET MAUI reports before the page loads applies nothing: see <see cref="StatusBarBehavior.ApplyOn"/>.
	/// </remarks>
	OnPageNavigatedTo
}

/// <summary>
/// <see cref="PlatformBehavior{TView,TPlatformView}"/> that controls the Status bar color
/// </summary>
/// <remarks>
/// Android needs 6.0 (API 23) or later. On Android 5.0 and 5.1 (API 21 and 22) the calls that the behavior makes to <see cref="StatusBar"/> do nothing, so the
/// behavior changes neither the color nor the style of the status bar.
/// </remarks>
[UnsupportedOSPlatform("Windows"), UnsupportedOSPlatform("MacCatalyst"), UnsupportedOSPlatform("MacOS"), UnsupportedOSPlatform("Tizen")]
public partial class StatusBarBehavior : BasePlatformBehavior<Page>
{
	/// <summary>
	/// Property that holds the value of the Status bar color. 
	/// </summary>
	[BindableProperty]
	public partial Color StatusBarColor { get; set; } = StatusBarBehaviorDefaults.StatusBarColor;

	/// <summary>
	/// Property that holds the value of the Status bar style.
	/// </summary>
	/// <remarks>
	/// <para>
	/// iOS, .NET 10: the behavior sets the style of the whole app with <see cref="StatusBar.SetStyle(StatusBarStyle)"/>, whose remarks say which status
	/// bars show it, when it applies the style (see <see cref="ApplyOn"/>) and whenever the style changes, also while it is not attached to a page. The
	/// style stays until a behavior or <see cref="StatusBar.SetStyle(StatusBarStyle)"/> sets another one; <see cref="StatusBarStyle.Default"/> gives every
	/// view controller its own style back.
	/// </para>
	/// <para>
	/// iOS, .NET 11: the behavior sets <c>Window.StatusBarTheme</c> of its page's window, and the view controllers of .NET MAUI return the matching
	/// style: <see cref="StatusBarStyle.LightContent"/> sets <c>StatusBarTheme.Dark</c>, <see cref="StatusBarStyle.DarkContent"/> sets
	/// <c>StatusBarTheme.Light</c> and <see cref="StatusBarStyle.Default"/> sets <c>StatusBarTheme.Default</c>. It does so when it applies the style
	/// (see <see cref="ApplyOn"/>) and when the style changes while it is attached. The other windows of the app keep their theme, and a style that
	/// changes while the behavior is not attached to a page of a window changes no status bar. UIKit applies the theme only when the app's Info.plist
	/// does not set <c>UIViewControllerBasedStatusBarAppearance</c> to false. With false (the .NET 10 setup of this behavior) the behavior also applies
	/// the style to the whole app with UIApplication.SetStatusBarStyle, as on .NET 10, when it sets the theme: UIKit applies it in an app built with an
	/// iOS SDK before 27.0. In an app built with a later SDK the status bar keeps its style and the behavior writes a trace message: remove the key.
	/// </para>
	/// </remarks>
	[BindableProperty]
	public partial StatusBarStyle StatusBarStyle { get; set; } = StatusBarBehaviorDefaults.StatusBarStyle;

	/// <summary>
	/// When the status bar color and style should be applied.
	/// </summary>
	/// <remarks>
	/// With <see cref="StatusBarApplyOn.OnPageNavigatedTo"/> the behavior applies them when the page raises <see cref="Page.NavigatedTo"/> while the
	/// behavior is attached, and the behavior attaches when the page loads: a NavigatedTo that .NET MAUI raises before the page loads applies nothing.
	/// On iOS that is the case when a TabbedPage selects the tab of the page and when a FlyoutPage shows the page as its Detail, and, with .NET 11, also
	/// when a NavigationPage pushes the page, or pops back to it, without animation.
	/// </remarks>
	[BindableProperty]
	public partial StatusBarApplyOn ApplyOn { get; set; } = StatusBarBehaviorDefaults.ApplyOn;


#if !(WINDOWS || MACCATALYST || TIZEN)
	/// <inheritdoc /> 
#if IOS
	protected override void OnAttachedTo(Page page, UIKit.UIView platformView)
#elif ANDROID
	protected override void OnAttachedTo(Page page, Android.Views.View platformView)
#else
	protected override void OnAttachedTo(Page page, object platformView)
#endif
	{
		base.OnAttachedTo(page, platformView);

		DeviceDisplay.Current.MainDisplayInfoChanged += OnMainDisplayInfoChanged;

		if (ApplyOn is StatusBarApplyOn.OnBehaviorAttachedTo)
		{
#if IOS
			StatusBar.SetBarSize(Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.Page.GetUseSafeArea(page));
#endif

			StatusBar.SetColor(StatusBarColor);
			ApplyStatusBarStyle();
		}

		page.NavigatedTo += OnPageNavigatedTo;
#if IOS
		page.SizeChanged += OnPageSizeChanged;
#endif
	}

	/// <inheritdoc /> 
#if IOS
	protected override void OnDetachedFrom(Page page, UIKit.UIView platformView)
#elif ANDROID
	protected override void OnDetachedFrom(Page page, Android.Views.View platformView)
#else
	protected override void OnDetachedFrom(Page page, object platformView)
#endif
	{
#if IOS
		page.SizeChanged -= OnPageSizeChanged;
#endif
		base.OnDetachedFrom(page, platformView);

		page.NavigatedTo -= OnPageNavigatedTo;
		DeviceDisplay.Current.MainDisplayInfoChanged -= OnMainDisplayInfoChanged;
	}

	/// <inheritdoc /> 
	protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		base.OnPropertyChanged(propertyName);

		if (string.IsNullOrWhiteSpace(propertyName))
		{
			return;
		}

#if ANDROID
		if (Platform.CurrentActivity is null)
		{
			return;
		}
#endif

		if (propertyName.IsOneOf(StatusBarColorProperty, VisualElement.WidthProperty, VisualElement.HeightProperty))
		{
			StatusBar.SetColor(StatusBarColor);
		}
		else if (propertyName == StatusBarStyleProperty.PropertyName)
		{
			ApplyStatusBarStyle();
		}
	}
#endif

#if IOS
	static void OnPageSizeChanged(object? sender, EventArgs e)
	{
		ArgumentNullException.ThrowIfNull(sender);
		StatusBar.SetBarSize(Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.Page.GetUseSafeArea((Page)sender));
	}
#endif

#if IOS && NET11_0_OR_GREATER
	// UseMauiCommunityToolkit connects StatusBar.SetStyle to this method (StatusBar.SetStatusBarThemeOfEveryWindow): only Microsoft.Maui.Controls.Window
	// can change the StatusBarTheme that the view controllers of .NET MAUI return, and CommunityToolkit.Maui.Core does not reference Microsoft.Maui.Controls
	internal static void SetStatusBarThemeOfEveryWindow(StatusBarTheme statusBarTheme)
	{
		foreach (var window in Application.Current?.Windows ?? [])
		{
			window.StatusBarTheme = statusBarTheme;
		}
	}
#endif

	void OnPageNavigatedTo(object? sender, NavigatedToEventArgs e)
	{
		if (ApplyOn is StatusBarApplyOn.OnPageNavigatedTo)
		{
			StatusBar.SetColor(StatusBarColor);
			ApplyStatusBarStyle();
		}
	}

	// iOS, .NET 11: the behavior sets the theme of its page's window only, where StatusBar.SetStyle sets the theme of every window (see the remarks of
	// StatusBarStyle). The style is mapped first, so that an undefined StatusBarStyle throws NotSupportedException whether or not the behavior is
	// attached, as StatusBar.SetStyle does.
	void ApplyStatusBarStyle()
	{
#if IOS && NET11_0_OR_GREATER
		var statusBarTheme = StatusBar.ToStatusBarTheme(StatusBarStyle);

		if (View?.Window is Window window)
		{
			window.StatusBarTheme = statusBarTheme;

			// UIKit ignores the theme of the window when Info.plist turns view controller-based status bar appearance off, the .NET 10 setup of this behavior
			if (StatusBar.IsViewControllerBasedStatusBarAppearanceOff)
			{
				StatusBar.SetStyleOfTheWholeApp(StatusBarStyle);
			}
		}
#else
		StatusBar.SetStyle(StatusBarStyle);
#endif
	}

	void OnMainDisplayInfoChanged(object? sender, DisplayInfoChangedEventArgs e)
	{
#if IOS
		if (View is null)
		{
			return;
		}

		StatusBar.SetBarSize(Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.Page.GetUseSafeArea(View));
#elif ANDROID
		if (OperatingSystem.IsAndroidVersionAtLeast(35))
		{
			StatusBar.UpdateBarSize();
		}
#else
		throw new NotSupportedException("StatusBarBehavior is not supported on the current platform.");
#endif
	}
}