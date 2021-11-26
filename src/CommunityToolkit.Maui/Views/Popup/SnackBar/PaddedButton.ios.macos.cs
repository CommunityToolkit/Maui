using System;
using UIKit;

namespace CommunityToolkit.Maui.Views.Popup.SnackBar;

class PaddedButton : UIButton
{
    public PaddedButton(double left, double top, double right, double bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
        ContentEdgeInsets = new UIEdgeInsets((nfloat)top, (nfloat)left, (nfloat)bottom, (nfloat)right);
    }

    public double Left { get; }

    public double Top { get; }

    public double Right { get; }

    public double Bottom { get; }
}