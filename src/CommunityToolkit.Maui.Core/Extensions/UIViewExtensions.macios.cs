namespace CommunityToolkit.Maui.Core.Extensions;

/// <summary>
/// Extensions for <see cref="UIView"/>
/// </summary>
public static class UIViewExtensions
{
	/// <summary>
	/// Safe bottom edge of the guide
	/// </summary>
	public static NSLayoutYAxisAnchor SafeBottomAnchor(this UIView view) =>
		UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
			? view.SafeAreaLayoutGuide.BottomAnchor
			: view.BottomAnchor;

	/// <summary>
	/// Safe horizontal center of the guide
	/// </summary>
	public static NSLayoutXAxisAnchor SafeCenterXAnchor(this UIView view) =>
		UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
			? view.SafeAreaLayoutGuide.CenterXAnchor
			: view.CenterXAnchor;

	/// <summary>
	/// Safe vertical center of the guide
	/// </summary>
	public static NSLayoutYAxisAnchor SafeCenterYAnchor(this UIView view) =>
		UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
			? view.SafeAreaLayoutGuide.CenterYAnchor
			: view.CenterYAnchor;

	/// <summary>
	/// Safe vertical extent of the guide
	/// </summary>
	public static NSLayoutDimension SafeHeightAnchor(this UIView view) =>
		UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
			? view.SafeAreaLayoutGuide.HeightAnchor
			: view.HeightAnchor;

	/// <summary>
	/// Safe leading edge of the guide
	/// </summary>
	public static NSLayoutXAxisAnchor SafeLeadingAnchor(this UIView view) =>
		UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
			? view.SafeAreaLayoutGuide.LeadingAnchor
			: view.LeadingAnchor;

	/// <summary>
	/// Safe left edge of the guide
	/// </summary>
	public static NSLayoutXAxisAnchor SafeLeftAnchor(this UIView view) =>
		UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
			? view.SafeAreaLayoutGuide.LeftAnchor
			: view.LeftAnchor;

	/// <summary>
	/// Safe right edge of the guide
	/// </summary>
	public static NSLayoutXAxisAnchor SafeRightAnchor(this UIView view) =>
		UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
			? view.SafeAreaLayoutGuide.RightAnchor
			: view.RightAnchor;

	/// <summary>
	/// Safe top edge of the guide
	/// </summary>
	public static NSLayoutYAxisAnchor SafeTopAnchor(this UIView view) =>
		UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
			? view.SafeAreaLayoutGuide.TopAnchor
			: view.TopAnchor;

	/// <summary>
	/// Safe trailing edge of the guide
	/// </summary>
	public static NSLayoutXAxisAnchor SafeTrailingAnchor(this UIView view) =>
		UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
			? view.SafeAreaLayoutGuide.TrailingAnchor
			: view.TrailingAnchor;

	/// <summary>
	/// Safe width edge of the guide
	/// </summary>
	public static NSLayoutDimension SafeWidthAnchor(this UIView view) =>
		UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
			? view.SafeAreaLayoutGuide.WidthAnchor
			: view.WidthAnchor;
}