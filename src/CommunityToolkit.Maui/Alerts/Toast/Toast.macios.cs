using CommunityToolkit.Maui.Core.Views;
using CoreGraphics;
using UIKit;

namespace CommunityToolkit.Maui.Alerts;

public partial class Toast
{
	static ToastView? _nativeToast;

	/// <summary>
	/// Dismiss Toast
	/// </summary>
	public virtual partial Task Dismiss(CancellationToken token)
	{
		if (_nativeToast is null)
			return Task.CompletedTask;

		_nativeToast.Dismiss();
		_nativeToast = null;
		return Task.CompletedTask;
	}

	/// <summary>
	/// Show Toast
	/// </summary>
	public virtual async partial Task Show(CancellationToken token)
	{
		await Dismiss(token);

		var cornerRadius = GetCornerRadius();
		var padding = GetMaximum(cornerRadius.X, cornerRadius.Y, cornerRadius.Width, cornerRadius.Height) + ToastView.DefaultPadding;
		_nativeToast = new ToastView(Text,
											UIColor.LightGray,
											cornerRadius,
											UIColor.DarkTextColor,
											UIFont.SystemFontOfSize(UIFont.LabelFontSize),
											1,
											padding)
		{
			Duration = GetDuration(Duration)
		};

		_nativeToast.Show();

		static T? GetMaximum<T>(params T[] items) => items.Max();
	}

	static CGRect GetCornerRadius(int defaultRadius = 4)
	{
		return new CGRect(defaultRadius, defaultRadius, defaultRadius, defaultRadius);
	}
}