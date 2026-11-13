using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that will rotate the supplied view to the specified <see cref="Rotation"/> and then return it to its original rotation.
/// </summary>
/// <remarks>
/// The original rotation is the rotation the view had before it started rotating. An animation that starts while another animation
/// still rotates the same view returns the view to the rotation it had before the first of these animations; the animation it interrupts
/// stops where it is. Cancelling the token while the animation runs returns the view to its original rotation and ends the returned
/// <see cref="Task"/> as canceled.
/// The view rotates over <see cref="BaseAnimation{TAnimatable}.Length"/> and then returns to its original rotation at once.
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
			var wasInterrupted = await WasInterruptedAsync(() => view.RotateToAsync(Rotation, Length, Easing, token), token);

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
