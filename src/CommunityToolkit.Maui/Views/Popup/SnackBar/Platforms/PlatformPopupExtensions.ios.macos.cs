using System;
using Microsoft.Maui;
using CoreGraphics;
using UIKit;
using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.Views.Popup.SnackBar.Platforms;

class PlatformPopupExtensions : IPlatformPopupExtensions
{
	public void Dismiss(Snackbar snackbar)
	{
		snackbar.nativeSnackbar?.Dismiss();
		snackbar.OnDismissed();
	}

	public NativeSnackBar Show(Snackbar snackBar)
	{
		nfloat defaultPadding = 10;
		var cornerRadius = GetCornerRadius(snackBar.VisualOptions.CornerRadius);
		var padding = LinqExtensions.Max(cornerRadius.X, cornerRadius.Y, cornerRadius.Width, cornerRadius.Height) + defaultPadding;
		var nativeSnackBar = new NativeSnackBar(padding)
		{
			Action = snackBar.Action,
			ActionButtonText = snackBar.ActionButtonText,
			ActionButtonFont = UIFont.SystemFontOfSize((float)snackBar.VisualOptions.ActionButtonFont.Size),
			Anchor = snackBar.Anchor?.Handler?.NativeView as UIView,
			Duration = snackBar.Duration,
			Message = snackBar.Text,
			ActionTextColor = snackBar.VisualOptions.ActionButtonTextColor.ToNative(),
			BackgroundColor = snackBar.VisualOptions.BackgroundColor.ToNative(),
			Font = UIFont.SystemFontOfSize((float)snackBar.VisualOptions.Font.Size),
			CharacterSpacing = snackBar.VisualOptions.CharacterSpacing,
			CornerRadius = cornerRadius,
			TextColor = snackBar.VisualOptions.TextColor.ToNative()
		};

		nativeSnackBar.Show();

		return nativeSnackBar;
	}

	static CGRect GetCornerRadius(CornerRadius cornerRadius)
	{
		return new CGRect(cornerRadius.BottomLeft, cornerRadius.TopLeft, cornerRadius.TopRight, cornerRadius.BottomRight);
	}
}
