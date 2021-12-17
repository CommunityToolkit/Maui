using CoreGraphics;
using UIKit;

namespace CommunityToolkit.Maui.Core.Primitives;

/// <summary>
/// Visual Options for <see cref="CommunityToolkit.Maui.Core.Views.PopupView"/>
/// </summary>
public class PopupViewVisualOptions
{
	/// <summary>
	/// <see cref="CommunityToolkit.Maui.Core.Views.PopupView"/> Border Corner Radius
	/// </summary>
	public CGRect CornerRadius { get; set; }

	/// <summary>
	/// <see cref="CommunityToolkit.Maui.Core.Views.PopupView"/> Background Color
	/// </summary>
	public UIColor BackgroundColor { get; set; } = UIColor.Gray;
}