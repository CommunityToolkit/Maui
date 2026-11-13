using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that will flip the supplied view vertically.
/// </summary>
/// <remarks>
/// The view rotates around its x-axis to <see cref="RotateAnimation.Rotation"/> and then back to the x-axis rotation it had before it started flipping.
/// Cancelling the token while the animation runs returns the view to that rotation and ends the returned <see cref="Task"/> as canceled.
/// </remarks>
public partial class FlipVerticalAnimation() : RotateAnimation(FlipVerticalAnimationDefaults.Length)
{
	/// <inheritdoc />
	protected override double DefaultRotation => FlipVerticalAnimationDefaults.Rotation;

	/// <inheritdoc />
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="view"/> is <see langword="null"/>.</exception>
	/// <exception cref="OperationCanceledException">The returned <see cref="Task"/> ends canceled when <paramref name="token"/> is cancelled; when that happens while the animation runs, the view returns to its original x-axis rotation first.</exception>
	public override Task Animate(VisualElement view, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(view);

		return FlipAsync(view, token);
	}

	// Animate validates its arguments before it calls this method: in an async method, a null argument would only fault the returned Task instead of throwing at the call that passed it
	async Task FlipAsync(VisualElement view, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		var rotations = AnimatedPropertyTracker.For(view, VisualElement.RotationXProperty);
		var (flipId, originalRotationX) = rotations.Start(view.RotationX);

		var halfLength = Length / 2;

		try
		{
			var wasInterrupted = await view.RotateXToAsync(Rotation, halfLength, Easing, token);

			if (!wasInterrupted && rotations.IsLatest(flipId))
			{
				wasInterrupted = await view.RotateXToAsync(originalRotationX, halfLength, Easing, token);
			}

			if (wasInterrupted && token.IsCancellationRequested)
			{
				// A cancelled animation has no successor to return the view to its original rotation
				if (rotations.IsLatest(flipId))
				{
					view.RotationX = originalRotationX;
				}

				token.ThrowIfCancellationRequested();
			}
		}
		finally
		{
			rotations.End(view.RotationX);
		}
	}
}
