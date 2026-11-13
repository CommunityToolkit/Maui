using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that will flip the supplied view horizontally.
/// </summary>
/// <remarks>
/// The view rotates around its y-axis to <see cref="RotateAnimation.Rotation"/> and then back to the y-axis rotation it had before it started flipping.
/// </remarks>
public class FlipHorizontalAnimation() : RotateAnimation(FlipHorizontalAnimationDefaults.Length)
{
	/// <inheritdoc />
	protected override double DefaultRotation { get; set; } = FlipHorizontalAnimationDefaults.Rotation;

	/// <inheritdoc />
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="view"/> is <see langword="null"/>.</exception>
	public override Task Animate(VisualElement view, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(view);

		return FlipAsync(view, token);
	}

	// Animate validates its arguments before it calls this method: in an async method, a null argument would only fault the returned Task instead of throwing at the call that passed it
	async Task FlipAsync(VisualElement view, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		var rotations = AnimatedPropertyTracker.For(view, VisualElement.RotationYProperty);
		var (flipId, originalRotationY) = rotations.Start(view.RotationY);

		var halfLength = Length / 2;

		try
		{
			var wasInterrupted = await view.RotateYToAsync(Rotation, halfLength, Easing, token);

			if (!wasInterrupted && rotations.IsLatest(flipId))
			{
				await view.RotateYToAsync(originalRotationY, halfLength, Easing, token);
			}
		}
		finally
		{
			rotations.End(view.RotationY);
		}
	}
}
