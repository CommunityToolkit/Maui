using CommunityToolkit.Maui.Core.Views;
using CoreGraphics;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.Alerts;

public partial class Snackbar
{
	readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

	static SnackbarView? _nativeSnackbar;

	/// <summary>
	/// Dismiss Snackbar
	/// </summary>
	public virtual async partial Task Dismiss(CancellationToken token)
	{
		if (_nativeSnackbar is null)
			return;

		await _semaphoreSlim.WaitAsync(token);

		try
		{
			token.ThrowIfCancellationRequested();
			_nativeSnackbar.Dismiss();
			_nativeSnackbar = null;
		}
		finally
		{
			_semaphoreSlim.Release();
		}
	}

	/// <summary>
	/// Show Snackbar
	/// </summary>
	public virtual async partial Task Show(CancellationToken token)
	{
		await Dismiss(token);
		token.ThrowIfCancellationRequested();

		var cornerRadius = GetCornerRadius(VisualOptions.CornerRadius);
		var padding = GetMaximum(cornerRadius.X, cornerRadius.Y, cornerRadius.Width, cornerRadius.Height) + ToastView.DefaultPadding;
		_nativeSnackbar = new SnackbarView(Text,
											VisualOptions.BackgroundColor.ToNative(),
											cornerRadius,
											VisualOptions.TextColor.ToNative(),
											UIFont.SystemFontOfSize((float)VisualOptions.Font.Size),
											VisualOptions.CharacterSpacing,
											ActionButtonText,
											VisualOptions.ActionButtonTextColor.ToNative(),
											UIFont.SystemFontOfSize((float)VisualOptions.ActionButtonFont.Size),
											padding)
		{
			Action = Action,
			Anchor = Anchor?.Handler?.NativeView as UIView,
			Duration = Duration,
			OnDismissed = OnDismissed,
			OnShown = OnShown
		};

		_nativeSnackbar.Show();

		static T? GetMaximum<T>(params T[] items) => items.Max();
	}

	static CGRect GetCornerRadius(CornerRadius cornerRadius)
	{
		return new CGRect(cornerRadius.BottomLeft, cornerRadius.TopLeft, cornerRadius.TopRight, cornerRadius.BottomRight);
	}
}