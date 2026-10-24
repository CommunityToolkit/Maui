using System.ComponentModel;
using CommunityToolkit.Maui.Behaviors;
#if IOS || MACCATALYST
using PlatformView = UIKit.UIView;
#elif ANDROID
using PlatformView = Android.Views.View;
#elif WINDOWS
using PlatformView = Microsoft.UI.Xaml.FrameworkElement;
#endif

namespace CommunityToolkit.Maui.DeviceTests;

public sealed class RecordingStatusBarBehavior : StatusBarBehavior
{
	public int AttachedCount { get; private set; }

	public int DetachedCount { get; private set; }

	public bool IsAttached { get; private set; }

	public PlatformView? AttachedPlatformView { get; private set; }

	public Page? AttachedPage => View;

	public List<(Page Sender, string? PropertyName)> ViewPropertyChanges { get; } = [];

	// The toolkit's behavior otherwise only receives OnPropertyChanged for its own properties.
	public void RaisePropertyChanged(string? propertyName) => OnPropertyChanged(propertyName);

	protected override void OnAttachedTo(Page page, PlatformView platformView)
	{
		base.OnAttachedTo(page, platformView);
		IsAttached = true;
		AttachedCount++;
		AttachedPlatformView = platformView;
	}

	protected override void OnDetachedFrom(Page page, PlatformView platformView)
	{
		base.OnDetachedFrom(page, platformView);
		IsAttached = false;
		DetachedCount++;
	}

	protected override void OnViewPropertyChanged(Page sender, PropertyChangedEventArgs e)
	{
		base.OnViewPropertyChanged(sender, e);
		ViewPropertyChanges.Add((sender, e.PropertyName));
	}
}