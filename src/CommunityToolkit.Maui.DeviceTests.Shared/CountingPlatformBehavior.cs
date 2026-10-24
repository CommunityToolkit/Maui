using Microsoft.Maui.Controls;
#if IOS || MACCATALYST
using PlatformView = UIKit.UIView;
#elif ANDROID
using PlatformView = Android.Views.View;
#elif WINDOWS
using PlatformView = Microsoft.UI.Xaml.FrameworkElement;
#endif

namespace CommunityToolkit.Maui.DeviceTests;

// Based on the probe in dotnet/maui's PlatformBehaviorTests. MAUI attaches a PlatformBehavior only once the view is Loaded (in a
// window), and detaches it when the view is Unloaded or the behavior is removed.
public sealed class CountingPlatformBehavior<TView> : PlatformBehavior<TView, PlatformView>
	where TView : VisualElement
{
	public int AttachedCount { get; private set; }

	public int DetachedCount { get; private set; }

	public bool IsAttached { get; private set; }

	public PlatformView? AttachedPlatformView { get; private set; }

	protected override void OnAttachedTo(TView bindable, PlatformView platformView)
	{
		base.OnAttachedTo(bindable, platformView);
		IsAttached = true;
		AttachedCount++;
		AttachedPlatformView = platformView;
	}

	protected override void OnDetachedFrom(TView bindable, PlatformView platformView)
	{
		base.OnDetachedFrom(bindable, platformView);
		IsAttached = false;
		DetachedCount++;
	}
}