using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Extensions;
using CoreGraphics;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.Alerts;

public partial class Snackbar
{
	static CommunityToolkit.Maui.Core.Views.PlatformSnackbar? PlatformSnackbar { get; set; }

	/// <summary>
	/// Dispose Snackbar
	/// </summary>
	protected virtual void Dispose(bool isDisposing)
	{
		if (isDisposed)
		{
			return;
		}

		if (isDisposing)
		{
			PlatformSnackbar?.Dispose();
		}

		isDisposed = true;
	}

	/// <summary>
	/// Dismiss Snackbar
	/// </summary>
	static Task DismissPlatform(CancellationToken token)
	{
		if (PlatformSnackbar is not null)
		{
			token.ThrowIfCancellationRequested();
			PlatformSnackbar.Dismiss();
			PlatformSnackbar = null;
		}

		return Task.CompletedTask;
	}

	/// <summary>
	/// Show Snackbar
	/// </summary>
	async Task ShowPlatform(CancellationToken token)
	{
		await DismissPlatform(token);
		token.ThrowIfCancellationRequested();

		var cornerRadius = GetCornerRadius(VisualOptions.CornerRadius);

		var padding = GetMaximum(cornerRadius.X, cornerRadius.Y, cornerRadius.Width, cornerRadius.Height);
		PlatformSnackbar = new PlatformSnackbar(Text,
											VisualOptions.BackgroundColor.ToPlatform(),
											cornerRadius,
											VisualOptions.TextColor.ToPlatform(),
											VisualOptions.Font.ToUIFont(),
											VisualOptions.CharacterSpacing,
											ActionButtonText,
											VisualOptions.ActionButtonTextColor.ToPlatform(),
											VisualOptions.ActionButtonFont.ToUIFont(),
											padding)
		{
			Action = Action,
			Anchor = Anchor?.Handler?.PlatformView as UIView,
			Duration = Duration,
			OnDismissed = OnDismissed,
			OnShown = OnShown
		};

		PlatformSnackbar.Show();

		static T? GetMaximum<T>(params T[] items) => items.Max();
	}

	static CGRect GetCornerRadius(CornerRadius cornerRadius)
	{
		return new CGRect(cornerRadius.BottomLeft, cornerRadius.TopLeft, cornerRadius.TopRight, cornerRadius.BottomRight);
	}
}