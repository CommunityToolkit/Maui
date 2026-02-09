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
	OnPageNavigatedTo
}

/// <summary>
/// <see cref="PlatformBehavior{TView,TPlatformView}"/> that controls the Status bar color
/// </summary>
[UnsupportedOSPlatform("Windows"), UnsupportedOSPlatform("MacCatalyst"), UnsupportedOSPlatform("MacOS"), UnsupportedOSPlatform("Tizen")]
public partial class StatusBarBehavior : BasePlatformBehavior<Page>
{
	/// <summary>
	/// Property that holds the value of the Status bar color. 
	/// </summary>
	[BindableProperty]
	public partial Color StatusBarColor { get; set; } = StatusBarBehaviorDefaults.StatusBarColor;

	/// <summary>
	/// Property that holds the value of the Status bar color. 
	/// </summary>
	[BindableProperty]
	public partial StatusBarStyle StatusBarStyle { get; set; } = StatusBarBehaviorDefaults.StatusBarStyle;

	/// <summary>
	/// When the status bar color and style should be applied.
	/// </summary>
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

		if (ApplyOn is StatusBarApplyOn.OnBehaviorAttachedTo)
		{
#if IOS
			StatusBar.SetBarSize(Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.Page.GetUseSafeArea(page));
#endif

			StatusBar.SetColor(StatusBarColor);
			StatusBar.SetStyle(StatusBarStyle);
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
	}

#if IOS
	static void OnPageSizeChanged(object? sender, EventArgs e)
	{
		ArgumentNullException.ThrowIfNull(sender);
		StatusBar.SetBarSize(Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.Page.GetUseSafeArea((Page)sender));
	}
#endif

	void OnPageNavigatedTo(object? sender, NavigatedToEventArgs e)
	{
		if (ApplyOn is StatusBarApplyOn.OnPageNavigatedTo)
		{
			StatusBar.SetColor(StatusBarColor);
			StatusBar.SetStyle(StatusBarStyle);
		}
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
			StatusBar.SetStyle(StatusBarStyle);
		}
	}
#endif
}