namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that will shake the supplied view on the x-axis, starting with the <see cref="StartFactor"/>
/// and then reducing each time by the <see cref="ReducingAmount"/>.
/// </summary>
public class ShakeAnimation : BaseAnimation
{
	/// <summary>
	/// Backing BindableProperty for the <see cref="StartFactor"/> property.
	/// </summary>
	public static readonly BindableProperty StartFactorProperty =
		BindableProperty.Create(
			nameof(StartFactor),
			typeof(double),
			typeof(ShakeAnimation),
			15.0,
			BindingMode.TwoWay);

	/// <summary>
	/// Gets or sets the start factor, this is the biggest movement during the shake.
	/// </summary>
	public double StartFactor
	{
		get => (double)GetValue(StartFactorProperty);
		set => SetValue(StartFactorProperty, value);
	}

	/// <summary>
	/// Backing BindableProperty for the <see cref="ReducingAmount"/> property.
	/// </summary>
	public static readonly BindableProperty ReducingAmountProperty =
		BindableProperty.Create(
			nameof(ReducingAmount),
			typeof(double),
			typeof(ShakeAnimation),
			5.0,
			BindingMode.TwoWay);

	/// <summary>
	/// Gets or sets the amount to reduce the <see cref="StartFactor"/> by on each return to 0 on the x-axis.
	/// </summary>
	public double ReducingAmount
	{
		get => (double)GetValue(ReducingAmountProperty);
		set => SetValue(ReducingAmountProperty, value);
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ShakeAnimation"/>.
	/// </summary>
	public ShakeAnimation() : base(300)
	{

	}

	/// <inheritdoc />
	public override async Task Animate(VisualElement view)
	{
		ArgumentNullException.ThrowIfNull(view);

		var duration = (uint)(Length / Math.Ceiling(StartFactor / ReducingAmount)) / 2;
	
		for (var i = StartFactor; i > 0; i -= ReducingAmount)
		{
			await view.TranslateTo(-i, 0, duration, Easing);
			await view.TranslateTo(i, 0, duration, Easing);
		}

		view.TranslationX = 0;
	}
}

