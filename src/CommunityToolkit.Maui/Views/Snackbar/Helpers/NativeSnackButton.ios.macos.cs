using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.UI.Views.Helpers.Apple;
using UIKit;

namespace CommunityToolkit.Maui.UI.Views.Snackbar.Helpers
{
	class NativeSnackButton : UIButton
	{
		public NativeSnackButton(double left, double top, double right, double bottom)
		{
			Left = left;
			Top = top;
			Right = right;
			Bottom = bottom;
			LineBreakMode = NativeSnackButtonAppearance.LineBreakMode;

			ContentEdgeInsets = new UIEdgeInsets((nfloat)top, (nfloat)left, (nfloat)bottom, (nfloat)right);
			TouchUpInside += async (s, e) =>
			{
				if (SnackButtonAction != null)
					await SnackButtonAction();
			};
		}

		public double Left { get; }

		public double Top { get; }

		public double Right { get; }

		public double Bottom { get; }

		public Func<Task>? SnackButtonAction { get; protected set; }

		public NativeSnackButton SetAction(Func<Task> action)
		{
			SnackButtonAction = action;
			return this;
		}

		public NativeSnackButton SetActionButtonText(string title)
		{
			SetTitle(title, UIControlState.Normal);
			return this;
		}
	}
}