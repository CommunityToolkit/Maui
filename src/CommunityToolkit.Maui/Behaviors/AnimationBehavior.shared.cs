using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Maui.Animations;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// The <see cref="AnimationBehavior"/> is a behavior that shows an animation on any <see cref="VisualElement"/> when the <see cref="AnimateCommand"/> is called.
/// </summary>
public partial class AnimationBehavior : EventToCommandBehavior
{
	const string animateCommandSetterWarning =
		"""
		Do not use this setter, it only exists to enable XAML Hot reload support in your IDE.

		Instead, apps should provide a value for this OneWayToSource property by creating a binding, in XAML or C#. If done via C# use code like this:

		behavior.SetBinding(AnimationBehavior.AnimateCommandProperty, nameof(ViewModel.TriggerAnimationCommand));
		""";

	TapGestureRecognizer? tapGestureRecognizer;

	/// <summary>
	/// Gets the Command that allows the triggering of the animation.
	///
	/// NOTE: Apps should not directly set this property, treating it as read only. The setter is only public because
	/// that's currently needed to make XAML Hot Reload work. Instead, apps should provide a value for this OneWayToSource
	/// property by creating a binding, in XAML or C#. If done via C# use code like this:
	/// <c>behavior.SetBinding(AnimationBehavior.AnimateCommandProperty, nameof(ViewModel.TriggerAnimationCommand));</c>
	/// </summary>
	/// <remarks>
	/// <see cref="AnimateCommand"/> has a <see cref="Type"/> of Command&lt;CancellationToken&gt; which requires a <see cref="CancellationToken"/> as a CommandParameter. See <see cref="Command{CancellationToken}"/> and <see cref="System.Windows.Input.ICommand.Execute(object)"/> for more information on passing a <see cref="CancellationToken"/> into <see cref="Command{T}"/> as a CommandParameter"
	/// </remarks>
	[BindableProperty(DefaultBindingMode = BindingMode.OneWayToSource, PropertyChangingMethodName = nameof(OnAnimateCommandChanging), DefaultValueCreatorMethodName = nameof(CreateAnimateCommand))]
	public partial Command<CancellationToken> AnimateCommand
	{
		get;
		[Obsolete(animateCommandSetterWarning), EditorBrowsable(EditorBrowsableState.Never)]
		set;
	}

	/// <summary>
	/// The type of animation to perform.
	/// </summary>
	[BindableProperty]
	public partial BaseAnimation? AnimationType { get; set; }

	/// <summary>
	/// Whether a TapGestureRecognizer is added to the control or not
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnAnimateOnTapPropertyChanged))]
	public partial bool AnimateOnTap { get; set; }

	/// <inheritdoc/>
	protected override void OnAttachedTo(VisualElement bindable)
	{
		base.OnAttachedTo(bindable);

		if (AnimateOnTap)
		{
			AddTapGestureRecognizer();
		}
	}

	/// <inheritdoc/>
	protected override void OnDetachingFrom(VisualElement bindable)
	{
		if (tapGestureRecognizer is not null)
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

	static void OnAnimateOnTapPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is not AnimationBehavior behavior)
		{
			return;
		}

		if ((bool)newValue)
		{
			behavior.AddTapGestureRecognizer();
		}
		else
		{
			behavior.RemoveTapGestureRecognizer();
		}
	}

	void AddTapGestureRecognizer()
	{
		if (View is not IGestureRecognizers gestureRecognizers)
		{
			return;
		}

		tapGestureRecognizer = new TapGestureRecognizer();
		tapGestureRecognizer.Tapped += OnTriggerHandled;
		gestureRecognizers.GestureRecognizers.Add(tapGestureRecognizer);
	}

	void RemoveTapGestureRecognizer()
	{
		if (tapGestureRecognizer is null)
		{
			return;
		}

		if (View is not IGestureRecognizers gestureRecognizers)
		{
			return;
		}

		gestureRecognizers.GestureRecognizers.Remove(tapGestureRecognizer);
		tapGestureRecognizer.Tapped -= OnTriggerHandled;
		tapGestureRecognizer = null;
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
			token.ThrowIfCancellationRequested();
			// We must `await` `AnimationType.Animate()` here in order to properly implement `Options.ShouldSuppressExceptionsInAnimations`
			// Returning the `Task` would cause the `OnAnimate()` method to return immediately, before `AnimationType.Animate()` has completed. Returning immediately exits our try/catch block and thus negates our opportunity to handle any Exceptions which breaks `Options.ShouldSuppressExceptionsInAnimations`.
			await AnimationType.Animate(View, token);
		}
		catch (Exception ex) when (Options.ShouldSuppressExceptionsInAnimations)
		{
			Trace.TraceInformation("{0}", ex);
		}
	}
}