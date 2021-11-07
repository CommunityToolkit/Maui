using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Maui.Animations;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Behaviors.Internals;

/// <summary>
/// The <see cref="AnimationBehavior"/> is a behavior that shows an animation on any <see cref="View"/> when the <see cref="AnimateCommand"/> is called.
/// </summary>
public class AnimationBehavior : EventToCommandBehavior
{
	/// <summary>
	/// Backing BindableProperty for the <see cref="AnimationType"/> property.
	/// </summary>
	public static readonly BindableProperty AnimationTypeProperty =
		BindableProperty.Create(nameof(AnimationType), typeof(AnimationBase), typeof(AnimationBehavior));

	/// <summary>
	/// Backing BindableProperty for the <see cref="AnimateCommand"/> property.
	/// </summary>
	internal static readonly BindablePropertyKey AnimateCommandPropertyKey =
 			BindableProperty.CreateReadOnly(
 				nameof(AnimateCommand),
 				typeof(ICommand),
 				typeof(AnimationBehavior),
 				default,
 				BindingMode.OneWayToSource,
 				defaultValueCreator: CreateAnimateCommand);
	
	/// <summary>
	/// Backing BindableProperty for the <see cref="AnimateCommand"/> property.
	/// </summary>
	public static readonly BindableProperty AnimateCommandProperty = AnimateCommandPropertyKey.BindableProperty;

	/// <summary>
	/// The type of animation to perform
	/// </summary>
	public AnimationBase? AnimationType
	{
		get => (AnimationBase?)GetValue(AnimationTypeProperty);
		set => SetValue(AnimationTypeProperty, value);
	}

	/// <summary>
	/// Command on which to perform the animation.
	/// </summary>
	public ICommand AnimateCommand => (ICommand)GetValue(AnimateCommandProperty);

	bool isAnimating;
	TapGestureRecognizer? tapGestureRecognizer;

	/// <inheritdoc/>
	protected override void OnAttachedTo(VisualElement bindable)
	{
		base.OnAttachedTo(bindable);

		if (!string.IsNullOrWhiteSpace(EventName) || !(bindable is View view))
			return;

		tapGestureRecognizer = new TapGestureRecognizer();
		tapGestureRecognizer.Tapped += OnTriggerHandled;
		view.GestureRecognizers.Clear();
		view.GestureRecognizers.Add(tapGestureRecognizer);
	}

	/// <inheritdoc/>
	protected override void OnDetachingFrom(VisualElement bindable)
	{
		if (tapGestureRecognizer != null)
			tapGestureRecognizer.Tapped -= OnTriggerHandled;

		base.OnDetachingFrom(bindable);
	}

	/// <inheritdoc/>
	protected override async void OnTriggerHandled(object? sender = null, object? eventArgs = null)
	{
		await OnAnimate();

		base.OnTriggerHandled(sender, eventArgs);
	}

	static object CreateAnimateCommand(BindableObject bindable)
		=> new Command(async () => await ((AnimationBehavior)bindable).OnAnimate()); //TODO replace with AsyncCommand
	
	async Task OnAnimate()
	{
		if (isAnimating || View is not View typedView)
			return;

		isAnimating = true;

		if (AnimationType != null)
			await AnimationType.Animate(typedView);

		isAnimating = false;
	}
}
