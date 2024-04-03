#if IOS || MACOS || MACCATALYST
using PlatformView = UIKit.UIView;
#elif ANDROID
using PlatformView = Android.Views.View;
#elif WINDOWS
using PlatformView = Microsoft.UI.Xaml.FrameworkElement;
#elif TIZEN
using PlatformView = Tizen.NUI.BaseComponents.View;
#elif NET6_0_OR_GREATER || (NETSTANDARD || !PLATFORM)
using PlatformView = System.Object;
#endif

using System.ComponentModel;

namespace CommunityToolkit.Maui.Behaviors;


/// <summary>
/// Abstract class for our behaviors to inherit.
/// </summary>
/// <typeparam name="TView">The <see cref="VisualElement"/> that the behavior can be applied to</typeparam>
public abstract class BasePlatformBehavior<TView> : BasePlatformBehavior<TView, PlatformView>
	where TView : Element
{
	private protected BasePlatformBehavior()
	{

	}
}

/// <summary>
/// Abstract class for our behaviors to inherit.
/// </summary>
/// <typeparam name="TView">The <see cref="VisualElement"/> that the behavior can be applied to</typeparam>
/// <typeparam name="TPlatformView">The <see langword="class"/> that the behavior can be applied to</typeparam>
public abstract class BasePlatformBehavior<TView, TPlatformView> : PlatformBehavior<TView, TPlatformView>, ICommunityToolkitBehavior<TView>
	where TView : Element
	where TPlatformView : class
{
	private protected BasePlatformBehavior()
	{

	}

	/// <summary>
	/// View used by the Behavior
	/// </summary>
	protected TView? View { get; set; }

	TView? ICommunityToolkitBehavior<TView>.View
	{
		get => View;
		set => View = value;
	}

	/// <summary>
	/// Virtual method that executes when a property on the View has changed
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	protected virtual void OnViewPropertyChanged(TView sender, PropertyChangedEventArgs e)
	{

	}

	/// <inheritdoc/>
	protected override void OnAttachedTo(TView bindable, TPlatformView platformView)
	{
		base.OnAttachedTo(bindable, platformView);

		((ICommunityToolkitBehavior<TView>)this).AssignViewAndBingingContext(bindable);
	}

	/// <inheritdoc/>
	protected override void OnDetachedFrom(TView bindable, TPlatformView platformView)
	{
		base.OnDetachedFrom(bindable, platformView);

		((ICommunityToolkitBehavior<TView>)this).UnassignViewAndBingingContext(bindable);
	}

	void ICommunityToolkitBehavior<TView>.OnViewPropertyChanged(TView sender, PropertyChangedEventArgs e) => OnViewPropertyChanged(sender, e);
}