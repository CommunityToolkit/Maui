using System.Runtime.InteropServices;
using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Maui;
using Microsoft.Maui.Platform;

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
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	public static void SetSize(this MauiPopup mauiPopup, in IPopup popup)
	{
		if (!popup.Size.IsZero)
		{
			mauiPopup.PreferredContentSize = new CGSize(popup.Size.Width, popup.Size.Height);
		}
		else if (popup.Content is not null)
		{
			var content = popup.Content;
			var measure = popup.Content.Measure(double.PositiveInfinity, double.PositiveInfinity);
			var width = content.Width.IsZeroOrNaN() ? measure.Width : content.Width;
			var height = content.Height.IsZeroOrNaN() ? measure.Height : content.Height;
			mauiPopup.PreferredContentSize = new CGSize(width, height);
		}
	}

	/// <summary>
	/// Method to update the <see cref="IPopup.Color"/> of the Popup.
	/// </summary>
	/// <param name="mauiPopup">An instance of <see cref="MauiPopup"/>.</param>
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	public static void SetBackgroundColor(this MauiPopup mauiPopup, in IPopup popup)
	{
		if (mauiPopup.PopoverPresentationController is not null && popup.Color == Colors.Transparent)
		{
			mauiPopup.PopoverPresentationController.PopoverBackgroundViewType = typeof(TransparentPopoverBackgroundView);
		}

		if (mauiPopup.Control is null)
		{
			return;
		}

		var color = popup.Color?.ToPlatform();
		mauiPopup.Control.PlatformView.BackgroundColor = color;

		if (mauiPopup.Control.ViewController?.View is UIView view)
		{
			view.BackgroundColor = color;
		}
	}

	/// <summary>
	/// Method to update the <see cref="IPopup.CanBeDismissedByTappingOutsideOfPopup"/> property of the Popup.
	/// </summary>
	/// <param name="mauiPopup">An instance of <see cref="MauiPopup"/>.</param>
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	public static void SetCanBeDismissedByTappingOutsideOfPopup(this MauiPopup mauiPopup, in IPopup popup)
	{
		if (OperatingSystem.IsIOSVersionAtLeast(13))
		{
			mauiPopup.ModalInPresentation = !popup.CanBeDismissedByTappingOutsideOfPopup;
		}
	}

	/// <summary>
	/// Method to update the layout of the Popup and <see cref="IPopup.Content"/>.
	/// </summary>
	/// <param name="mauiPopup">An instance of <see cref="MauiPopup"/>.</param>
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	public static void SetLayout(this MauiPopup mauiPopup, in IPopup popup)
	{
		if (mauiPopup.View is null)
		{
			return;
		}

		CGRect frame;

		if (mauiPopup.ViewController?.View?.Window is UIWindow window)
		{
			frame = window.Frame;
		}
		else
		{
			frame = UIScreen.MainScreen.Bounds;
		}

		if (mauiPopup.PopoverPresentationController is null)
		{
			throw new InvalidOperationException("PopoverPresentationController cannot be null");
		}

		if (popup.Anchor is null)
		{
			nfloat originY;
			if (mauiPopup.PreferredContentSize.Height < frame.Height)
			{
				originY = popup.VerticalOptions switch
				{
					Microsoft.Maui.Primitives.LayoutAlignment.Start => mauiPopup.PreferredContentSize.Height / 2,
					Microsoft.Maui.Primitives.LayoutAlignment.End => frame.Height - (mauiPopup.PreferredContentSize.Height / 2),
					Microsoft.Maui.Primitives.LayoutAlignment.Center or Microsoft.Maui.Primitives.LayoutAlignment.Fill => frame.GetMidY(),
					_ => throw new NotSupportedException($"{nameof(Microsoft.Maui.Primitives.LayoutAlignment)} {popup.VerticalOptions} is not yet supported")
				};
			}
			else
			{
				originY = -frame.GetMidY();
			}

			nfloat originX;
			if (mauiPopup.PreferredContentSize.Width < frame.Width)
			{
				originX = popup.HorizontalOptions switch
				{
					Microsoft.Maui.Primitives.LayoutAlignment.Start => mauiPopup.PreferredContentSize.Width / 2,
					Microsoft.Maui.Primitives.LayoutAlignment.End => frame.Width - (mauiPopup.PreferredContentSize.Width / 2),
					Microsoft.Maui.Primitives.LayoutAlignment.Center or Microsoft.Maui.Primitives.LayoutAlignment.Fill => frame.GetMidX(),
					_ => throw new NotSupportedException($"{nameof(Microsoft.Maui.Primitives.LayoutAlignment)} {popup.VerticalOptions} is not yet supported")
				};
			}
			else
			{
				originX = -frame.GetMidX();
			}

			mauiPopup.PopoverPresentationController.SourceRect = new CGRect(originX, originY, 0, 0);
			mauiPopup.PopoverPresentationController.PermittedArrowDirections = 0;
			// From the point of view of usability, the top, bottom, left, and right values of UIEdgeInsets cannot all be 0.
			// If you specify 0 for the top, bottom, left, and right of UIEdgeInsets, the default margins will be added, so 
			// specify a value as close to 0 here as possible.
			mauiPopup.PopoverPresentationController.PopoverLayoutMargins = new UIEdgeInsets(0.0001f, 0.0001f, 0.0001f, 0.0001f);
		}
		else
		{
			var view = popup.Anchor.ToPlatform(popup.Handler?.MauiContext ?? throw new InvalidOperationException($"{nameof(popup.Handler.MauiContext)} Cannot Be Null"));
			mauiPopup.PopoverPresentationController.SourceView = view;
			mauiPopup.PopoverPresentationController.SourceRect = view.Bounds;
		}
	}

	class TransparentPopoverBackgroundView : UIPopoverBackgroundView
	{
		public TransparentPopoverBackgroundView(IntPtr handle) : base(handle)
		{
			BackgroundColor = Colors.Transparent.ToPlatform();
			Alpha = 0.0f;
		}

		public override NFloat ArrowOffset { get; set; }

		public override UIPopoverArrowDirection ArrowDirection { get; set; }

		[Export("arrowHeight")]
		static new float GetArrowHeight()
		{
			return 0f;
		}

		[Export("arrowBase")]
		static new float GetArrowBase()
		{
			return 0f;
		}

		[Export("contentViewInsets")]
		static new UIEdgeInsets GetContentViewInsets()
		{
			return UIEdgeInsets.Zero;
		}
	}
}