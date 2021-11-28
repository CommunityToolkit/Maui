using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Alerts.Toast;
using CoreGraphics;
using Microsoft.Maui;
using UIKit;

namespace CommunityToolkit.Maui.Alerts.Snackbar;

public partial class Snackbar
{
	readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

	static SnackbarView? _nativeSnackbar;

	/// <summary>
	/// Dismiss Snackbar
	/// </summary>
	public async Task Dismiss()
	{
		if (_nativeSnackbar is null)
			return;

		await _semaphoreSlim.WaitAsync();

		try
		{
			_nativeSnackbar.Dismiss();
			_nativeSnackbar = null;

			OnDismissed();
		}
		finally
		{
			_semaphoreSlim.Release();
		}
	}

	/// <summary>
	/// Show Snackbar
	/// </summary>
	public async Task Show()
	{
		await Dismiss();

		var cornerRadius = GetCornerRadius(VisualOptions.CornerRadius);
		var padding = GetMaximum(cornerRadius.X, cornerRadius.Y, cornerRadius.Width, cornerRadius.Height) + SnackbarView.DefaultPadding;
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
			Duration = Duration
		};

		_nativeSnackbar.Show();

		OnShown();

		static T? GetMaximum<T>(params T[] items) => items.Max();
	}

	static CGRect GetCornerRadius(CornerRadius cornerRadius)
	{
		return new CGRect(cornerRadius.BottomLeft, cornerRadius.TopLeft, cornerRadius.TopRight, cornerRadius.BottomRight);
	}

	sealed class SnackbarView : ToastView, IDisposable
	{
		readonly PaddedButton _actionButton;

		public SnackbarView(
			string message,
			UIColor backgroundColor,
			CGRect cornerRadius,
			UIColor textColor,
			UIFont textFont,
			double characterSpacing,
			string actionButtonText,
			UIColor actionTextColor,
			UIFont actionButtonFont,
			double padding = DefaultPadding)
			: base(message, backgroundColor, cornerRadius, textColor, textFont, characterSpacing, padding)
		{
			_actionButton = new PaddedButton(padding, padding, padding, padding);
			ActionButtonText = actionButtonText;
			ActionTextColor = actionTextColor;
			ActionButtonFont = actionButtonFont;

			_actionButton.TouchUpInside += ActionButton_TouchUpInside;
			PopupView.AddChild(_actionButton);
		}

		public Action? Action { get; init; }

		public string ActionButtonText
		{
			get => _actionButton.Title(UIControlState.Normal);
			private init => _actionButton.SetTitle(value, UIControlState.Normal);
		}

		public UIColor ActionTextColor
		{
			get => _actionButton.TitleColor(UIControlState.Normal);
			private init => _actionButton.SetTitleColor(value, UIControlState.Normal);
		}

		public UIFont ActionButtonFont
		{
			get => _actionButton.Font;
			private init => _actionButton.Font = value;
		}

		void ActionButton_TouchUpInside(object? sender, EventArgs e)
		{
			Action?.Invoke();
			PopupView.Dismiss();
		}

		public void Dispose()
		{
			_actionButton.TouchUpInside -= ActionButton_TouchUpInside;
		}

		class PaddedButton : UIButton
		{
			public PaddedButton(double left, double top, double right, double bottom)
			{
				Left = left;
				Top = top;
				Right = right;
				Bottom = bottom;
				ContentEdgeInsets = new UIEdgeInsets((nfloat)top, (nfloat)left, (nfloat)bottom, (nfloat)right);
			}

			public double Left { get; }

			public double Top { get; }

			public double Right { get; }

			public double Bottom { get; }
		}
	}
}