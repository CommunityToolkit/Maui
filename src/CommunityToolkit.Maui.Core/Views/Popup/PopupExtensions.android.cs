using System.Diagnostics.CodeAnalysis;
using Android.Content;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using AndroidX.AppCompat.Widget;
using Microsoft.Maui.Platform;
using static Android.Views.View;
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
	/// <exception cref="InvalidOperationException">if the <see cref="Window"/> is null an exception will be thrown.</exception>
	public static void SetAnchor(this Dialog dialog, in IPopup popup)
	{
		var window = GetWindow(dialog);

		if (popup.Handler?.MauiContext is null)
		{
			return;
		}

		if (popup.Anchor is not null)
		{
			var anchorView = popup.Anchor.ToPlatform();

			var locationOnScreen = new int[2];
			anchorView.GetLocationOnScreen(locationOnScreen);
			window.SetGravity(GravityFlags.Top | GravityFlags.Left);
			window.DecorView.Measure((int)MeasureSpecMode.Unspecified, (int)MeasureSpecMode.Unspecified);

			// This logic is tricky, please read these notes if you need to modify
			// Android window coordinate starts (0,0) at the top left and (max,max) at the bottom right. All of the positions
			// that are being handled in this operation assume the point is at the top left of the rectangle. This means the
			// calculation operates in this order:
			// 1. Calculate top-left position of Anchor
			// 2. Calculate the Actual Center of the Anchor by adding the width /2 and height / 2
			// 3. Calculate the top-left point of where the dialog should be positioned by subtracting the Width / 2 and height / 2
			//    of the dialog that is about to be drawn.
			_ = window.Attributes ?? throw new InvalidOperationException($"{nameof(window.Attributes)} cannot be null");

			window.Attributes.X = locationOnScreen[0] + (anchorView.Width / 2) - (window.DecorView.MeasuredWidth / 2);
			window.Attributes.Y = locationOnScreen[1] + (anchorView.Height / 2) - (window.DecorView.MeasuredHeight / 2);
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
	/// <exception cref="InvalidOperationException">if the <see cref="Window"/> is null an exception will be thrown. If the <paramref name="container"/> is null an exception will be thrown.</exception>
	public static void SetSize(this Dialog dialog, in IPopup popup, in AView container)
	{
		ArgumentNullException.ThrowIfNull(dialog);
		ArgumentNullException.ThrowIfNull(container);
		ArgumentNullException.ThrowIfNull(popup?.Content);

		var window = GetWindow(dialog);
		var context = dialog.Context;
		var windowManager = window.WindowManager;

		var decorView = (ViewGroup)window.DecorView;
		var child = decorView.GetChildAt(0) ?? throw new InvalidOperationException($"No child found in {nameof(ViewGroup)}");

		var windowSize = GetWindowSize(windowManager);

		if (!TryCalculateSizes(popup, context, windowSize, out var realWidth, out var realHeight, out _, out _))
		{
			throw new InvalidOperationException("Unable to calculate screen size");
		}

		window.SetLayout(realWidth.Value, realHeight.Value);

		var childLayoutParams = (FrameLayout.LayoutParams)(child.LayoutParameters ?? throw new InvalidOperationException($"{nameof(child.LayoutParameters)} cannot be null"));
		childLayoutParams.Width = realWidth.Value;
		childLayoutParams.Height = realHeight.Value;
		child.LayoutParameters = childLayoutParams;

		var horizontalParams = realWidth;
		var verticalParams = realHeight;

		var containerLayoutParams = new FrameLayout.LayoutParams(horizontalParams.Value, verticalParams.Value);

		switch (popup.Content.VerticalLayoutAlignment)
		{
			case LayoutAlignment.Start:
				containerLayoutParams.Gravity = GravityFlags.Top;
				break;
			case LayoutAlignment.Center:
			case LayoutAlignment.Fill:
				containerLayoutParams.Gravity = GravityFlags.FillVertical;
				containerLayoutParams.Height = realHeight.Value;
				break;
			case LayoutAlignment.End:
				containerLayoutParams.Gravity = GravityFlags.Bottom;
				break;
			default:
				throw new NotSupportedException($"{nameof(popup.Content.HorizontalLayoutAlignment)} {popup.Content.VerticalLayoutAlignment} is not supported");
		}

		switch (popup.Content.HorizontalLayoutAlignment)
		{
			case LayoutAlignment.Start:
				containerLayoutParams.Gravity |= GravityFlags.Left;
				break;
			case LayoutAlignment.Center:
			case LayoutAlignment.Fill:
				containerLayoutParams.Gravity |= GravityFlags.FillHorizontal;
				containerLayoutParams.Width = realWidth.Value;
				break;
			case LayoutAlignment.End:
				containerLayoutParams.Gravity |= GravityFlags.Right;
				break;
			default:
				throw new NotSupportedException($"{nameof(popup.Content.HorizontalLayoutAlignment)} {popup.Content.HorizontalLayoutAlignment} is not supported");
		}

		container.LayoutParameters = containerLayoutParams;


		static bool TryCalculateSizes(IPopup popup,
			Context context,
			Size windowSize,
			[NotNullWhen(true)] out int? realWidth,
			[NotNullWhen(true)] out int? realHeight,
			[NotNullWhen(true)] out int? realContentWidth,
			[NotNullWhen(true)] out int? realContentHeight)
		{
			ArgumentNullException.ThrowIfNull(popup.Content);

			realWidth = realHeight = realContentWidth = realContentHeight = null;

			var view = popup.Content.ToPlatform();

			if (popup.Size.IsZero)
			{
				if (double.IsNaN(popup.Content.Width) || double.IsNaN(popup.Content.Height))
				{
					var isRootView = true;
					Measure(
						view,
						(int)(double.IsNaN(popup.Content.Width) ? windowSize.Width : (int)context.ToPixels(popup.Content.Width)),
						(int)(double.IsNaN(popup.Content.Height) ? windowSize.Height : (int)context.ToPixels(popup.Content.Height)),
						double.IsNaN(popup.Content.Width) && popup.HorizontalOptions != LayoutAlignment.Fill,
						double.IsNaN(popup.Content.Height) && popup.VerticalOptions != LayoutAlignment.Fill,
						ref isRootView
					);

					realContentWidth = view.MeasuredWidth;
					realContentHeight = view.MeasuredHeight;

					if (double.IsNaN(popup.Content.Width))
					{
						realContentWidth = popup.HorizontalOptions == LayoutAlignment.Fill ? (int)windowSize.Width : realContentWidth;
					}

					if (double.IsNaN(popup.Content.Height))
					{
						realContentHeight = popup.VerticalOptions == LayoutAlignment.Fill ? (int)windowSize.Height : realContentHeight;
					}
				}
				else
				{
					realContentWidth = (int)context.ToPixels(popup.Content.Width);
					realContentHeight = (int)context.ToPixels(popup.Content.Height);
				}
			}
			else
			{
				var isRootView = true;
				Measure(
					view,
					(int)context.ToPixels(popup.Size.Width),
					(int)context.ToPixels(popup.Size.Height),
					false,
					false,
					ref isRootView
				);

				realContentWidth = view.MeasuredWidth;
				realContentHeight = view.MeasuredHeight;
			}

			realWidth = Math.Min(realWidth ?? realContentWidth.Value, (int)windowSize.Width);
			realHeight = Math.Min(realHeight ?? realContentHeight.Value, (int)windowSize.Height);

			if (realHeight is 0 || realWidth is 0)
			{
				realWidth = (int)(context.Resources?.DisplayMetrics?.WidthPixels * 0.8 ?? throw new InvalidOperationException($"Unable to determine width. {nameof(context.Resources.DisplayMetrics)} cannot be null"));
				realHeight = (int)(context.Resources?.DisplayMetrics?.HeightPixels * 0.6 ?? throw new InvalidOperationException($"Unable to determine height. {nameof(context.Resources.DisplayMetrics)} cannot be null"));
			}

			return true;
		}

		static void Measure(AView view, int width, int height, bool isNanWidth, bool isNanHeight, ref bool isRootView)
		{
			if (isRootView)
			{
				isRootView = false;
				view.Measure(
					MeasureSpec.MakeMeasureSpec(width, !isNanWidth ? MeasureSpecMode.Exactly : MeasureSpecMode.AtMost),
					MeasureSpec.MakeMeasureSpec(height, !isNanHeight ? MeasureSpecMode.Exactly : MeasureSpecMode.AtMost)
				);
			}

			if (view is AppCompatTextView)
			{
				// https://github.com/dotnet/maui/issues/2019
				// https://github.com/dotnet/maui/pull/2059
				var layoutParams = view.LayoutParameters;
				view.Measure(
					MeasureSpec.MakeMeasureSpec(width, (layoutParams?.Width == LinearLayout.LayoutParams.WrapContent && !isNanWidth) ? MeasureSpecMode.Exactly : MeasureSpecMode.Unspecified),
					MeasureSpec.MakeMeasureSpec(height, (layoutParams?.Height == LinearLayout.LayoutParams.WrapContent && !isNanHeight) ? MeasureSpecMode.Exactly : MeasureSpecMode.Unspecified)
				);
			}

			if (view is ViewGroup viewGroup)
			{
				for (int i = 0; i < viewGroup.ChildCount; i++)
				{
					if (viewGroup.GetChildAt(i) is AView childView)
					{
						Measure(childView, width, height, isNanWidth, isNanHeight, ref isRootView);
					}
				}
			}
		}

		static Size GetWindowSize(IWindowManager? windowManager)
		{
			ArgumentNullException.ThrowIfNull(windowManager);

			int windowWidth;
			int windowHeight;
			int statusBarHeight;
			int navigationBarHeight;

			if (OperatingSystem.IsAndroidVersionAtLeast((int)BuildVersionCodes.R))
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

			windowWidth = (windowWidth - (windowHeight < windowWidth ? navigationBarHeight : 0));
			windowHeight = (windowHeight - ((windowHeight < windowWidth ? 0 : navigationBarHeight) + statusBarHeight));

			return new Size(windowWidth, windowHeight);
		}
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
}