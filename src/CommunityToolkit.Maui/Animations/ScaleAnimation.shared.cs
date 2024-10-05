namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that will scale the supplied view to the specified <see cref="Scale"/> and then back down to its original scale.
/// </summary>
public class ScaleAnimation : BaseAnimation
{
	/// <summary>
	/// Backing BindableProperty for the <see cref="Scale"/> property.
	/// </summary>
	public static readonly BindableProperty ScaleProperty =
		BindableProperty.Create(
			nameof(Scale),
			typeof(double),
			typeof(ScaleAnimation),
			1.2,
			BindingMode.TwoWay);

	/// <summary>
	/// Gets or sets the opacity to fade to before returning to the elements current Scale.
	/// </summary>
	public double Scale
	{
		get => (double)GetValue(ScaleProperty);
		set => SetValue(ScaleProperty, value);
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ScaleAnimation"/>.
	/// </summary>
	public ScaleAnimation() : base(340)
	{

	}

	/// <inheritdoc />
	public override async Task Animate(VisualElement view)
	{
		ArgumentNullException.ThrowIfNull(view);

		var originalScale = view.Scale;

		var duration = Length / 2;

		await view.ScaleTo(Scale, duration, Easing);
		await view.ScaleTo(originalScale, duration, Easing);
	}
}
