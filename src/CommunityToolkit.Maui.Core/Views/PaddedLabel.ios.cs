using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreGraphics;
using UIKit;

namespace CommunityToolkit.Maui.Core.Views;

class PaddedLabel : UILabel
{
	public PaddedLabel(double left, double top, double right, double bottom)
	{
		Left = (nfloat)left;
		Top = (nfloat)top;
		Right = (nfloat)right;
		Bottom = (nfloat)bottom;
	}

	public nfloat Left { get; }

	public nfloat Top { get; }

	public nfloat Right { get; }

	public nfloat Bottom { get; }

	public override CGSize IntrinsicContentSize => new(
		base.IntrinsicContentSize.Width + Left + Right,
		base.IntrinsicContentSize.Height + Top + Bottom);

	public override void DrawText(CGRect rect)
	{
		var insets = new UIEdgeInsets(Top, Left, Bottom, Right);
		base.DrawText(insets.InsetRect(rect));
	}
}