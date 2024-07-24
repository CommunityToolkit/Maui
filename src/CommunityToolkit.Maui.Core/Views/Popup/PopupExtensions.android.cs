using System.Diagnostics.CodeAnalysis;
using Android.Graphics.Drawables;
using Android.Views;
using CommunityToolkit.Maui.Core.Handlers;
using Microsoft.Maui.Platform;
using static Android.Views.ViewGroup;
using AColorRes = Android.Resource.Color;
using APoint = Android.Graphics.Point;
using AView = Android.Views.View;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// Extension class where Helper methods for Popup lives.
/// </summary>
public static class PopupExtensions
{
	/// <summary>
	/// Method to update the <see cref="IPopup.Anchor"/> view.
	/// </summary>
	/// <param name="dialog">An instance of <see cref="Dialog"/>.</param>
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	/// <param name="popupWidth">Width of Popup</param>
	/// <param name="popupHeight">Height of Popup</param>
	/// <exception cref="InvalidOperationException">if the <see cref="Window"/> is null an exception will be thrown.</exception>
	public static void SetAnchor(this Dialog dialog, in IPopup popup, int? popupWidth = null, int? popupHeight = null)
	{
		var window = GetWindow(dialog);

		var windowManager = window.WindowManager;
		var statusBarHeight = GetStatusBarHeight(windowManager);
		var navigationBarHeight = GetNavigationBarHeight(windowManager);
		var windowSize = GetWindowSize(windowManager);
		var rotation = windowManager.DefaultDisplay?.Rotation ?? throw new InvalidOperationException("DefaultDisplay cannot be null");
		navigationBarHeight = windowSize.Height < windowSize.Width ? (rotation == SurfaceOrientation.Rotation270 ? navigationBarHeight : 0) : 0;

		if (popup.Handler?.MauiContext is null)
		{
			return;
		}

		if (popup.Anchor is not null)
		{
			var anchorView = popup.Anchor.ToPlatform();

			var locationOnScreen = new int[2];
			anchorView.GetLocationOnScreen(locationOnScreen);
			if (popupWidth is null && popupHeight is null)
			{
				window.DecorView.Measure((int)MeasureSpecMode.Unspecified, (int)MeasureSpecMode.Unspecified);
			}

			// This logic is tricky, please read these notes if you need to modify
			// Android window coordinate starts (0,0) at the top left and (max,max) at the bottom right. All of the positions
			// that are being handled in this operation assume the point is at the top left of the rectangle. This means the
			// calculation operates in this order:
			// 1. Calculate top-left position of Anchor
			// 2. Calculate the Actual Center of the Anchor by adding the width /2 and height / 2
			// 3. Calculate the top-left point of where the dialog should be positioned by subtracting the Width / 2 and height / 2
			//    of the dialog that is about to be drawn.
			var attribute = window.Attributes ?? throw new InvalidOperationException($"{nameof(window.Attributes)} cannot be null");

			var newX = locationOnScreen[0] - navigationBarHeight + (anchorView.Width / 2) - (popupWidth == null ? (window.DecorView.Width / 2) : (int)(popupWidth / 2));
			var newY = locationOnScreen[1] - statusBarHeight + (anchorView.Height / 2) - (popupHeight == null ? (window.DecorView.Height / 2) : (int)(popupHeight / 2));

			if (!(newX == attribute.X &&
				  newY == attribute.Y))
			{
				window.SetGravity(GravityFlags.Top | GravityFlags.Left);
				attribute.X = newX;
				attribute.Y = newY;
				window.Attributes = attribute;
			}
		}
		else
		{
			SetDialogPosition(popup, window);
		}
	}

	/// <summary>
	/// Method to update the <see cref="IPopup.Color"/> property.
	/// </summary>
	/// <param name="dialog">An instance of <see cref="Dialog"/>.</param>
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	public static void SetColor(this Dialog dialog, in IPopup popup)
	{
		if (popup.Color is null)
		{
			return;
		}

		var window = GetWindow(dialog);
		window.SetBackgroundDrawable(new ColorDrawable(popup.Color.ToPlatform(AColorRes.BackgroundLight, dialog.Context)));
	}

