using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;
using CommunityToolkit.Maui.Animations;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// The <see cref="AnimationBehavior"/> is a behavior that shows an animation on any <see cref="View"/> when the <see cref="AnimateCommand"/> is called.
/// </summary>
public class AnimationBehavior : EventToCommandBehavior
{
	/// <summary>
	/// Backing BindableProperty for the <see cref="AnimationType"/> property.
	/// </summary>
	public static readonly BindableProperty AnimationTypeProperty =
		BindableProperty.Create(nameof(AnimationType), typeof(BaseAnimation), typeof(AnimationBehavior));

	/// <summary>
	/// Backing BindableProperty for the <see cref="AnimateCommand"/> property.
	/// </summary>
	public static readonly BindableProperty AnimateCommandProperty =
		BindableProperty.CreateReadOnly(nameof(AnimateCommand), typeof(ICommand), typeof(AnimationBehavior), default, BindingMode.OneWayToSource, defaultValueCreator: CreateAnimateCommand).BindableProperty;

	TapGestureRecognizer? tapGestureRecognizer;

	/// <summary>
	/// Command on which to perform the animation.
	/// </summary>
	public ICommand AnimateCommand => (ICommand)GetValue(AnimateCommandProperty);

	/// <summary>
	/// The type of animation to perform
	/// </summary>
	public BaseAnimation? AnimationType
	{
		get => (BaseAnimation?)GetValue(AnimationTypeProperty);
		set => SetValue(AnimationTypeProperty, value);
	}

	/// <inheritdoc/>
	[MemberNotNull(nameof(tapGestureRecognizer))]
	protected override void OnAttachedTo(VisualElement bindable)
	{
		base.OnAttachedTo(bindable);

		if (!string.IsNullOrWhiteSpace(EventName))
		{
			throw new InvalidOperationException($"{nameof(EventName)} must be null. It is not used by {nameof(AnimationBehavior)}");
		}

		if (bindable is not IGestureRecognizers gestureRecognizers)
		{
			throw new InvalidOperationException($"VisualElement does not implement {nameof(IGestureRecognizers)}");
		}

		if (bindable is not ITextInput)
		{
			tapGestureRecognizer = new TapGestureRecognizer();
			tapGestureRecognizer.Tapped += OnTriggerHandled;

			foreach (var recognizer in gestureRecognizers.GestureRecognizers.OfType<TapGestureRecognizer>())
			{
				gestureRecognizers.GestureRecognizers.Remove(recognizer);
			}

			gestureRecognizers.GestureRecognizers.Add(tapGestureRecognizer);
		}
		else
		{
			throw new InvalidOperationException($"Animation Behavior can not be attached to {nameof(ITextInput)}");
		}
	}

	/// <inheritdoc/>
	protected override void OnDetachingFrom(VisualElement bindable)
	{
		if (tapGestureRecognizer != null)
		{
			tapGestureRecognizer.Tapped -= OnTriggerHandled;
			tapGestureRecognizer = null;
		}

		base.OnDetachingFrom(bindable);
	}

	/// <inheritdoc/>
	protected override async void OnTriggerHandled(object? sender = null, object? eventArgs = null)
	{
		await OnAnimate();

		base.OnTriggerHandled(sender, eventArgs);
	}

	static object CreateAnimateCommand(BindableObject bindable)
		=> new Command(async () => await ((AnimationBehavior)bindable).OnAnimate().ConfigureAwait(false));

	Task OnAnimate()
	{
		if (View is null || AnimationType is null)
		{
			return Task.CompletedTask;
		}

		View.CancelAnimations();
		return AnimationType.Animate(View);
	}
}