using CommunityToolkit.Maui.Core;
using CoreGraphics;
using Microsoft.Maui.Platform;
using ObjCRuntime;
using UIKit;

namespace CommunityToolkit.Core.Platform;
public static class PopupExtensions
{
	public static void SetSize(this PopupRenderer popup, in IBasePopup basepopup)
	{
		if (!basepopup.Size.IsZero)
		{
			popup.PreferredContentSize = new CGSize(basepopup.Size.Width, basepopup.Size.Height);
		}
	}

	public static void SetBackgroundColor(this PopupRenderer popup, in IBasePopup basePopup)
	{
		if (popup.Control is null)
		{
			return;
		}
		var color = basePopup.Color?.ToNative() ?? null;
		popup.Control.NativeView.BackgroundColor = color;
	}

	public static void SetLightDismiss(this PopupRenderer popup, in IBasePopup basepopup)
	{
			popup.ModalInPresentation = !basepopup.IsLightDismissEnabled;
	}

	public static void SetLayout(this PopupRenderer popup, in IBasePopup basepopup)
	{
		var presentationController = popup.PresentationController;
		var preferredContentSize = popup.PreferredContentSize;

		((UIPopoverPresentationController)presentationController).SourceRect = new CGRect(0, 0, preferredContentSize.Width, preferredContentSize.Height);

		if (basepopup.Anchor is null)
		{
			var originY = basepopup.VerticalOptions switch
			{
				Microsoft.Maui.Primitives.LayoutAlignment.End => UIScreen.MainScreen.Bounds.Height,
				Microsoft.Maui.Primitives.LayoutAlignment.Center => UIScreen.MainScreen.Bounds.Height / 2,
				_ => 0f
			};

			var originX = basepopup.HorizontalOptions switch
			{
				Microsoft.Maui.Primitives.LayoutAlignment.End => UIScreen.MainScreen.Bounds.Width,
				Microsoft.Maui.Primitives.LayoutAlignment.Center => UIScreen.MainScreen.Bounds.Width / 2,
				_ => 0f
			};

			popup.PopoverPresentationController.SourceRect = new CGRect(originX, originY, 0, 0);
			popup.PopoverPresentationController.PermittedArrowDirections = 0;
		}
		else
		{
			var view = basepopup.Anchor.ToNative(basepopup.Handler?.MauiContext ?? throw new NullReferenceException());
			popup.PopoverPresentationController.SourceView = view;
			popup.PopoverPresentationController.SourceRect = view.Bounds;
		}
	}
}
