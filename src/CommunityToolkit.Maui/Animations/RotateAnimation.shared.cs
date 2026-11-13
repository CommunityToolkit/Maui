using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that will rotate the supplied view to the specified <see cref="Rotation"/> and then return it to its original rotation.
/// </summary>
/// <remarks>
/// The original rotation is the rotation the view had before it started rotating. An animation that starts while another animation
/// still rotates the same view returns the view to the rotation it had before the first of these animations; the animation it interrupts
/// stops where it is.
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
	[BindableProperty(DefaultBindingMode = BindingMode.TwoWay, DefaultValueCreatorMethodName = nameof(CreateRotationDefaultValue))]
	public partial double Rotation { get; set; }

	/// <summary>
	/// Gets or sets the default rotation used by the animation.
	/// </summary>
	protected virtual double DefaultRotation { get; set; } = RotateAnimationDefaults.Rotation;

	/// <inheritdoc />
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="view"/> is <see langword="null"/>.</exception>
	public override Task Animate(VisualElement view, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(view);

		return RotateAsync(view, token);
	}

	// Animate validates its arguments before it calls this method: in an async method, a null argument would only fault the returned Task instead of throwing at the call that passed it
	async Task RotateAsync(VisualElement view, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		var rotations = AnimatedPropertyTracker.For(view, VisualElement.RotationProperty);
		var (rotationId, originalRotation) = rotations.Start(view.Rotation);

		try
		{
			var wasInterrupted = await view.RotateToAsync(Rotation, Length, Easing, token);

			if (!wasInterrupted && rotations.IsLatest(rotationId))
			{
				view.Rotation = originalRotation;
			}
		}
		finally
		{
			rotations.End(view.Rotation);
		}
	}

	static object CreateRotationDefaultValue(BindableObject bindable) => ((RotateAnimation)bindable).DefaultRotation;
}
