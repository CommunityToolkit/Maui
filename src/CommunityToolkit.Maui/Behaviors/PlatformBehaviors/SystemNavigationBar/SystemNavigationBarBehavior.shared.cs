
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Platform;
using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// <see cref="PlatformBehavior{TView,TPlatformView}"/> that controls the System navigation bar color
/// </summary>
[UnsupportedOSPlatform("Windows"), UnsupportedOSPlatform("MacCatalyst"), UnsupportedOSPlatform("MacOS"), UnsupportedOSPlatform("Tizen"), UnsupportedOSPlatform("iOS")]
public class SystemNavigationBarBehavior : PlatformBehavior<Page>
{
	/// <summary>
	/// <see cref="BindableProperty"/> that manages the SystemNavigationBarColor property.
	/// </summary>
	public static readonly BindableProperty SystemNavigationBarColorProperty =
		BindableProperty.Create(nameof(SystemNavigationBarColor), typeof(Color), typeof(SystemNavigationBarBehavior), Colors.Transparent);


	/// <summary>
	/// <see cref="BindableProperty"/> that manages the SystemNavigationBarStyle property.
	/// </summary>
	public static readonly BindableProperty SystemNavigationBarStyleProperty =
		BindableProperty.Create(nameof(SystemNavigationBarStyle), typeof(SystemNavigationBarStyle), typeof(SystemNavigationBarBehavior), SystemNavigationBarStyle.Default);

	/// <summary>
	/// Property that holds the value of the system navigation bar color. 
	/// </summary>
	public Color SystemNavigationBarColor
	{
		get => (Color)GetValue(SystemNavigationBarColorProperty);
		set => SetValue(SystemNavigationBarColorProperty, value);
	}

	/// <summary>
	/// Property that holds the value of the system navigation bar style. 
	/// </summary>
	public SystemNavigationBarStyle SystemNavigationBarStyle
	{
		get => (SystemNavigationBarStyle)GetValue(SystemNavigationBarStyleProperty);
		set => SetValue(SystemNavigationBarStyleProperty, value);
	}

#if !(WINDOWS || MACCATALYST || TIZEN || IOS)

	/// <inheritdoc /> 
#if ANDROID
	protected override void OnAttachedTo(Page bindable, Android.Views.View platformView)
#else
	protected override void OnAttachedTo(Page bindable, object platformView)
#endif
	{
		SystemNavigationBar.SetColor(SystemNavigationBarColor);
		SystemNavigationBar.SetStyle(SystemNavigationBarStyle);
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

		if (propertyName.IsOneOf(SystemNavigationBarColorProperty, VisualElement.WidthProperty, VisualElement.HeightProperty))
		{
			SystemNavigationBar.SetColor(SystemNavigationBarColor);
		}
		else if (propertyName == SystemNavigationBarStyleProperty.PropertyName)
		{
			SystemNavigationBar.SetStyle(SystemNavigationBarStyle);
		}
	}
#endif
}