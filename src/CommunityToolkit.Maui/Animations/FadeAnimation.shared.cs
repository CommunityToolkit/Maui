namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that will fade the supplied view to the specified <see cref="Opacity"/>
/// and then back to it's original <see cref="Opacity"/>.
/// </summary>
public class FadeAnimation : BaseAnimation
{
	/// <summary>
	/// Backing BindableProperty for the <see cref="Opacity"/> property.
	/// </summary>
	public static readonly BindableProperty OpacityProperty =
		BindableProperty.Create(
			nameof(Opacity),
			typeof(double),
			typeof(FadeAnimation),
			0.3,
			BindingMode.TwoWay);

	/// <summary>
	/// Initializes a new instance of <see cref="FadeAnimation"/>.
	/// </summary>
	public FadeAnimation() : base(600)
	{

	}

	/// <summary>
	/// Gets or sets the opacity to fade to before returning to the elements current Opacity.
	/// </summary>
	public double Opacity
	{
		get => (double)GetValue(OpacityProperty);
		set => SetValue(OpacityProperty, value);
	}

	/// <inheritdoc />
	public override async Task Animate(VisualElement view)
	{
		ArgumentNullException.ThrowIfNull(view);

		var originalOpacity = view.Opacity;

		var duration = Length / 2;

		await view.FadeTo(Opacity, duration, Easing);
		await view.FadeTo(originalOpacity, duration, Easing);
	}
}