#if IOS || MACCATALYST
using System;
using CoreGraphics;
using UIKit;

namespace CommunityToolkit.Maui.UI.Views.Helpers
{
	class PaddedLabel : UILabel
	{
		public PaddedLabel(nfloat left, nfloat top, nfloat right, nfloat bottom)
		{
			Left = left;
			Top = top;
			Right = right;
			Bottom = bottom;
		}

		public nfloat Left { get; }

		public nfloat Top { get; }

		public nfloat Right { get; }

		public nfloat Bottom { get; }

		public override CGSize IntrinsicContentSize => new CGSize(
			base.IntrinsicContentSize.Width + Left + Right,
			base.IntrinsicContentSize.Height + Top + Bottom);

		public override void DrawText(CGRect rect)
		{
			var insets = new UIEdgeInsets(Top, Left, Bottom, Right);
			base.DrawText(insets.InsetRect(rect));
		}
	}
}
#endif