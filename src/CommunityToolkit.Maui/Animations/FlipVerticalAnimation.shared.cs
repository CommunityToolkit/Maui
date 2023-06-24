namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that will flip the supplied view vertically.
/// </summary>
public class FlipVerticalAnimation : RotateAnimation
{
	/// <summary>
	/// Initializes a new instance of <see cref="FlipVerticalAnimation"/>.
	/// </summary>
	public FlipVerticalAnimation() : base(300)
	{

	}

	/// <inheritdoc />
	protected override double DefaultRotation { get; set; } = 90;

	/// <inheritdoc />
	public override async Task Animate(VisualElement view)
	{
		ArgumentNullException.ThrowIfNull(view);

		await view.RotateXTo(Rotation, Length, Easing);
		await view.RotateXTo(0, Length, Easing);
	}
}
