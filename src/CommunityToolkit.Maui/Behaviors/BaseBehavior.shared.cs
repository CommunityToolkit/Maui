using System.ComponentModel;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// Abstract class for our behaviors to inherit.
/// </summary>
/// <typeparam name="TView">The <see cref="VisualElement"/> that the behavior can be applied to</typeparam>
public abstract class BaseBehavior<TView> : Behavior<TView>, ICommunityToolkitBehavior<TView> where TView : VisualElement
{
	TView? ICommunityToolkitBehavior<TView>.View
	{
		get => View;
		set => View = value;
	}

	/// <summary>
	/// View used by the Behavior
	/// </summary>
	protected TView? View { get; private set; }

	void ICommunityToolkitBehavior<TView>.OnViewPropertyChanged(TView sender, PropertyChangedEventArgs e) => OnViewPropertyChanged(sender, e);

	/// <summary>
	/// Virtual method that executes when a property on the View has changed
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	protected virtual void OnViewPropertyChanged(TView sender, PropertyChangedEventArgs e)
	{

	}

	/// <inheritdoc/>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="bindable"/> is <see langword="null"/>.</exception>
	protected override void OnAttachedTo(TView bindable)
	{
		ArgumentNullException.ThrowIfNull(bindable);

		base.OnAttachedTo(bindable);

		((ICommunityToolkitBehavior<TView>)this).InitializeBehavior(bindable);
	}

	/// <inheritdoc/>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="bindable"/> is <see langword="null"/>.</exception>
	protected override void OnDetachingFrom(TView bindable)
	{
		ArgumentNullException.ThrowIfNull(bindable);

		base.OnDetachingFrom(bindable);

		((ICommunityToolkitBehavior<TView>)this).UninitializeBehavior(bindable);
	}
}