using System;
using CommunityToolkit.Maui.UI.Views.Helpers.Apple.SnackBar;
using CoreGraphics;

namespace CommunityToolkit.Maui.UI.Views.Helpers.Apple.SnackBarViews
{
	class MessageSnackBarView : BaseSnackBarView
	{
		public MessageSnackBarView(NativeSnackBar snackBar)
			: base(snackBar)
		{
		}

		protected override void Initialize(CGRect cornerRadius)
		{
			base.Initialize(cornerRadius);

			var messageLabel = new PaddedLabel(SnackBar.Layout.PaddingLeft,
				SnackBar.Layout.PaddingTop,
				SnackBar.Layout.PaddingRight,
				SnackBar.Layout.PaddingBottom)
			{
				Text = SnackBar.Message,
				Lines = 0,
				AdjustsFontSizeToFitWidth = true,
				TextAlignment = SnackBar.Appearance.TextAlignment
			};
			if (SnackBar.Appearance.Background != NativeSnackBarAppearance.DefaultColor)
			{
				messageLabel.BackgroundColor = SnackBar.Appearance.Background;
			}

			if (SnackBar.Appearance.Foreground != NativeSnackBarAppearance.DefaultColor)
			{
				messageLabel.TextColor = SnackBar.Appearance.Foreground;
			}

			if (SnackBar.Appearance.Font != NativeSnackBarAppearance.DefaultFont)
			{
				messageLabel.Font = SnackBar.Appearance.Font;
			}

			_ = StackView ?? throw new InvalidOperationException($"{nameof(StackView)} is null");
			StackView.AddArrangedSubview(messageLabel);
		}
	}
}