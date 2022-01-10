using CommunityToolkit.Maui.Core.Views;
using CoreGraphics;
using UIKit;

namespace CommunityToolkit.Maui.Alerts;

public partial class Toast
{
	static ToastView? nativeToast;

	/// <summary>
	/// Dismiss Toast
	/// </summary>
	public virtual partial Task Dismiss(CancellationToken token)
	{
		if (nativeToast is null)
		{
			return Task.CompletedTask;
		}

		token.ThrowIfCancellationRequested();
		nativeToast.Dismiss();
		nativeToast = null;
		return Task.CompletedTask;
	}

	/// <summary>
	/// Show Toast
	/// </summary>
	public virtual async partial Task Show(CancellationToken token)
	{
		await Dismiss(token);
		token.ThrowIfCancellationRequested();

		var cornerRadius = GetCornerRadius();
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

	static CGRect GetCornerRadius(int defaultRadius = 4)
	{
		return new CGRect(defaultRadius, defaultRadius, defaultRadius, defaultRadius);
	}
}