using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CoreGraphics;
using Microsoft.Maui.Platform;
using ObjCRuntime;
using UIKit;

namespace CommunityToolkit.Maui.Alerts;

public partial class Toast
{
	static ToastView? nativeToast;
	static readonly SemaphoreSlim semaphoreSlim = new(1, 1);

	/// <summary>
	/// Dismiss Toast
	/// </summary>
	public virtual async partial Task Dismiss(CancellationToken token)
	{
		if (nativeToast is null)
		{
			return;
		}

		await semaphoreSlim.WaitAsync(token);

		try
		{
			token.ThrowIfCancellationRequested();

			nativeToast.Dismiss();
		}
		finally
		{
			semaphoreSlim.Release();
		}
	}

	/// <summary>
	/// Show Toast
	/// </summary>
	public virtual async partial Task Show(CancellationToken token)
	{
		await Dismiss(token);
		token.ThrowIfCancellationRequested();

		var cornerRadius = CreateCornerRadius();
		var padding = GetMaximum(cornerRadius.X, cornerRadius.Y, cornerRadius.Width, cornerRadius.Height) + ToastView.DefaultPadding;
		
		nativeToast = new ToastView(Text,
											Defaults.BackgroundColor.ToNative(),
											cornerRadius,
											Defaults.TextColor.ToNative(),
											UIFont.SystemFontOfSize((nfloat)TextSize),
											Defaults.CharacterSpacing,
											padding)
		{
			Duration = GetDuration(Duration)
		};

		nativeToast.Show();

		static T? GetMaximum<T>(params T[] items) => items.Max();
	}

	static CGRect CreateCornerRadius(int radius = 4)
	{
		return new CGRect(radius, radius, radius, radius);
	}
}