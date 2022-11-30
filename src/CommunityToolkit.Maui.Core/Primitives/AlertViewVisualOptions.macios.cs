namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Visual Options for <see cref="CommunityToolkit.Maui.Core.Views.AlertView"/>
/// </summary>
public class AlertViewVisualOptions
{
	/// <summary>
	/// <see cref="CommunityToolkit.Maui.Core.Views.AlertView"/> Border Corner Radius
	/// </summary>
	public CGRect CornerRadius { get; set; }

	/// <summary>
	/// <see cref="CommunityToolkit.Maui.Core.Views.AlertView"/> Background Color
	/// </summary>
	public UIColor BackgroundColor { get; set; } = UIColor.Gray;
}