	/// <summary>
	/// Method to update the <see cref="IPopup.CanBeDismissedByTappingOutsideOfPopup"/> property.
	/// </summary>
	/// <param name="dialog">An instance of <see cref="Dialog"/>.</param>
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	public static void SetCanBeDismissedByTappingOutsideOfPopup(this Dialog dialog, in IPopup popup)
	{
		dialog.SetCancelable(popup.CanBeDismissedByTappingOutsideOfPopup);
		dialog.SetCanceledOnTouchOutside(popup.CanBeDismissedByTappingOutsideOfPopup);
	}

	/// <summary>
	/// Method to update the <see cref="IPopup.Size"/> property.
	/// </summary>
	/// <param name="dialog">An instance of <see cref="Dialog"/>.</param>
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	/// <param name="container">The native representation of <see cref="IPopup.Content"/>.</param>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <exception cref="InvalidOperationException">if the <see cref="Window"/> is null an exception will be thrown. If the <paramref name="container"/> is null an exception will be thrown.</exception>
	public static void SetSize(this Dialog dialog, in IPopup popup, in AView container, PopupHandler handler)
	{
		ArgumentNullException.ThrowIfNull(dialog);
		ArgumentNullException.ThrowIfNull(container);
		ArgumentNullException.ThrowIfNull(popup.Content);
		ArgumentNullException.ThrowIfNull(handler);

		var window = GetWindow(dialog);
		var context = dialog.Context;
		var windowManager = window.WindowManager;

		var decorView = (ViewGroup)window.DecorView;

		var windowSize = GetWindowSize(windowManager);
		int width = LayoutParams.WrapContent;
		int height = LayoutParams.WrapContent;

		if (popup.Size.IsZero)
		{
			if (double.IsNaN(popup.Content.Width) || double.IsNaN(popup.Content.Height))
			{
				if ((handler.LastPopupWidth == decorView.MeasuredWidth
					&& handler.LastPopupHeight == decorView.MeasuredHeight)
					&& Math.Abs(handler.LastWindowWidth - windowSize.Width) < 0.01 // Allow for floating point variation 
					&& Math.Abs(handler.LastWindowHeight - windowSize.Height) < 0.01) // Allow for floating point variation
				{
					SetAnchor(dialog, popup, handler.LastPopupWidth, handler.LastPopupHeight);
					return;
				}

				decorView.Measure(
					MeasureSpecMode.AtMost.MakeMeasureSpec((int)windowSize.Width),
					MeasureSpecMode.AtMost.MakeMeasureSpec((int)windowSize.Height)
				);

				if (double.IsNaN(popup.Content.Width))
				{
					if (popup.HorizontalOptions == LayoutAlignment.Fill)
					{
						width = (int)windowSize.Width;
					}
					else
					{
						if (decorView.MeasuredWidth >= windowSize.Width)
						{
							width = (int)windowSize.Width;
						}
					}
				}
				else
				{
					if (context.ToPixels(popup.Content.Width) >= windowSize.Width)
					{
						width = (int)windowSize.Width;
					}
					else
					{
						width = (int)context.ToPixels(popup.Content.Width);
					}
				}

				if (double.IsNaN(popup.Content.Height))
				{
					if (popup.VerticalOptions == LayoutAlignment.Fill)
					{
						height = (int)windowSize.Height;
					}
					else
					{
						if (decorView.MeasuredHeight >= windowSize.Height)
						{
							height = (int)windowSize.Height;
						}
					}
				}
				else
				{
					if (context.ToPixels(popup.Content.Height) >= windowSize.Height)
					{
						height = (int)windowSize.Height;
					}
					else
					{
						height = (int)context.ToPixels(popup.Content.Height);
					}
				}

				window.SetLayout(width, height);

				width = width == LayoutParams.WrapContent ? decorView.MeasuredWidth : width;
				height = height == LayoutParams.WrapContent ? decorView.MeasuredHeight : height;

				handler.LastPopupWidth = decorView.Width;
				handler.LastPopupHeight = decorView.Height;
				handler.LastWindowWidth = windowSize.Width;
				handler.LastWindowHeight = windowSize.Height;
			}
			else
			{
				width = (int)context.ToPixels(popup.Content.Width);
				height = (int)context.ToPixels(popup.Content.Height);
				width = width > windowSize.Width ? (int)windowSize.Width : width;
				height = height > windowSize.Height ? (int)windowSize.Height : height;

				if (handler.LastPopupWidth == width
					&& handler.LastPopupHeight == height
					&& Math.Abs(handler.LastWindowWidth - windowSize.Width) < 0.01  // Allow for floating point variation 
					&& Math.Abs(handler.LastWindowHeight - windowSize.Height) < 0.01)// Allow for floating point variation
				{
					SetAnchor(dialog, popup, handler.LastPopupWidth, handler.LastPopupHeight);
					return;
				}

				window.SetLayout(width, height);

				handler.LastPopupWidth = decorView.Width;
				handler.LastPopupHeight = decorView.Height;
				handler.LastWindowWidth = windowSize.Width;
				handler.LastWindowHeight = windowSize.Height;
			}
		}
		else
		{
			width = (int)context.ToPixels(popup.Size.Width);
			height = (int)context.ToPixels(popup.Size.Height);
			width = width > windowSize.Width ? (int)windowSize.Width : width;
			height = height > windowSize.Height ? (int)windowSize.Height : height;

			if (handler.LastPopupWidth == width
				&& handler.LastPopupHeight == height
				&& Math.Abs(handler.LastWindowWidth - windowSize.Width) < 0.01  // Allow for floating point variation
				&& Math.Abs(handler.LastWindowHeight - windowSize.Height) < 0.01)  // Allow for floating point variation
			{
				SetAnchor(dialog, popup, handler.LastPopupWidth, handler.LastPopupHeight);
				return;
			}

			window.SetLayout(width, height);

			handler.LastPopupWidth = decorView.Width;
			handler.LastPopupHeight = decorView.Height;
			handler.LastWindowWidth = windowSize.Width;
			handler.LastWindowHeight = windowSize.Height;
		}

		SetAnchor(dialog, popup, width, height);
	}

