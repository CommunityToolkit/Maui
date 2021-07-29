#if IOS || MACCATALYST
using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.UI.Views.Helpers;
using CommunityToolkit.Maui.UI.Views.Options;
using CommunityToolkit.Maui.UI.Views.Snackbar.Helpers;
using Microsoft.Maui.Controls;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Compatibility.Platform.iOS;
using Microsoft.Maui.Graphics;
using UIKit;

namespace CommunityToolkit.Maui.UI.Views
{
	class SnackBar
	{
		internal ValueTask Show(VisualElement sender, SnackBarOptions arguments)
		{
			var snackBar = NativeSnackBar.MakeSnackBar(arguments.MessageOptions.Message)
							.SetDuration(arguments.Duration)
							.SetCornerRadius(arguments.CornerRadius)
							.SetTimeoutAction(() =>
							{
								arguments.SetResult(false);
								return Task.CompletedTask;
							});

			if (arguments.BackgroundColor != Colors.Black)
			{
				snackBar.Appearance.Background = arguments.BackgroundColor.ToUIColor();
			}

			if (arguments.MessageOptions.Font != Font.Default)
			{
				snackBar.Appearance.Font = arguments.MessageOptions.Font.ToUIFont();
			}

			if (arguments.MessageOptions.Foreground != Colors.White)
			{
				snackBar.Appearance.Foreground = arguments.MessageOptions.Foreground.ToUIColor();
			}

			if (arguments.MessageOptions.Padding != MessageOptions.DefaultPadding)
			{
				snackBar.Layout.PaddingTop = (nfloat)arguments.MessageOptions.Padding.Top;
				snackBar.Layout.PaddingLeft = (nfloat)arguments.MessageOptions.Padding.Left;
				snackBar.Layout.PaddingBottom = (nfloat)arguments.MessageOptions.Padding.Bottom;
				snackBar.Layout.PaddingRight = (nfloat)arguments.MessageOptions.Padding.Right;
			}

			snackBar.Appearance.TextAlignment = arguments.IsRtl ? UITextAlignment.Right : UITextAlignment.Left;

			if (!UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
			{
				snackBar.Layout.PaddingTop = (nfloat)arguments.MessageOptions.Padding.Top;
				snackBar.Layout.PaddingLeft = (nfloat)arguments.MessageOptions.Padding.Left;
				snackBar.Layout.PaddingBottom = (nfloat)arguments.MessageOptions.Padding.Bottom;
				snackBar.Layout.PaddingRight = (nfloat)arguments.MessageOptions.Padding.Right;
			}

			snackBar.Appearance.TextAlignment = arguments.IsRtl ? UITextAlignment.Right : UITextAlignment.Left;

			if (sender is not Page)
			{
				snackBar.SetAnchor((sender.Handler.NativeView as UIKit.UIView) ?? throw new InvalidOperationException("NativeView is null"));			
			}

			foreach (var action in arguments.Actions)
			{
				var actionButton = new NativeSnackButton(action.Padding.Left,
					action.Padding.Top,
					action.Padding.Right,
					action.Padding.Bottom);
				actionButton.SetActionButtonText(action.Text);

				if (action.BackgroundColor != Colors.Black)
				{
					actionButton.BackgroundColor = action.BackgroundColor.ToUIColor();
				}

				if (action.Font != Font.Default)
				{
					actionButton.Font = action.Font.ToUIFont();
				}

				if (action.ForegroundColor != Colors.White)
				{
					actionButton.SetTitleColor(action.ForegroundColor.ToUIColor(), UIControlState.Normal);
				}

				actionButton.SetAction(async () =>
				{
					snackBar.Dismiss();

					if (action.Action != null)
						await action.Action();

					arguments.SetResult(true);
				});

				snackBar.Actions.Add(actionButton);
			}

			snackBar.Show();

			return default;
		}
	}
}
#endif