namespace CommunityToolkit.Maui.Animations;
/// <summary>
/// Abstract class for animation types to inherit.
/// </summary>
/// <typeparam name="TAnimatable">The <see cref="VisualElement"/> that the behavior can be applied to</typeparam>
public abstract class BaseAnimation<TAnimatable> : BindableObject where TAnimatable : IAnimatable
{
	readonly uint defaultLength;

	/// <summary>
	/// Backing BindableProperty for the <see cref="Length"/> property.
	/// </summary>
	public static readonly BindableProperty LengthProperty =
		BindableProperty.Create(nameof(Length), typeof(uint), typeof(BaseAnimation<TAnimatable>), 250u,
			BindingMode.OneWay, defaultValueCreator: bindable => ((BaseAnimation<TAnimatable>)bindable).defaultLength);

	/// <summary>
	/// Backing BindableProperty for the <see cref="Easing"/> property.
	/// </summary>
	public static readonly BindableProperty EasingProperty =
		BindableProperty.Create(nameof(Easing), typeof(Easing), typeof(BaseAnimation<TAnimatable>), Easing.Linear, BindingMode.OneWay);

	/// <summary>
	/// Initialize BaseAnimation
	/// </summary>
	/// <param name="defaultLength">The default time, in milliseconds, over which to animate the transition</param>
	protected BaseAnimation(uint defaultLength = 250u) => this.defaultLength = defaultLength;

	/// <summary>
	/// The time, in milliseconds, over which to animate the transition.
	/// </summary>
	public uint Length
	{
		get => (uint)GetValue(LengthProperty);
		set => SetValue(LengthProperty, value);
	}

	/// <summary>
	/// The easing function to use for the animation
	/// </summary>
	public Easing Easing
	{
		get => (Easing)GetValue(EasingProperty);
		set => SetValue(EasingProperty, value);
	}

	/// <summary>
	/// Performs the animation on the View
	/// </summary>
	/// <param name="view">The view to perform the animation on.</param>
	public abstract Task Animate(TAnimatable view);
}

/// <inheritdoc/>
public abstract class BaseAnimation : BaseAnimation<VisualElement>
{
	/// <summary>
	/// Initialize BaseAnimation
	/// </summary>
	/// <param name="defaultLength">The default time, in milliseconds, over which to animate the transition</param>
	protected BaseAnimation(uint defaultLength = 250u) : base(defaultLength)
	{

	}
}