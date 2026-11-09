using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Extensions;
using CoreGraphics;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.Alerts;

public partial class Snackbar
{
	static PlatformSnackbar? PlatformSnackbar { get; set; }

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
		token.ThrowIfCancellationRequested();

		// Alert.Dismiss invokes OnDismissed, which forgets and disposes the platform snackbar (see OnPlatformSnackbarDismissed)
		PlatformSnackbar?.Dismiss();

		return Task.CompletedTask;
	}

	static CGRect GetCornerRadius(CornerRadius cornerRadius)
	{
		return new CGRect(cornerRadius.BottomLeft, cornerRadius.TopLeft, cornerRadius.TopRight, cornerRadius.BottomRight);
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
		var platformSnackbar = new PlatformSnackbar(Text,
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
			Anchor = Anchor is Page ? null : Anchor?.Handler?.PlatformView as UIView,
			Duration = Duration,
			OnShown = OnShown
		};
		platformSnackbar.OnDismissed = () => OnPlatformSnackbarDismissed(platformSnackbar);

		PlatformSnackbar = platformSnackbar;
		PlatformSnackbar.Show();

		static T? GetMaximum<T>(params IReadOnlyList<T> items) => items.Max();
	}

	// Forget the platform snackbar, so that a later Dismiss or Show does not dismiss it (and raise Dismissed) again, and dispose it, so that
	// the subscription of its action button no longer keeps it, its AlertView and this Snackbar alive.
	void OnPlatformSnackbarDismissed(PlatformSnackbar platformSnackbar)
	{
		// Alert.Dismiss invokes OnDismissed each time it runs: Dismissed is raised once per platform snackbar
		platformSnackbar.OnDismissed = null;

		if (PlatformSnackbar == platformSnackbar)
		{
			PlatformSnackbar = null;
		}

		platformSnackbar.Dispose();
		OnDismissed();
	}
}