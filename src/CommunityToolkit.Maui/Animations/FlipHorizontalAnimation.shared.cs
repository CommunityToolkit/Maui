namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that will flip the supplied view horizontally.
/// </summary>
public class FlipHorizontalAnimation : RotateAnimation
{
	/// <summary>
	/// Initializes a new instance of <see cref="FlipHorizontalAnimation"/>.
	/// </summary>
	public FlipHorizontalAnimation() : base(300)
	{

	}

	/// <inheritdoc />
	protected override double DefaultRotation { get; set; } = 90;

	/// <inheritdoc />
	public override async Task Animate(VisualElement view)
	{
		ArgumentNullException.ThrowIfNull(view);

		await view.RotateYTo(Rotation, Length, Easing);
		await view.RotateYTo(0, Length, Easing);
	}
}
