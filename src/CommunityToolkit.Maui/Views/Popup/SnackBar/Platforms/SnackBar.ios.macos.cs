using System;
using Microsoft.Maui;
using CoreGraphics;
using UIKit;

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
			Action = snackBar.Action,
			ActionButtonText = snackBar.ActionButtonText,
			ActionButtonFont = UIFont.SystemFontOfSize((float)snackBar.VisualOptions.ActionButtonFont.Size),
			Anchor = snackBar.Anchor?.Handler?.NativeView as UIView,
			Duration = snackBar.Duration,
			Message = snackBar.Text,
			ActionTextColor = snackBar.VisualOptions.ActionButtonTextColor.ToNative(),
			BackgroundColor = snackBar.VisualOptions.BackgroundColor.ToNative(),
			CharacterSpacing = snackBar.VisualOptions.CharacterSpacing,
			CornerRadius = GetCornerRadius(snackBar.VisualOptions.CornerRadius),
			Font = UIFont.SystemFontOfSize((float)snackBar.VisualOptions.Font.Size),
			TextColor = snackBar.VisualOptions.TextColor.ToNative()
		};

		nativeSnackBar.Show();

		return nativeSnackBar;
	}

	static CGRect GetCornerRadius(CornerRadius cornerRadius)
	{
		return new CGRect((nfloat)cornerRadius.BottomLeft, (nfloat)cornerRadius.TopLeft, (nfloat)cornerRadius.TopRight, (nfloat)cornerRadius.BottomRight);
	}
}
