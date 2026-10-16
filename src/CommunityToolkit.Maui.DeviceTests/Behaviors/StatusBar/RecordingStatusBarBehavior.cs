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

/// <summary>
/// A <see cref="StatusBarBehavior"/> that records the calls MAUI makes (attach, detach, view property changes) and can raise
/// <see cref="BindableObject.OnPropertyChanged"/> for any property name, which the toolkit's behavior otherwise only receives for its own properties.
/// </summary>
public sealed class RecordingStatusBarBehavior : StatusBarBehavior
{
	public int AttachedCount { get; private set; }

	public int DetachedCount { get; private set; }

	public bool IsAttached { get; private set; }

	public PlatformView? AttachedPlatformView { get; private set; }

	/// <summary>The protected <c>View</c> of the base class: the page while attached, <see langword="null"/> otherwise.</summary>
	public Page? AttachedPage => View;

	public List<(Page Sender, string? PropertyName)> ViewPropertyChanges { get; } = [];

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