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
	OnPageNavigatedTo,
}

/// <summary>
/// <see cref="PlatformBehavior{TView,TPlatformView}"/> that controls the Status bar color
/// </summary>
[UnsupportedOSPlatform("Windows"), UnsupportedOSPlatform("MacCatalyst"), UnsupportedOSPlatform("MacOS"), UnsupportedOSPlatform("Tizen")]
public class StatusBarBehavior : PlatformBehavior<Page>
{
	/// <summary>
	/// <see cref="BindableProperty"/> that manages the StatusBarColor property.
	/// </summary>
	public static readonly BindableProperty StatusBarColorProperty =
		BindableProperty.Create(nameof(StatusBarColor), typeof(Color), typeof(StatusBarBehavior), Colors.Transparent);

	/// <summary>
	/// <see cref="BindableProperty"/> that manages the StatusBarColor property.
	/// </summary>
	public static readonly BindableProperty StatusBarStyleProperty =
		BindableProperty.Create(nameof(StatusBarStyle), typeof(StatusBarStyle), typeof(StatusBarBehavior), StatusBarStyle.Default);

	/// <summary>
	/// <see cref="BindableProperty"/> that manages the ApplyOn property.
	/// </summary>
	public static readonly BindableProperty ApplyOnProperty =
		BindableProperty.Create(nameof(ApplyOn), typeof(StatusBarApplyOn), typeof(StatusBarBehavior), StatusBarApplyOn.OnBehaviorAttachedTo);

	/// <summary>
	/// Property that holds the value of the Status bar color. 
	/// </summary>
	public Color StatusBarColor
	{
		get => (Color)GetValue(StatusBarColorProperty);
		set => SetValue(StatusBarColorProperty, value);
	}

	/// <summary>
	/// Property that holds the value of the Status bar color. 
	/// </summary>
	public StatusBarStyle StatusBarStyle
	{
		get => (StatusBarStyle)GetValue(StatusBarStyleProperty);
		set => SetValue(StatusBarStyleProperty, value);
	}

	/// <summary>
	/// When the status bar color and style should be applied.
	/// </summary>
	public StatusBarApplyOn ApplyOn
	{
		get => (StatusBarApplyOn)GetValue(ApplyOnProperty);
		set => SetValue(ApplyOnProperty, value);
	}


#if !(WINDOWS || MACCATALYST || TIZEN)

	/// <inheritdoc /> 
#if IOS
	protected override void OnAttachedTo(Page bindable, UIKit.UIView platformView)
#elif ANDROID
	protected override void OnAttachedTo(Page bindable, Android.Views.View platformView)
#else
	protected override void OnAttachedTo(Page bindable, object platformView)
#endif
	{
		if (ApplyOn == StatusBarApplyOn.OnBehaviorAttachedTo)
		{
			StatusBar.SetColor(StatusBarColor);
			StatusBar.SetStyle(StatusBarStyle);
		}

		bindable.NavigatedTo += OnPageNavigatedTo;
#if IOS
		bindable.SizeChanged += OnPageSizeChanged;
#endif
	}

	/// <inheritdoc /> 
#if IOS
	protected override void OnDetachedFrom(Page bindable, UIKit.UIView platformView)
#elif ANDROID
	protected override void OnDetachedFrom(Page bindable, Android.Views.View platformView)
#else
	protected override void OnDetachedFrom(Page bindable, object platformView)
#endif
	{
#if IOS
		bindable.SizeChanged -= OnPageSizeChanged;
#endif
		bindable.NavigatedTo -= OnPageNavigatedTo;
	}

#if IOS
	void OnPageSizeChanged(object? sender, EventArgs e)
	{
		StatusBar.UpdateBarSize();
	}
#endif

	void OnPageNavigatedTo(object? sender, NavigatedToEventArgs e)
	{
		if (ApplyOn == StatusBarApplyOn.OnPageNavigatedTo)
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