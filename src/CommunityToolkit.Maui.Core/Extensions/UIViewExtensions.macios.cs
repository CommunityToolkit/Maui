namespace CommunityToolkit.Maui.Core.Extensions;

/// <summary>
/// Extensions for <see cref="UIView"/>
/// </summary>
public static class UIViewExtensions
{
	/// <summary>
	/// Safe bottom edge of the guide
	/// </summary>
	public static NSLayoutYAxisAnchor SafeBottomAnchor(this UIView view) => view.SafeAreaLayoutGuide.BottomAnchor;

	/// <summary>
	/// Safe horizontal center of the guide
	/// </summary>
	public static NSLayoutXAxisAnchor SafeCenterXAnchor(this UIView view) => view.SafeAreaLayoutGuide.CenterXAnchor;

	/// <summary>
	/// Safe vertical center of the guide
	/// </summary>
	public static NSLayoutYAxisAnchor SafeCenterYAnchor(this UIView view) => view.SafeAreaLayoutGuide.CenterYAnchor;

	/// <summary>
	/// Safe vertical extent of the guide
	/// </summary>
	public static NSLayoutDimension SafeHeightAnchor(this UIView view) => view.SafeAreaLayoutGuide.HeightAnchor;

	/// <summary>
	/// Safe leading edge of the guide
	/// </summary>
	public static NSLayoutXAxisAnchor SafeLeadingAnchor(this UIView view) => view.SafeAreaLayoutGuide.LeadingAnchor;

	/// <summary>
	/// Safe left edge of the guide
	/// </summary>
	public static NSLayoutXAxisAnchor SafeLeftAnchor(this UIView view) => view.SafeAreaLayoutGuide.LeftAnchor;

	/// <summary>
	/// Safe right edge of the guide
	/// </summary>
	public static NSLayoutXAxisAnchor SafeRightAnchor(this UIView view) => view.SafeAreaLayoutGuide.RightAnchor;

	/// <summary>
	/// Safe top edge of the guide
	/// </summary>
	public static NSLayoutYAxisAnchor SafeTopAnchor(this UIView view) => view.SafeAreaLayoutGuide.TopAnchor;

	/// <summary>
	/// Safe trailing edge of the guide
	/// </summary>
	public static NSLayoutXAxisAnchor SafeTrailingAnchor(this UIView view) => view.SafeAreaLayoutGuide.TrailingAnchor;

	/// <summary>
	/// Safe width edge of the guide
	/// </summary>
	public static NSLayoutDimension SafeWidthAnchor(this UIView view) => view.SafeAreaLayoutGuide.WidthAnchor;
}