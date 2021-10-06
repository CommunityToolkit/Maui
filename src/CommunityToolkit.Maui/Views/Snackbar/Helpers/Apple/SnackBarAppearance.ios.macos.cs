using Microsoft.Maui;
using Microsoft.Maui.Controls.Compatibility.Platform.iOS;
using Microsoft.Maui.Graphics;
using UIKit;

namespace CommunityToolkit.Maui.UI.Views.Helpers.Apple
{
	class NativeSnackBarAppearance
	{
		public UIColor Background { get; set; } = UIColor.Gray;

		public UIColor Foreground { get; set; } = DefaultColor;

		public UIFont Font { get; set; } = DefaultFont;

		public UITextAlignment TextAlignment { get; set; } = UITextAlignment.Left;

		public static UIColor DefaultColor { get; } = UIColor.White;

		public static UIFont DefaultFont { get; } = Microsoft.Maui.Font.Default.ToUIFont();
	}

	static class NativeSnackButtonAppearance
	{
		public static UILineBreakMode LineBreakMode { get; set; } = UILineBreakMode.MiddleTruncation;

		public static UIColor DefaultColor { get; } = Colors.Black.ToUIColor();

		public static UIFont DefaultFont { get; } = Font.Default.ToUIFont();
	}
}