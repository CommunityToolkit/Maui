using UIKit;

namespace CommunityToolkit.Maui.UI.Views.Helpers.Apple.Extensions
{
	static class UIViewExtensions
	{
		public static NSLayoutYAxisAnchor SafeBottomAnchor(this UIView view) =>
			UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
				? view.SafeAreaLayoutGuide.BottomAnchor
				: view.BottomAnchor;

		public static NSLayoutXAxisAnchor SafeCenterXAnchor(this UIView view) =>
			UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
				? view.SafeAreaLayoutGuide.CenterXAnchor
				: view.CenterXAnchor;

		public static NSLayoutYAxisAnchor SafeCenterYAnchor(this UIView view) =>
			UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
				? view.SafeAreaLayoutGuide.CenterYAnchor
				: view.CenterYAnchor;

		public static NSLayoutDimension SafeHeightAnchor(this UIView view) =>
			UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
				? view.SafeAreaLayoutGuide.HeightAnchor
				: view.HeightAnchor;

		public static NSLayoutXAxisAnchor SafeLeadingAnchor(this UIView view) =>
			UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
				? view.SafeAreaLayoutGuide.LeadingAnchor
				: view.LeadingAnchor;

		public static NSLayoutXAxisAnchor SafeLeftAnchor(this UIView view) =>
			UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
				? view.SafeAreaLayoutGuide.LeftAnchor
				: view.LeftAnchor;

		public static NSLayoutXAxisAnchor SafeRightAnchor(this UIView view) =>
			UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
				? view.SafeAreaLayoutGuide.RightAnchor
				: view.RightAnchor;

		public static NSLayoutYAxisAnchor SafeTopAnchor(this UIView view) =>
			UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
				? view.SafeAreaLayoutGuide.TopAnchor
				: view.TopAnchor;

		public static NSLayoutXAxisAnchor SafeTrailingAnchor(this UIView view) =>
			UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
				? view.SafeAreaLayoutGuide.TrailingAnchor
				: view.TrailingAnchor;

		public static NSLayoutDimension SafeWidthAnchor(this UIView view) =>
			UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
				? view.SafeAreaLayoutGuide.WidthAnchor
				: view.WidthAnchor;
	}
}