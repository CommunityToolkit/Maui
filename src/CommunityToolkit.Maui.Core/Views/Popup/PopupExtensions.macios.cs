using CommunityToolkit.Maui.Core;
using CoreGraphics;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.Core.Views;
/// <summary>
/// Extension class where Helper methods for Popup lives.
/// </summary>
public static class PopupExtensions
{
	/// <summary>
	/// Method to update the <see cref="IPopup.Size"/> of the Popup.
	/// </summary>
	/// <param name="mauiPopup">An instance of <see cref="MauiPopup"/>.</param>
	/// <param name="popup">An istance of <see cref="IPopup"/>.</param>
	public static void SetSize(this MauiPopup mauiPopup, in IPopup popup)
	{
		if (!popup.Size.IsZero)
		{
			mauiPopup.PreferredContentSize = new CGSize(popup.Size.Width, popup.Size.Height);
		}
		else if (popup.Content is not null)
		{
			if (!popup.Content.DesiredSize.IsZero)
			{
				var contentSize = popup.Content.DesiredSize;
				mauiPopup.PreferredContentSize = new CGSize(contentSize.Width, contentSize.Height);
			}
			else
			{
				var measure = popup.Content.Measure(double.PositiveInfinity, double.PositiveInfinity);
				mauiPopup.PreferredContentSize = new CGSize(measure.Width, measure.Height);
			}
		}
	}

	/// <summary>
	/// Method to update the <see cref="IPopup.Color"/> of the Popup.
	/// </summary>
	/// <param name="mauiPopup">An instance of <see cref="MauiPopup"/>.</param>
	/// <param name="popup">An istance of <see cref="IPopup"/>.</param>
	public static void SetBackgroundColor(this MauiPopup mauiPopup, in IPopup popup)
	{
		if (mauiPopup.Control is null)
		{
			return;
		}

		var color = popup.Color?.ToNative();
		mauiPopup.Control.NativeView.BackgroundColor = color;
	}

	/// <summary>
	/// Method to update the <see cref="IPopup.CanBeDismissedByTappingOutsideOfPopup"/> property of the Popup.
	/// </summary>
	/// <param name="mauiPopup">An instance of <see cref="MauiPopup"/>.</param>
	/// <param name="popup">An istance of <see cref="IPopup"/>.</param>
	public static void SetCanBeDismissedByTappingOutsideOfPopup(this MauiPopup mauiPopup, in IPopup popup)
	{
		mauiPopup.ModalInPresentation = !popup.CanBeDismissedByTappingOutsideOfPopup;
	}

	/// <summary>
	/// Method to update the layout of the Popup and <see cref="IPopup.Content"/>.
	/// </summary>
	/// <param name="mauiPopup">An instance of <see cref="MauiPopup"/>.</param>
	/// <param name="popup">An istance of <see cref="IPopup"/>.</param>
	public static void SetLayout(this MauiPopup mauiPopup, in IPopup popup)
	{
		var presentationController = mauiPopup.PresentationController;
		var preferredContentSize = mauiPopup.PreferredContentSize;

		((UIPopoverPresentationController)presentationController).SourceRect = new CGRect(0, 0, preferredContentSize.Width, preferredContentSize.Height);

		if (popup.Anchor is null)
		{
			var originY = popup.VerticalOptions switch
			{
				Microsoft.Maui.Primitives.LayoutAlignment.End => UIScreen.MainScreen.Bounds.Height,
				Microsoft.Maui.Primitives.LayoutAlignment.Center => UIScreen.MainScreen.Bounds.Height / 2,
				_ => 0f
			};

			var originX = popup.HorizontalOptions switch
			{
				Microsoft.Maui.Primitives.LayoutAlignment.End => UIScreen.MainScreen.Bounds.Width,
				Microsoft.Maui.Primitives.LayoutAlignment.Center => UIScreen.MainScreen.Bounds.Width / 2,
				_ => 0f
			};

			mauiPopup.PopoverPresentationController.SourceRect = new CGRect(originX, originY, 0, 0);
			mauiPopup.PopoverPresentationController.PermittedArrowDirections = 0;
		}
		else
		{
			var view = popup.Anchor.ToNative(popup.Handler?.MauiContext ?? throw new NullReferenceException());
			mauiPopup.PopoverPresentationController.SourceView = view;
			mauiPopup.PopoverPresentationController.SourceRect = view.Bounds;
		}

	}
}
