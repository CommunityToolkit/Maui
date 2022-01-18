using CommunityToolkit.Maui.Core;
//using CommunityToolkit.Maui.Extensions.Workarounds;
using CoreGraphics;
using Microsoft.Maui.Platform;
using Microsoft.Maui.Primitives;
using UIKit;
//using PopoverArrowDirection = CommunityToolkit.Maui.PlatformConfiguration.iOSSpecific.PopoverArrowDirection;
//using Specifics = CommunityToolkit.Maui.PlatformConfiguration.iOSSpecific.PopUp;

namespace CommunityToolkit.Core.Platform;
public static class PopupExtensions
{
	public static void SetSize(this UIViewController popup, in IBasePopup basepopup)
	{
		if (!basepopup.Size.IsZero)
		{
			popup.PreferredContentSize = new CGSize(basepopup.Size.Width, basepopup.Size.Height);
		}
	}

	public static void SetBackgroundColor(this UIViewController popup, in IBasePopup basePopup)
	{
		if (popup.View is null || basePopup.Color is null)
		{
			return;
		}

		popup.View.BackgroundColor = basePopup.Color.ToNative();
	}

	public static void SetLayout(this UIViewController popup, in IBasePopup basepopup)
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
			//if (basepopup is not BasePopup bp)
			//{
			//	throw new ArgumentException(nameof(basepopup), $"{nameof(basepopup)} must be an {typeof(BasePopup)}.");
			//}

			//popup.SetLayout(basepopup);
			//var view = basepopup.Anchor.ToNative(basepopup.Handler?.MauiContext ?? throw new NullReferenceException());
			//popup.PopoverPresentationController.SourceView = view;
			//popup.PopoverPresentationController.SourceRect = view.Bounds;

			//var arrowDirection = Specifics.GetArrowDirection(bp);
			//popup.PopoverPresentationController.PermittedArrowDirections = arrowDirection switch
			//{
			//	PopoverArrowDirection.Up => UIPopoverArrowDirection.Up,
			//	PopoverArrowDirection.Down => UIPopoverArrowDirection.Down,
			//	PopoverArrowDirection.Left => UIPopoverArrowDirection.Left,
			//	PopoverArrowDirection.Right => UIPopoverArrowDirection.Right,
			//	PopoverArrowDirection.Any => UIPopoverArrowDirection.Any,
			//	PopoverArrowDirection.Unknown => UIPopoverArrowDirection.Unknown,
			//	_ => 0
			//};
		}
	}
}
