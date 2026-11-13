using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that will rotate the supplied view by the specified <see cref="Rotation"/> and then return it to its original rotation.
/// </summary>
/// <remarks>
/// The view turns by <see cref="Rotation"/> degrees from its original rotation over <see cref="BaseAnimation{TAnimatable}.Length"/> (clockwise for a positive value),
/// and then returns to its original rotation at once. The default, 360 degrees, is one full turn, so the return is invisible from any rotation the view starts at.
/// The original rotation is the rotation the view had before it started rotating. An animation that starts while another animation
/// still rotates the same view, for example when an <see cref="Behaviors.AnimationBehavior"/> is triggered again, takes over and turns the view on to the same end,
/// <see cref="Rotation"/> degrees from the rotation it had before the first of these animations, so repeated triggers never leave it turned.
/// The animation it interrupts stops where it is. Cancelling the token while the animation runs returns the view to its original rotation and ends the returned
/// <see cref="Task"/> as canceled.
/// With the system's animations off (for example iOS Reduce Motion or Android's "Remove animations"), the view stays at its original rotation.
/// </remarks>
public partial class RotateAnimation : BaseAnimation
{
	/// <summary>
	/// Initializes a new instance of <see cref="RotateAnimation"/>.
	/// </summary>
	public RotateAnimation() : this(RotateAnimationDefaults.Length)
	{
	}

	/// <summary>
	/// Initializes a new instance of <see cref="RotateAnimation"/>.
	/// </summary>
	/// <param name="defaultLength">The default length of the animation.</param>
	protected RotateAnimation(uint defaultLength) : base(defaultLength)
	{
	}

	/// <summary>
	/// Gets or sets the rotation, in degrees, used by the animation.
	/// </summary>
	/// <remarks>
	/// <see cref="RotateAnimation"/> turns the view by this angle from its original rotation; <see cref="FlipHorizontalAnimation"/> and <see cref="FlipVerticalAnimation"/> turn the view to this angle about the y- or x-axis.
	/// Must be a finite number; assigning NaN or an infinity is ignored.
	/// </remarks>
	[BindableProperty(DefaultBindingMode = BindingMode.OneWay, ValidateValueMethodName = nameof(IsFinite), DefaultValueCreatorMethodName = nameof(CreateRotationDefaultValue))]
	public partial double Rotation { get; set; }

	/// <summary>
	/// Gets the default rotation used by the animation.
	/// </summary>
	protected virtual double DefaultRotation => RotateAnimationDefaults.Rotation;

	/// <inheritdoc />
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="view"/> is <see langword="null"/>.</exception>
	/// <exception cref="OperationCanceledException">The returned <see cref="Task"/> ends canceled when <paramref name="token"/> is cancelled; when that happens while the animation runs, the view returns to its original rotation first.</exception>
	public override Task Animate(VisualElement view, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(view);

		return AnimateOnDispatcherAsync(view, () => RotateAsync(view, token));
	}

	// Animate validates its arguments before it calls this method: in an async method, a null argument would only fault the returned Task instead of throwing at the call that passed it
	async Task RotateAsync(VisualElement view, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		var rotations = AnimatedPropertyTracker.For(view, VisualElement.RotationProperty);
		var (rotationId, originalRotation) = rotations.Start(view.Rotation);

		try
		{
			var wasInterrupted = await WasInterruptedAsync(() => view.RotateToAsync(originalRotation + Rotation, Length, Easing, token), token);

			if (!wasInterrupted && rotations.IsLatest(rotationId))
			{
				view.Rotation = originalRotation;
			}

			if (wasInterrupted && token.IsCancellationRequested)
			{
				// A cancelled animation has no successor to return the view to its original rotation
				if (rotations.IsLatest(rotationId))
				{
					view.Rotation = originalRotation;
				}

				token.ThrowIfCancellationRequested();
			}
		}
		finally
		{
			rotations.End(view.Rotation);
		}
	}

	static object CreateRotationDefaultValue(BindableObject bindable) => ((RotateAnimation)bindable).DefaultRotation;
}
