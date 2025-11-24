namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that will fade the supplied view to the specified <see cref="Opacity"/>
/// and then back to its original <see cref="Opacity"/>.
/// </summary>
public partial class FadeAnimation() : BaseAnimation(300)
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
	/// Gets or sets the opacity to fade to before returning to the elements current Opacity.
	/// </summary>
	public double Opacity
	{
		get => (double)GetValue(OpacityProperty);
		set => SetValue(OpacityProperty, value);
	}

	/// <inheritdoc />
	public override async Task Animate(VisualElement view, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(view);
		token.ThrowIfCancellationRequested();

		var originalOpacity = view.Opacity;

		await view.FadeToAsync(Opacity, Length, Easing).WaitAsync(token);
		await view.FadeToAsync(originalOpacity, Length, Easing).WaitAsync(token);
	}
}