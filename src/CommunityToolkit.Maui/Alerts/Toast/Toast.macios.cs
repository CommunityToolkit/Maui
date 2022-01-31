using CommunityToolkit.Maui.Core.Views;
using CoreGraphics;
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
											UIColor.LightGray,
											cornerRadius,
											UIColor.DarkTextColor,
											UIFont.SystemFontOfSize(UIFont.LabelFontSize),
											1,
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