	static void SetDialogPosition(in IPopup popup, Window window)
	{
		var isFlowDirectionRightToLeft = popup.Content?.FlowDirection == FlowDirection.RightToLeft;

		var gravityFlags = popup.VerticalOptions switch
		{
			LayoutAlignment.Start => GravityFlags.Top,
			LayoutAlignment.End => GravityFlags.Bottom,
			LayoutAlignment.Center or LayoutAlignment.Fill => GravityFlags.CenterVertical,
			_ => throw new NotSupportedException($"{nameof(IPopup.VerticalOptions)}: {popup.VerticalOptions} is not yet supported")
		};

		gravityFlags |= popup.HorizontalOptions switch
		{
			LayoutAlignment.Start => isFlowDirectionRightToLeft ? GravityFlags.Right : GravityFlags.Left,
			LayoutAlignment.End => isFlowDirectionRightToLeft ? GravityFlags.Left : GravityFlags.Right,
			LayoutAlignment.Center or LayoutAlignment.Fill => GravityFlags.CenterHorizontal,
			_ => throw new NotSupportedException($"{nameof(IPopup.HorizontalOptions)}: {popup.HorizontalOptions} is not yet supported")
		};

		window.SetGravity(gravityFlags);
	}

	static Window GetWindow(in Dialog dialog) =>
		dialog.Window ?? throw new InvalidOperationException($"{nameof(Dialog)}.{nameof(Dialog.Window)} cannot be null");

	static Size GetWindowSize([NotNull] IWindowManager? windowManager)
	{
		ArgumentNullException.ThrowIfNull(windowManager);

		int windowWidth;
		int windowHeight;
		int statusBarHeight;
		int navigationBarHeight;

		if (OperatingSystem.IsAndroidVersionAtLeast(30))
		{
			var windowMetrics = windowManager.CurrentWindowMetrics;
			var windowInsets = windowMetrics.WindowInsets.GetInsetsIgnoringVisibility(WindowInsets.Type.SystemBars());
			windowWidth = windowMetrics.Bounds.Width();
			windowHeight = windowMetrics.Bounds.Height();
			statusBarHeight = windowInsets.Top;
			navigationBarHeight = windowHeight < windowWidth ? windowInsets.Left + windowInsets.Right : windowInsets.Bottom;
		}
		else if (windowManager.DefaultDisplay is null)
		{
			throw new InvalidOperationException($"{nameof(IWindowManager)}.{nameof(IWindowManager.DefaultDisplay)} cannot be null");
		}
		else
		{
			APoint realSize = new();
			APoint displaySize = new();
			APoint displaySmallSize = new();
			APoint displayLargeSize = new();

			windowManager.DefaultDisplay.GetRealSize(realSize);
			ArgumentNullException.ThrowIfNull(realSize);

			windowManager.DefaultDisplay.GetSize(displaySize);
			ArgumentNullException.ThrowIfNull(displaySize);

			windowManager.DefaultDisplay.GetCurrentSizeRange(displaySmallSize, displayLargeSize);
			ArgumentNullException.ThrowIfNull(displaySmallSize);
			ArgumentNullException.ThrowIfNull(displayLargeSize);

			windowWidth = realSize.X;
			windowHeight = realSize.Y;

			if (displaySize.X > displaySize.Y)
			{
				statusBarHeight = displaySize.Y - displaySmallSize.Y;
			}
			else
			{
				statusBarHeight = displaySize.Y - displayLargeSize.Y;
			}

			navigationBarHeight = realSize.Y < realSize.X
									? (realSize.X - displaySize.X)
									: (realSize.Y - displaySize.Y);
		}

		windowWidth -= windowHeight < windowWidth
			? navigationBarHeight
			: 0;

		windowHeight -= (windowHeight < windowWidth
			? 0
			: navigationBarHeight)
			+ statusBarHeight;

		return new Size(windowWidth, windowHeight);
	}

