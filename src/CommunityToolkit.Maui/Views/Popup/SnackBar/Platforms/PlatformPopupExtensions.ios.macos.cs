using System;
using CommunityToolkit.Maui.Extensions;
using CoreGraphics;
using Microsoft.Maui;
using UIKit;

namespace CommunityToolkit.Maui.Views.Popup.Snackbar.Platforms;

class PlatformPopupExtensions : IPlatformPopupExtensions
{
	public void Dismiss(Snackbar snackbar)
	{
		snackbar.NativeSnackbar?.Dismiss();
		snackbar.OnDismissed();
	}

	public AppleSnackbar Show(Snackbar snackbar)
	{
		nfloat defaultPadding = 10;
		var cornerRadius = GetCornerRadius(snackbar.VisualOptions.CornerRadius);
		var padding = LinqExtensions.Max(cornerRadius.X, cornerRadius.Y, cornerRadius.Width, cornerRadius.Height) + defaultPadding;
		var nativeSnackbar = new AppleSnackbar(padding)
		{
			Action = snackbar.Action,
			ActionButtonText = snackbar.ActionButtonText,
			ActionButtonFont = UIFont.SystemFontOfSize((float)snackbar.VisualOptions.ActionButtonFont.Size),
			Anchor = snackbar.Anchor?.Handler?.NativeView as UIView,
			Duration = snackbar.Duration,
			Message = snackbar.Text,
			ActionTextColor = snackbar.VisualOptions.ActionButtonTextColor.ToNative(),
			BackgroundColor = snackbar.VisualOptions.BackgroundColor.ToNative(),
			Font = UIFont.SystemFontOfSize((float)snackbar.VisualOptions.Font.Size),
			CharacterSpacing = snackbar.VisualOptions.CharacterSpacing,
			CornerRadius = cornerRadius,
			TextColor = snackbar.VisualOptions.TextColor.ToNative()
		};

		nativeSnackbar.Show();

		return nativeSnackbar;
	}

	static CGRect GetCornerRadius(CornerRadius cornerRadius)
	{
		return new CGRect(cornerRadius.BottomLeft, cornerRadius.TopLeft, cornerRadius.TopRight, cornerRadius.BottomRight);
	}
}