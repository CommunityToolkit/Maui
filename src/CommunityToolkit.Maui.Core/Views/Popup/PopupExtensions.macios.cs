using System.Runtime.InteropServices;
using Microsoft.Maui.Platform;
using ObjCRuntime;

namespace CommunityToolkit.Maui.Core.Views;
/// <summary>
/// Extension class where Helper methods for Popup lives.
/// </summary>
public static class PopupExtensions
{
	static readonly nfloat defaultPopoverLayoutMargin = 0.0001f;

#if MACCATALYST
	// https://github.com/CommunityToolkit/Maui/pull/1361#issuecomment-1736487174
	static nfloat popupMargin = 18f;
#endif


	/// <summary>
	/// Method to update the <see cref="IPopup.Size"/> of the Popup.
	/// </summary>
	/// <param name="mauiPopup">An instance of <see cref="MauiPopup"/>.</param>
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	public static void SetSize(this MauiPopup mauiPopup, in IPopup popup)
	{
		ArgumentNullException.ThrowIfNull(popup.Content);

		CGRect frame;

		if (mauiPopup.ViewController?.View?.Window is UIWindow window)
		{
			frame = window.Frame;
		}
		else
		{
			frame = UIScreen.MainScreen.Bounds;
		}

		CGSize currentSize;

		if (popup.Size.IsZero)
		{
			if (double.IsNaN(popup.Content.Width) || double.IsNaN(popup.Content.Height))
			{
				var content = popup.Content.ToPlatform(popup.Handler?.MauiContext ?? throw new InvalidOperationException($"{nameof(popup.Handler.MauiContext)} Cannot Be Null"));
				var contentSize = content.SizeThatFits(new CGSize(double.IsNaN(popup.Content.Width) ? frame.Width : popup.Content.Width, double.IsNaN(popup.Content.Height) ? frame.Height : popup.Content.Height));
				var width = contentSize.Width;
				var height = contentSize.Height;

				if (double.IsNaN(popup.Content.Width))
				{
					width = popup.HorizontalOptions == Microsoft.Maui.Primitives.LayoutAlignment.Fill ? frame.Size.Width : width;
				}
				if (double.IsNaN(popup.Content.Height))
				{
					height = popup.VerticalOptions == Microsoft.Maui.Primitives.LayoutAlignment.Fill ? frame.Size.Height : height;
				}

				currentSize = new CGSize(width, height);
			}
			else
			{
				currentSize = new CGSize(popup.Content.Width, popup.Content.Height);
			}
		}
		else
		{
			currentSize = new CGSize(popup.Size.Width, popup.Size.Height);
		}

#if MACCATALYST
		currentSize.Width = NMath.Min(currentSize.Width, frame.Size.Width - defaultPopoverLayoutMargin * 2 - popupMargin * 2);
		currentSize.Height = NMath.Min(currentSize.Height, frame.Size.Height - defaultPopoverLayoutMargin * 2 - popupMargin * 2);
#else
		currentSize.Width = NMath.Min(currentSize.Width, frame.Size.Width);
		currentSize.Height = NMath.Min(currentSize.Height, frame.Size.Height);
#endif
		mauiPopup.PreferredContentSize = currentSize;
	}

