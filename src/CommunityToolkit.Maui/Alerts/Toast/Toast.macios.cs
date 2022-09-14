using System.Runtime.InteropServices;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CoreGraphics;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.Alerts;

public partial class Toast
{
	static CommunityToolkit.Maui.Core.Views.PlatformToast? PlatformToast { get; set; }

	/// <summary>
	/// Dispose Toast
	/// </summary>
	protected virtual void Dispose(bool isDisposing)
	{
		if (isDisposed)
		{
			return;
		}

		if (isDisposing)
		{
			PlatformToast?.Dispose();
		}

		isDisposed = true;
	}

	static void DismissPlatform(CancellationToken token)
	{
		if (PlatformToast is not null)
		{
			token.ThrowIfCancellationRequested();

			PlatformToast.Dismiss();
		}
	}

	/// <summary>
	/// Show Toast
	/// </summary>
	void ShowPlatform(CancellationToken token)
	{
		DismissPlatform(token);
		token.ThrowIfCancellationRequested();

		var cornerRadius = CreateCornerRadius();
		var padding = GetMaximum(cornerRadius.X, cornerRadius.Y, cornerRadius.Width, cornerRadius.Height);

		PlatformToast = new PlatformToast(Text,
											AlertDefaults.BackgroundColor.ToPlatform(),
											cornerRadius,
											AlertDefaults.TextColor.ToPlatform(),
											UIFont.SystemFontOfSize((NFloat)TextSize),
											AlertDefaults.CharacterSpacing,
											padding)
		{
			Duration = GetDuration(Duration)
		};

		PlatformToast.Show();

		static T? GetMaximum<T>(params T[] items) => items.Max();
	}

	static CGRect CreateCornerRadius(int radius = 4)
	{
		return new CGRect(radius, radius, radius, radius);
	}
}