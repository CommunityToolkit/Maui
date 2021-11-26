using System;
using CoreGraphics;
using UIKit;

namespace CommunityToolkit.Maui.Views.Popup.SnackBar;

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

    public override CGSize IntrinsicContentSize => new CGSize(
        base.IntrinsicContentSize.Width + Left + Right,
        base.IntrinsicContentSize.Height + Top + Bottom);

    public override void DrawText(CGRect rect)
    {
        var insets = new UIEdgeInsets(Top, Left, Bottom, Right);
        base.DrawText(insets.InsetRect(rect));
    }
}