	/// <summary>
	/// Method to update the <see cref="IPopup.Color"/> of the Popup.
	/// </summary>
	/// <param name="mauiPopup">An instance of <see cref="MauiPopup"/>.</param>
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	public static void SetBackgroundColor(this MauiPopup mauiPopup, in IPopup popup)
	{
		if (mauiPopup.PopoverPresentationController is not null && Equals(popup.Color, Colors.Transparent))
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

#if MACCATALYST
		var titleBarHeight = mauiPopup.ViewController?.NavigationController?.NavigationBar.Frame.Y ?? 0;
		var navigationBarHeight = mauiPopup.ViewController?.NavigationController?.NavigationBar.Frame.Size.Height ?? 0;
#endif

		if (popup.Anchor is null)
		{
			var isFlowDirectionRightToLeft = popup.Content?.FlowDirection == FlowDirection.RightToLeft;
			var horizontalOptionsPositiveNegativeMultiplier = isFlowDirectionRightToLeft ? (NFloat)(-1) : (NFloat)1;

			nfloat originY;
			if (mauiPopup.PreferredContentSize.Height < frame.Height)
			{
				originY = popup.VerticalOptions switch
				{
#if MACCATALYST
					Microsoft.Maui.Primitives.LayoutAlignment.Start => mauiPopup.PreferredContentSize.Height / 2 - (titleBarHeight + navigationBarHeight - popupMargin),
					Microsoft.Maui.Primitives.LayoutAlignment.End => frame.Height - mauiPopup.PreferredContentSize.Height / 2 - (titleBarHeight + navigationBarHeight + popupMargin),
					Microsoft.Maui.Primitives.LayoutAlignment.Center or Microsoft.Maui.Primitives.LayoutAlignment.Fill => frame.GetMidY() - (titleBarHeight + navigationBarHeight),
#else
					Microsoft.Maui.Primitives.LayoutAlignment.Start => mauiPopup.PreferredContentSize.Height / 2,
					Microsoft.Maui.Primitives.LayoutAlignment.End => frame.Height - (mauiPopup.PreferredContentSize.Height / 2),
					Microsoft.Maui.Primitives.LayoutAlignment.Center or Microsoft.Maui.Primitives.LayoutAlignment.Fill => frame.GetMidY(),
#endif
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
#if MACCATALYST
					Microsoft.Maui.Primitives.LayoutAlignment.Start => (frame.Width - frame.Width * horizontalOptionsPositiveNegativeMultiplier) / 2 - (mauiPopup.PreferredContentSize.Width - mauiPopup.PreferredContentSize.Width * horizontalOptionsPositiveNegativeMultiplier) / 2 - (popupMargin - popupMargin * horizontalOptionsPositiveNegativeMultiplier),
					Microsoft.Maui.Primitives.LayoutAlignment.End => (frame.Width + frame.Width * horizontalOptionsPositiveNegativeMultiplier) / 2 - (mauiPopup.PreferredContentSize.Width + mauiPopup.PreferredContentSize.Width * horizontalOptionsPositiveNegativeMultiplier) / 2 - (popupMargin + popupMargin * horizontalOptionsPositiveNegativeMultiplier),
					Microsoft.Maui.Primitives.LayoutAlignment.Center or Microsoft.Maui.Primitives.LayoutAlignment.Fill => frame.GetMidX() - mauiPopup.PreferredContentSize.Width / 2 - popupMargin,
#else
					Microsoft.Maui.Primitives.LayoutAlignment.Start => (frame.Width - frame.Width * horizontalOptionsPositiveNegativeMultiplier) / 2 - (mauiPopup.PreferredContentSize.Width / 2 - mauiPopup.PreferredContentSize.Width / 2 * horizontalOptionsPositiveNegativeMultiplier) / 2,
					Microsoft.Maui.Primitives.LayoutAlignment.End => (frame.Width + frame.Width * horizontalOptionsPositiveNegativeMultiplier) / 2 - (mauiPopup.PreferredContentSize.Width / 2 + mauiPopup.PreferredContentSize.Width / 2 * horizontalOptionsPositiveNegativeMultiplier) / 2,
					Microsoft.Maui.Primitives.LayoutAlignment.Center or Microsoft.Maui.Primitives.LayoutAlignment.Fill => frame.GetMidX(),
#endif
					_ => throw new NotSupportedException($"{nameof(Microsoft.Maui.Primitives.LayoutAlignment)} {popup.VerticalOptions} is not yet supported")
				};
			}
			else
			{
				originX = -frame.GetMidX();
			}

			if (mauiPopup.ViewController?.PopoverPresentationController is UIPopoverPresentationController popoverPresentationController)
			{
				mauiPopup.PopoverPresentationController.SourceView = popoverPresentationController.SourceView;
			}
			mauiPopup.PopoverPresentationController.SourceRect = new CGRect(originX, originY, 0, 0);
			mauiPopup.PopoverPresentationController.PermittedArrowDirections = 0;
			// From the point of view of usability, the top, bottom, left, and right values of UIEdgeInsets cannot all be 0.
			// If you specify 0 for the top, bottom, left, and right of UIEdgeInsets, the default margins will be added, so 
			// specify a value as close to 0 here as possible.
			mauiPopup.PopoverPresentationController.PopoverLayoutMargins = new UIEdgeInsets(defaultPopoverLayoutMargin, defaultPopoverLayoutMargin, defaultPopoverLayoutMargin, defaultPopoverLayoutMargin);
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