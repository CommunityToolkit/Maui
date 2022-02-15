using CommunityToolkit.Maui.Core;
using CoreGraphics;
using Microsoft.Maui.Platform;
using ObjCRuntime;
using UIKit;

namespace CommunityToolkit.Core.Platform;
/// <summary>
/// Extension class where Helper methods for Popup lives.
/// </summary>
public static class PopupExtensions
{
	/// <summary>
	/// Method to update the <see cref="IPopup.Size"/> of the Popup.
	/// </summary>
	/// <param name="popup">An instance of <see cref="MCTPopup"/>.</param>
	/// <param name="basePopup">An istance of <see cref="IPopup"/>.</param>
	public static void SetSize(this MCTPopup popup, in IPopup basePopup)
	{
		if (!basePopup.Size.IsZero)
		{
			popup.PreferredContentSize = new CGSize(basePopup.Size.Width, basePopup.Size.Height);
		}
	}

	/// <summary>
	/// Method to update the <see cref="IPopup.Color"/> of the Popup.
	/// </summary>
	/// <param name="popup">An instance of <see cref="MCTPopup"/>.</param>
	/// <param name="basePopup">An istance of <see cref="IPopup"/>.</param>
	public static void SetBackgroundColor(this MCTPopup popup, in IPopup basePopup)
	{
		if (popup.Control is null)
		{
			return;
		}
		var color = basePopup.Color?.ToNative() ?? null;
		popup.Control.NativeView.BackgroundColor = color;
	}

	/// <summary>
	/// Method to update the <see cref="IPopup.IsLightDismissEnabled"/> property of the Popup.
	/// </summary>
	/// <param name="popup">An instance of <see cref="MCTPopup"/>.</param>
	/// <param name="basePopup">An istance of <see cref="IPopup"/>.</param>
	public static void SetLightDismiss(this MCTPopup popup, in IPopup basePopup)
	{
			popup.ModalInPresentation = !basePopup.IsLightDismissEnabled;
	}

	/// <summary>
	/// Method to update the layout of the Popup and <see cref="IPopup.Content"/>.
	/// </summary>
	/// <param name="popup">An instance of <see cref="MCTPopup"/>.</param>
	/// <param name="basepopup">An istance of <see cref="IPopup"/>.</param>
	public static void SetLayout(this MCTPopup popup, in IPopup basepopup)
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
