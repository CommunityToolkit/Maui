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
	public override async Task Animate(VisualElement view)
	{
		ArgumentNullException.ThrowIfNull(view);

		var duration = Length / 2;

		await view.RotateYTo(Rotation, duration, Easing);
		await view.RotateYTo(0, duration, Easing);
	}
}
