using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that will flip the supplied view horizontally.
/// </summary>
public class FlipHorizontalAnimation : RotateAnimation
{
	/// <summary>
	/// Initializes a new instance of <see cref="FlipHorizontalAnimation"/>.
	/// </summary>
	public FlipHorizontalAnimation() : base(600)
	{

	}

	/// <inheritdoc />
	protected override double DefaultRotation { get; set; } = 90;

	/// <inheritdoc />
	public override async Task Animate(VisualElement view, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(view);

		var duration = Length / 2;

		await view.RotateYToAsync(Rotation, duration, Easing, token);
		await view.RotateYToAsync(0, duration, Easing, token);
	}
}
