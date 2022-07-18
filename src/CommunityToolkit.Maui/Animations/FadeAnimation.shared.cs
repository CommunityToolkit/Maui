namespace CommunityToolkit.Maui.Animations;

class FadeAnimation : BaseAnimation
{
	public FadeAnimation() : base(300)
	{

	}

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
	/// Gets or sets the opacity to fade to before returning to the elements current Opacity.
	/// </summary>
	public double Opacity
	{
		get => (double)GetValue(OpacityProperty);
		set => SetValue(OpacityProperty, value);
	}

	public override async Task Animate(VisualElement view)
	{
		ArgumentNullException.ThrowIfNull(view);

		var originalOpacity = view.Opacity;

		await view.FadeTo(Opacity, Length, Easing);
		await view.FadeTo(originalOpacity, Length, Easing);
	}
}
