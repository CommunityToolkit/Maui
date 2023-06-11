namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that will rotate the supplied view by the specified <see cref="Rotation"/>.
/// </summary>
public class RotateAnimation : BaseAnimation
{
	/// <summary>
	/// Backing BindableProperty for the <see cref="Rotation"/> property.
	/// </summary>
	public static readonly BindableProperty RotationProperty =
		BindableProperty.Create(
			nameof(Rotation),
			typeof(double),
			typeof(RotateAnimation),
			180.0,
			BindingMode.TwoWay,
			defaultValueCreator: GetDefaultRotationProperty);

	/// <summary>
	/// Gets or sets the rotation used by the animation.
	/// </summary>
	public double Rotation
	{
		get => (double)GetValue(RotationProperty);
		set => SetValue(RotationProperty, value);
	}

	static object GetDefaultRotationProperty(BindableObject bindable)
		=> ((RotateAnimation)bindable).DefaultRotation;

	/// <summary>
	/// Initializes a new instance of <see cref="RotateAnimation"/>.
	/// </summary>
	public RotateAnimation() : base(200)
	{
	}

	/// <summary>
	/// Initializes a new instance of <see cref="RotateAnimation"/>.
	/// </summary>
	/// <param name="defaultLength">The default length of the animation.</param>
	protected RotateAnimation(uint defaultLength) : base(defaultLength)
	{
	}

	/// <summary>
	/// Gets or sets the default rotation used by the animation.
	/// </summary>
	protected virtual double DefaultRotation { get; set; } = 180.0;

	/// <inheritdoc />
	public override async Task Animate(VisualElement view)
	{
		if (view != null)
		{
			await view.RotateTo(Rotation, Length, Easing);
			view.Rotation = 0;
		}
	}
}

