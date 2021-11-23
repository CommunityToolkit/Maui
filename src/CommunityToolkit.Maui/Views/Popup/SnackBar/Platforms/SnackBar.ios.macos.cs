using System;
using Microsoft.Maui;
using CoreGraphics;

namespace CommunityToolkit.Maui.Views.Popup.SnackBar.Platforms;

static partial class PlatformPopupExtensions
{
	public static void Dismiss(NativeSnackBar? snackbar)
	{
		snackbar?.Dismiss();
	}

	public static NativeSnackBar Show(ISnackbar snackBar)
	{
		var nativeSnackBar = new NativeSnackBar()
		{
			Message = snackBar.Text,
			//Anchor = snackBar.Anchor,
			CornerRadius = GetCornerRadius(snackBar.VisualOptions.CornerRadius),
			Action = snackBar.Action,
			Duration = snackBar.Duration
		};

		snackBar.Show();

		return nativeSnackBar;
	}

	static CGRect GetCornerRadius(CornerRadius cornerRadius)
	{
		return new CGRect((nfloat)cornerRadius.BottomLeft, (nfloat)cornerRadius.TopLeft, (nfloat)cornerRadius.TopRight, (nfloat)cornerRadius.BottomRight);
	}
}
