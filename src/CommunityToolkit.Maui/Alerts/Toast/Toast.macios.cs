using System.Runtime.InteropServices;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CoreGraphics;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.Alerts;

public partial class Toast
{
	private partial void DismissNative(CancellationToken token)
	{
		if (NativeToast is not null)
		{
			token.ThrowIfCancellationRequested();

			NativeToast.Dismiss();
		}
	}

	/// <summary>
	/// Show Toast
	/// </summary>
	private partial void ShowNative(CancellationToken token)
	{
		DismissNative(token);
		token.ThrowIfCancellationRequested();

		var cornerRadius = CreateCornerRadius();
		var padding = GetMaximum(cornerRadius.X, cornerRadius.Y, cornerRadius.Width, cornerRadius.Height) + ToastView.DefaultPadding;

		NativeToast = new ToastView(Text,
											Defaults.BackgroundColor.ToPlatform(),
											cornerRadius,
											Defaults.TextColor.ToPlatform(),
											UIFont.SystemFontOfSize((NFloat)TextSize),
											Defaults.CharacterSpacing,
											padding)
		{
			Duration = GetDuration(Duration)
		};

		NativeToast.Show();

		static T? GetMaximum<T>(params T[] items) => items.Max();
	}

	static CGRect CreateCornerRadius(int radius = 4)
	{
		return new CGRect(radius, radius, radius, radius);
	}
}