namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// A behavior that allows you to tint an icon with a specified <see cref="Color"/>.
/// </summary>
public partial class IconTintColorBehavior : BasePlatformBehavior<View>
{
	/// <summary>
	/// Property that represents the <see cref="Color"/> that Icon will be tinted.
	/// </summary>
	[BindableProperty]
	public partial Color? TintColor { get; set; }
}