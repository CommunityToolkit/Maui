using System;
using CoreAnimation;
using CoreGraphics;
using UIKit;

namespace CommunityToolkit.Maui.Views.Popup;

class NativeRoundedStackView : UIStackView
{
	public nfloat Left { get; }

	public nfloat Top { get; }

	public nfloat Right { get; }

	public nfloat Bottom { get; }

	public NativeRoundedStackView(nfloat left, nfloat top, nfloat right, nfloat bottom)
	{
		Left = left;
		Top = top;
		Right = right;
		Bottom = bottom;
	}

	public override void Draw(CGRect rect)
	{
		ClipsToBounds = true;
		var path = GetRoundedPath(rect, Left, Top, Right, Bottom);
		var maskLayer = new CAShapeLayer { Frame = rect, Path = path };
		Layer!.Mask = maskLayer;
		Layer.MasksToBounds = true;
	}

	CGPath? GetRoundedPath(CGRect rect, nfloat left, nfloat top, nfloat right, nfloat bottom)
	{
		var path = new UIBezierPath();
		path.MoveTo(new CGPoint(rect.Width - right, rect.Y));

		path.AddArc(new CGPoint((float)rect.X + rect.Width - right, (float)rect.Y + right), (nfloat)right, (float)(Math.PI * 1.5), (float)Math.PI * 2, true);
		path.AddLineTo(new CGPoint(rect.Width, rect.Height - bottom));

		path.AddArc(new CGPoint((float)rect.X + rect.Width - bottom, (float)rect.Y + rect.Height - bottom), (nfloat)bottom, 0, (float)(Math.PI * .5), true);
		path.AddLineTo(new CGPoint(left, rect.Height));

		path.AddArc(new CGPoint((float)rect.X + left, (float)rect.Y + rect.Height - left), (nfloat)left, (float)(Math.PI * .5), (float)Math.PI, true);
		path.AddLineTo(new CGPoint(rect.X, top));

		path.AddArc(new CGPoint((float)rect.X + top, (float)rect.Y + top), (nfloat)top, (float)Math.PI, (float)(Math.PI * 1.5), true);

		path.ClosePath();

		return path.CGPath;
	}
}
