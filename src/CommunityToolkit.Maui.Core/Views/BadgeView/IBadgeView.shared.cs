using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Core.Views.BadgeView;

/// <summary>
/// The <see cref="IBadgeView"/> allows the user to show a badge with a string value on top of any control. By wrapping a control in a <see cref="IBadgeView"/> control, you can show a badge value on top of it. This is very much like the badges you see on the app icons on iOS and Android.
/// </summary>
public interface IBadgeView : IView
{
	/// <summary>
	/// Gets or sets the <see cref="View"/> on top of which the <see cref="BadgeView"/> will be shown. This is a bindable property.
	/// </summary>
	View? Content { get; }

	/// <summary>
	/// Determines the position where the badge will be shown on top of <see cref="Content"/>. This is a bindable property.
	/// </summary>
	BadgePosition BadgePosition { get; }

	/// <summary>
	/// Determines whether or not the badge is automatically hidden when the <see cref="Text"/> is set to 0. This is a bindable property.
	/// </summary>
	bool AutoHide { get; }

	/// <summary>
	/// Determines if an animation is used when the badge is shown or hidden. This is a bindable property.
	/// </summary>
	bool IsAnimated { get; }

	/// <summary>
	/// Gets or sets the animation that is used when the badge is shown or hidden. The animation only shows when <see cref="IsAnimated"/> is set to true. This is a bindable property.
	/// </summary>
	IBadgeAnimation? BadgeAnimation { get; }

	/// <summary>
	/// Gets or sets the background <see cref="Color"/> of the badge. This is a bindable property.
	/// </summary>
	new Color BackgroundColor { get; }

	/// <summary>
	/// Gets or sets the border <see cref="Color"/> of the badge. This is a bindable property.
	/// </summary>
	Color BorderColor { get; }

	/// <summary>
	/// Enabled or disables a shadow being shown behind the <see cref="BadgeView"/>. This is a bindable property.
	/// </summary>
	bool HasShadow { get; }

	/// <summary>
	/// Gets or sets the <see cref="Color"/> or the test shown in  the <see cref="BadgeView"/>. This is a bindable property.
	/// </summary>
	Color TextColor { get; }

	/// <summary>
	/// Text that is shown on the <see cref="BadgeView"/>. Set this property to 0 and <see cref="AutoHide"/> to true, to make the badge disappear automatically. This is a bindable property.
	/// </summary>
	string Text { get; }

	/// <summary>
	/// Font size of all the text on the <see cref="BadgeView" />. <see cref="NamedSize" /> values can be used. This is a bindable property.
	/// </summary>
	double FontSize { get; }

	/// <summary>
	/// Font of the text on the <see cref="BadgeView" />. This is a bindable property.
	/// </summary>
	string FontFamily { get; }

	/// <summary>
	/// Font attributes of all the text on the <see cref="BadgeView" />. This is a bindable property.
	/// </summary>
	FontAttributes FontAttributes { get; }
}
