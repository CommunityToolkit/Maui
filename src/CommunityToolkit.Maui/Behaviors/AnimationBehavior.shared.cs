using System.Diagnostics;
using CommunityToolkit.Maui.Animations;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// The <see cref="AnimationBehavior"/> is a behavior that shows an animation on any <see cref="VisualElement"/> when the <see cref="AnimateCommand"/> is called.
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
		BindableProperty.CreateReadOnly(nameof(AnimateCommand), typeof(Command<CancellationToken>), typeof(AnimationBehavior), default, BindingMode.OneWayToSource, propertyChanging: OnAnimateCommandChanging, defaultValueCreator: CreateAnimateCommand).BindableProperty;

	TapGestureRecognizer? tapGestureRecognizer;

	/// <summary>
	/// Gets the Command that allows the triggering of the animation.
	/// </summary>
	/// <remarks>
	/// <see cref="AnimateCommand"/> has a <see cref="Type"/> of Command&lt;CancellationToken&gt; which requires a <see cref="CancellationToken"/> as a CommandParameter. See <see cref="Command{CancellationToken}"/> and <see cref="System.Windows.Input.ICommand.Execute(object)"/> for more information on passing a <see cref="CancellationToken"/> into <see cref="Command{T}"/> as a CommandParameter"
	/// </remarks>
	public Command<CancellationToken> AnimateCommand => (Command<CancellationToken>)GetValue(AnimateCommandProperty);

	/// <summary>
	/// The type of animation to perform.
	/// </summary>
	public BaseAnimation? AnimationType
	{
		get => (BaseAnimation?)GetValue(AnimationTypeProperty);
		set => SetValue(AnimationTypeProperty, value);
	}

	/// <inheritdoc/>
	protected override void OnAttachedTo(VisualElement bindable)
	{
		base.OnAttachedTo(bindable);

		if (string.IsNullOrWhiteSpace(EventName))
		{
			if (bindable is ITextInput)
			{
				throw new InvalidOperationException($"Animation Behavior can not be attached to {nameof(ITextInput)} without using the EventName property.");
			}

			if (bindable is not IGestureRecognizers gestureRecognizers)
			{
				throw new InvalidOperationException($"VisualElement does not implement {nameof(IGestureRecognizers)}.");
			}

			tapGestureRecognizer = new TapGestureRecognizer();
			tapGestureRecognizer.Tapped += OnTriggerHandled;

			gestureRecognizers.GestureRecognizers.Add(tapGestureRecognizer);
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
		await OnAnimate(CancellationToken.None);

		base.OnTriggerHandled(sender, eventArgs);
	}

	static Command<CancellationToken> CreateAnimateCommand(BindableObject bindable)
	{
		var animationBehavior = (AnimationBehavior)bindable;
		return new Command<CancellationToken>(async token => await animationBehavior.OnAnimate(token).ConfigureAwait(false));
	}

	static void OnAnimateCommandChanging(BindableObject bindable, object oldValue, object newValue)
	{
		if (newValue is not Command<CancellationToken>)
		{
			var animationBehavior = (AnimationBehavior)bindable;
			throw new InvalidOperationException($"{nameof(AnimateCommand)} must of Type {animationBehavior.AnimateCommand.GetType().FullName}");
		}
	}

	async Task OnAnimate(CancellationToken token)
	{
		if (View is null || AnimationType is null)
		{
			return;
		}

		View.CancelAnimations();

		try
		{
			// We must `await` `AnimationType.Animate()` here in order to properly implement `Options.ShouldSuppressExceptionsInAnimations`
			// Returning the `Task` would cause the `OnAnimate()` method to return immediately, before `AnimationType.Animate()` has completed. Returning immediately exits our try/catch block and thus negates our opportunity to handle any Exceptions which breaks `Options.ShouldSuppressExceptionsInAnimations`.
			await AnimationType.Animate(View, token);
		}
		catch (Exception ex) when (Options.ShouldSuppressExceptionsInAnimations)
		{
			Trace.WriteLine(ex);
		}
	}
}