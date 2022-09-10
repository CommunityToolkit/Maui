
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Capabilities;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// <see cref="PlatformBehavior{TView,TPlatformView}"/> that controls the Status bar color
/// </summary>
[UnsupportedOSPlatform("windows")]
public class StatusBarBehavior : PlatformBehavior<VisualElement>
{
	/// <summary>
	/// <see cref="BindableProperty"/> that manages the StatusBarColor property.
	/// </summary>
	public static readonly BindableProperty StatusBarColorProperty =
		BindableProperty.Create(nameof(StatusBarColor), typeof(Color), typeof(StatusBarBehavior));

	/// <summary>
	/// Property that holds the value of the Status bar color. 
	/// </summary>
	public Color StatusBarColor
	{
		get => (Color)GetValue(StatusBarColorProperty);
		set => SetValue(StatusBarColorProperty, value);
	}


	/// <summary>
	/// <see cref="BindableProperty"/> that manages the StatusBarColor property.
	/// </summary>
	public static readonly BindableProperty StatusBarStyleProperty =
		BindableProperty.Create(nameof(StatusBarStyle), typeof(StatusBarStyle), typeof(StatusBarBehavior), StatusBarStyle.Default);

	/// <summary>
	/// Property that holds the value of the Status bar color. 
	/// </summary>
	public StatusBarStyle StatusBarStyle
	{
		get => (StatusBarStyle)GetValue(StatusBarStyleProperty);
		set => SetValue(StatusBarStyleProperty, value);
	}

#if !WINDOWS

	/// <inheritdoc /> 
#if IOS || MACCATALYST
	protected override void OnAttachedTo(VisualElement bindable, UIKit.UIView platformView)
#elif ANDROID
	protected override void OnAttachedTo(VisualElement bindable, Android.Views.View platformView)
#else
	protected override void OnAttachedTo(VisualElement bindable, object platformView)
#endif
	{
		StatusBar.SetColor(StatusBarColor);
		StatusBar.SetStyle(StatusBarStyle);
	}


	/// <inheritdoc /> 
	protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		base.OnPropertyChanged(propertyName);

		if (propertyName == StatusBarColorProperty.PropertyName)
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