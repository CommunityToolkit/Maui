using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Maui.UI.Views.Helpers.Apple;
using CommunityToolkit.Maui.UI.Views.Helpers.Apple.SnackBar;
using CommunityToolkit.Maui.UI.Views.Helpers.Apple.SnackBarViews;
using CommunityToolkit.Maui.UI.Views.Snackbar.Helpers;
using CoreGraphics;
using Foundation;
using Microsoft.Maui;
using AnchorView = UIKit.UIView;

namespace CommunityToolkit.Maui.UI.Views.Helpers
{
	class NativeSnackBar
	{
		NSTimer? timer;

		public List<NativeSnackButton> Actions { get; protected set; } = new();

		public Func<Task>? TimeoutAction { get; protected set; }

		public NativeSnackBarAppearance Appearance { get; protected set; } = new();

		public TimeSpan Duration { get; protected set; }

		public SnackBarLayout Layout { get; } = new();

		public string Message { get; protected set; } = string.Empty;

		public AnchorView? Anchor { get; protected set; }

		protected BaseSnackBarView? SnackBarView { get; set; }

		public CGRect CornerRadius { get; set; } = new CGRect(10, 10, 10, 10);

		public void Dismiss()
		{
			if (timer != null)
			{
				timer.Invalidate();
				timer.Dispose();
				timer = null;
			}

			SnackBarView?.Dismiss();
		}

		public static NativeSnackBar MakeSnackBar(string message) => new NativeSnackBar { Message = message };

		public NativeSnackBar SetTimeoutAction(Func<Task> action)
		{
			TimeoutAction = action;
			return this;
		}

		public NativeSnackBar SetDuration(TimeSpan duration)
		{
			Duration = duration;
			return this;
		}

		public NativeSnackBar SetAnchor(AnchorView anchor)
		{
			Anchor = anchor;
			return this;
		}

		public NativeSnackBar SetCornerRadius(Thickness cornerRadius)
		{
			CornerRadius = new CGRect(cornerRadius.Left, cornerRadius.Top, cornerRadius.Right, cornerRadius.Bottom);
			return this;
		}

		public NativeSnackBar Show()
		{
			SnackBarView = GetSnackBarView();
			SnackBarView.AnchorView = Anchor;

			SnackBarView.ParentView?.AddSubview(SnackBarView);
			SnackBarView.ParentView?.BringSubviewToFront(SnackBarView);
			SnackBarView.Setup(CornerRadius);

			timer = NSTimer.CreateScheduledTimer(Duration, async t =>
			{
				if (TimeoutAction != null)
					await TimeoutAction();
				Dismiss();
			});

			return this;
		}

		BaseSnackBarView GetSnackBarView() => Actions.Any() ? new ActionMessageSnackBarView(this) : new MessageSnackBarView(this);
	}
}