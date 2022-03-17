using System.Runtime.InteropServices;
using CommunityToolkit.Maui.Core.Views;
using CoreGraphics;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.Alerts;

public partial class Snackbar
{
	/// <summary>
	/// Dismiss Snackbar
	/// </summary>
	private partial Task DismissNative(CancellationToken token)
	{
		if (NativeSnackbar is not null)
		{
			token.ThrowIfCancellationRequested();
			NativeSnackbar.Dismiss();
			NativeSnackbar = null;
		}

		return Task.CompletedTask;
	}

	/// <summary>
	/// Show Snackbar
	/// </summary>
	private partial async Task ShowNative(CancellationToken token)
	{
		await DismissNative(token);
		token.ThrowIfCancellationRequested();

		var cornerRadius = GetCornerRadius(VisualOptions.CornerRadius);

		var padding = GetMaximum(cornerRadius.X, cornerRadius.Y, cornerRadius.Width, cornerRadius.Height) + ToastView.DefaultPadding;
		NativeSnackbar = new SnackbarView(Text,
											VisualOptions.BackgroundColor.ToPlatform(),
											cornerRadius,
											VisualOptions.TextColor.ToPlatform(),
											UIFont.SystemFontOfSize((NFloat)VisualOptions.Font.Size),
											VisualOptions.CharacterSpacing,
											ActionButtonText,
											VisualOptions.ActionButtonTextColor.ToPlatform(),
											UIFont.SystemFontOfSize((NFloat)VisualOptions.ActionButtonFont.Size),
											padding)
		{
			Action = Action,
			Anchor = Anchor?.Handler?.PlatformView as UIView,
			Duration = Duration,
			OnDismissed = OnDismissed,
			OnShown = OnShown
		};

		NativeSnackbar.Show();

		static T? GetMaximum<T>(params T[] items) => items.Max();
	}

	static CGRect GetCornerRadius(CornerRadius cornerRadius)
	{
		return new CGRect(cornerRadius.BottomLeft, cornerRadius.TopLeft, cornerRadius.TopRight, cornerRadius.BottomRight);
	}
}