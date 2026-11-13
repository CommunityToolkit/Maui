using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that will flip the supplied view horizontally.
/// </summary>
/// <remarks>
/// The view rotates around its y-axis to <see cref="RotateAnimation.Rotation"/> and then back to the y-axis rotation it had before it started flipping.
/// The animation plays over <see cref="BaseAnimation{TAnimatable}.Length"/>: half of it to reach <see cref="RotateAnimation.Rotation"/>, and half to return to the original rotation.
/// Cancelling the token while the animation runs returns the view to that rotation and ends the returned <see cref="Task"/> as canceled.
/// With the system's animations off (for example iOS Reduce Motion or Android's "Remove animations"), the view stays at its original y-axis rotation.
/// </remarks>
public partial class FlipHorizontalAnimation() : RotateAnimation(FlipHorizontalAnimationDefaults.Length)
{
	/// <inheritdoc />
	protected override double DefaultRotation => FlipHorizontalAnimationDefaults.Rotation;

	/// <inheritdoc />
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="view"/> is <see langword="null"/>.</exception>
	/// <exception cref="OperationCanceledException">The returned <see cref="Task"/> ends canceled when <paramref name="token"/> is cancelled; when that happens while the animation runs, the view returns to its original y-axis rotation first.</exception>
	public override Task Animate(VisualElement view, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(view);

		return AnimateOnDispatcherAsync(view, () => FlipAsync(view, token));
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
			var wasInterrupted = await WasInterruptedAsync(() => view.RotateYToAsync(Rotation, halfLength, Easing, token), token);

			if (!wasInterrupted && rotations.IsLatest(flipId))
			{
				wasInterrupted = await WasInterruptedAsync(() => view.RotateYToAsync(originalRotationY, halfLength, Easing, token), token);
			}

			if (wasInterrupted && token.IsCancellationRequested)
			{
				// A cancelled animation has no successor to return the view to its original rotation
				if (rotations.IsLatest(flipId))
				{
					view.RotationY = originalRotationY;
				}

				token.ThrowIfCancellationRequested();
			}
		}
		finally
		{
			rotations.End(view.RotationY);
		}
	}
}
