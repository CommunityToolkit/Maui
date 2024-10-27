namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// When to apply the status bar color and style.
/// </summary>
public enum IconTintColorApplyOn
{
	/// <summary>
	/// Apply color when the behavior has been attached to the view.
	/// </summary>
	OnBehaviorAttachedTo,

	/// <summary>
	/// Apply color when the view has been loaded.
	/// </summary>
	OnViewLoaded
}

/// <summary>
/// A behavior that allows you to tint an icon with a specified <see cref="Color"/>.
/// </summary>
public partial class IconTintColorBehavior : BasePlatformBehavior<View>
{
	/// <summary>
	/// <see cref="BindableProperty"/> that manages the ApplyOn property.
	/// </summary>
	public static readonly BindableProperty ApplyOnProperty =
		BindableProperty.Create(nameof(ApplyOn), typeof(IconTintColorApplyOn), typeof(IconTintColorBehavior), IconTintColorApplyOn.OnBehaviorAttachedTo);
	
	/// <summary>
	/// Attached Bindable Property for the <see cref="TintColor"/>.
	/// </summary>
	public static readonly BindableProperty TintColorProperty =
		BindableProperty.Create(nameof(TintColor), typeof(Color), typeof(IconTintColorBehavior), default);

	/// <summary>
	/// When the status bar color should be applied.
	/// </summary>
	public IconTintColorApplyOn ApplyOn
	{
		get => (IconTintColorApplyOn)GetValue(ApplyOnProperty);
		set => SetValue(ApplyOnProperty, value);
	}
	
	/// <summary>
	/// Property that represents the <see cref="Color"/> that Icon will be tinted.
	/// </summary>
	public Color? TintColor
	{
		get => (Color?)GetValue(TintColorProperty);
		set => SetValue(TintColorProperty, value);
	}
}