	static int GetNavigationBarHeight(IWindowManager? windowManager)
	{
		ArgumentNullException.ThrowIfNull(windowManager);

		int navigationBarHeight;

		if (OperatingSystem.IsAndroidVersionAtLeast(30))
		{
			var windowMetrics = windowManager.CurrentWindowMetrics;
			var windowInsets = windowMetrics.WindowInsets.GetInsetsIgnoringVisibility(WindowInsets.Type.SystemBars());
			var windowWidth = windowMetrics.Bounds.Width();
			var windowHeight = windowMetrics.Bounds.Height();
			navigationBarHeight = windowHeight < windowWidth ? windowInsets.Left + windowInsets.Right : windowInsets.Bottom;
		}
		else if (windowManager.DefaultDisplay is null)
		{
			throw new InvalidOperationException($"{nameof(IWindowManager)}.{nameof(IWindowManager.DefaultDisplay)} cannot be null");
		}
		else
		{
			APoint realSize = new();
			APoint displaySize = new();
			APoint displaySmallSize = new();
			APoint displayLargeSize = new();

			windowManager.DefaultDisplay.GetRealSize(realSize);
			ArgumentNullException.ThrowIfNull(realSize);

			windowManager.DefaultDisplay.GetSize(displaySize);
			ArgumentNullException.ThrowIfNull(displaySize);

			windowManager.DefaultDisplay.GetCurrentSizeRange(displaySmallSize, displayLargeSize);
			ArgumentNullException.ThrowIfNull(displaySmallSize);
			ArgumentNullException.ThrowIfNull(displayLargeSize);

			navigationBarHeight = realSize.Y < realSize.X
									? (realSize.X - displaySize.X)
									: (realSize.Y - displaySize.Y);
		}

		return navigationBarHeight;
	}

	static int GetStatusBarHeight(IWindowManager? windowManager)
	{
		ArgumentNullException.ThrowIfNull(windowManager);

		int statusBarHeight;

		if (OperatingSystem.IsAndroidVersionAtLeast(30))
		{
			var windowMetrics = windowManager.CurrentWindowMetrics;
			var windowInsets = windowMetrics.WindowInsets.GetInsetsIgnoringVisibility(WindowInsets.Type.SystemBars());
			statusBarHeight = windowInsets.Top;
		}
		else if (windowManager.DefaultDisplay is null)
		{
			throw new InvalidOperationException($"{nameof(IWindowManager)}.{nameof(IWindowManager.DefaultDisplay)} cannot be null");
		}
		else
		{
			APoint realSize = new();
			APoint displaySize = new();
			APoint displaySmallSize = new();
			APoint displayLargeSize = new();

			windowManager.DefaultDisplay.GetRealSize(realSize);
			ArgumentNullException.ThrowIfNull(realSize);

			windowManager.DefaultDisplay.GetSize(displaySize);
			ArgumentNullException.ThrowIfNull(displaySize);

			windowManager.DefaultDisplay.GetCurrentSizeRange(displaySmallSize, displayLargeSize);
			ArgumentNullException.ThrowIfNull(displaySmallSize);
			ArgumentNullException.ThrowIfNull(displayLargeSize);

			if (displaySize.X > displaySize.Y)
			{
				statusBarHeight = displaySize.Y - displaySmallSize.Y;
			}
			else
			{
				statusBarHeight = displaySize.Y - displayLargeSize.Y;
			}
		}

		return statusBarHeight;
	}
}