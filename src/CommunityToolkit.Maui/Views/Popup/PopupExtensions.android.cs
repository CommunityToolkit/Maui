using System.Diagnostics.CodeAnalysis;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Platform;
using static Android.Views.View;
using APoint = Android.Graphics.Point;
using AView = Android.Views.View;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;
using LayoutOptions = Microsoft.Maui.Controls.LayoutAlignment;
using MView = Microsoft.Maui.Controls.View;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Extension methods for <see cref="Popup"/>.
/// </summary>
public static partial class PopupExtensions
{
	static APoint realSize = new();
	static APoint displaySize = new();
	static APoint displaySmallSize = new();
	static APoint displayLargeSize = new();

	static void PlatformShowPopup(Page page, Popup popup)
	{
		var mauiContext = GetMauiContext(page);
		popup.Parent = PageExtensions.GetCurrentPage(page);
		var platformPopup = popup.ToHandler(mauiContext);
		platformPopup.Invoke(nameof(IPopup.OnOpened));

		if (platformPopup.PlatformView is Dialog dialog &&
			platformPopup.VirtualView is IPopup pPopup &&
			pPopup.Content?.ToPlatform(mauiContext) is AView container)
		{
			PopupExtensions.SetSize(dialog, popup, pPopup, container);
		}
	}

	static Task<object?> PlatformShowPopupAsync(Page page, Popup popup)
	{
		PlatformShowPopup(page, popup);
		return popup.Result;
	}

	static Android.Views.Window GetWindow(in Dialog dialog) =>
		dialog.Window ?? throw new InvalidOperationException($"{nameof(Dialog)}.{nameof(Dialog.Window)} cannot be null");

	public static void SetSize(Dialog dialog, Popup vPopup, IPopup pPopup, AView container)
	{
		ArgumentNullException.ThrowIfNull(dialog);
		ArgumentNullException.ThrowIfNull(container);
		ArgumentNullException.ThrowIfNull(pPopup?.Content);

		var window = GetWindow(dialog);
		var context = dialog.Context;
		var windowManager = window.WindowManager;

		var decorView = (ViewGroup)window.DecorView;
		var child = decorView.GetChildAt(0) ?? throw new InvalidOperationException($"No child found in {nameof(ViewGroup)}");

		var windowSize = GetWindowSize(windowManager, decorView);

		if (!TryCalculateSizes(vPopup, pPopup, context, windowSize, out var realWidth, out var realHeight, out _, out _))
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

		switch (pPopup.Content.VerticalLayoutAlignment)
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
				throw new NotSupportedException($"{nameof(pPopup.Content.HorizontalLayoutAlignment)} {pPopup.Content.VerticalLayoutAlignment} is not supported");
		}

		switch (pPopup.Content.HorizontalLayoutAlignment)
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
				throw new NotSupportedException($"{nameof(pPopup.Content.HorizontalLayoutAlignment)} {pPopup.Content.HorizontalLayoutAlignment} is not supported");
		}

		container.LayoutParameters = containerLayoutParams;
	}

	static void Measure(Context context, MView target, int width, int height, bool isNanWidth, bool isNanHeight)
	{
		if (target is Microsoft.Maui.Controls.Shapes.RoundRectangle)
		{
			return;
		}

		var platformView = target.ToPlatform();

		bool isHorizontalFill = ((int)target.HorizontalOptions.Alignment) == ((int)LayoutOptions.Fill);
		bool isVerticalFill = ((int)target.VerticalOptions.Alignment) == ((int)LayoutOptions.Fill);

		int paddingH = 0, paddingV = 0;
		if (target.Parent is Layout layout)
		{
			paddingH = (int)context.ToPixels(layout.Padding.Left + layout.Padding.Right);
			paddingV = (int)context.ToPixels(layout.Padding.Top + layout.Padding.Bottom);
		}

		if (target.Parent is VerticalStackLayout ||
		   (target.Parent is StackLayout stackV && stackV.Orientation == StackOrientation.Vertical))
		{
			var requestWidth = ((target.Width != -1 && target.WidthRequest != -1) ? (int)context.ToPixels(target.Width) : width) - paddingH;
			var requestHeight = ((target.Height != -1 && target.HeightRequest != -1) ? (int)context.ToPixels(target.Height) : height) - paddingV;

			platformView.Measure(
				MeasureSpec.MakeMeasureSpec(requestWidth, (target.Width != -1 && target.WidthRequest != -1) ? MeasureSpecMode.Exactly : isHorizontalFill ? MeasureSpecMode.Exactly : MeasureSpecMode.AtMost),
				MeasureSpec.MakeMeasureSpec(requestHeight, (target.Height != -1 && target.HeightRequest != -1) ? MeasureSpecMode.Exactly : MeasureSpecMode.AtMost)
			);
		}
		else if (target.Parent is HorizontalStackLayout ||
				(target.Parent is StackLayout stackH && stackH.Orientation == StackOrientation.Horizontal))
		{
			var requestWidth = ((target.Width != -1 && target.WidthRequest != -1) ? (int)context.ToPixels(target.Width) : width) - paddingH;
			var requestHeight = ((target.Height != -1 && target.HeightRequest != -1) ? (int)context.ToPixels(target.Height) : height) - paddingV;

			platformView.Measure(
				MeasureSpec.MakeMeasureSpec(requestWidth, (target.Width != -1 && target.WidthRequest != -1) ? MeasureSpecMode.Exactly : MeasureSpecMode.AtMost),
				MeasureSpec.MakeMeasureSpec(requestHeight, (target.Height != -1 && target.HeightRequest != -1) ? MeasureSpecMode.Exactly : isVerticalFill ? MeasureSpecMode.Exactly : MeasureSpecMode.AtMost)
			);
		}
		else if (target.Parent is Grid grid)
		{
			int requestWidth, requestHeight;
			MeasureSpecMode measureSpecModeH, measureSpecModeV;
			if (grid.ColumnDefinitions.Count == 0)
			{
				requestWidth = ((target.Width != -1 && target.WidthRequest != -1) ? (int)context.ToPixels(target.Width) : width);
				measureSpecModeH = (target.Width != -1 && target.WidthRequest != -1) ? MeasureSpecMode.Exactly : isHorizontalFill ? MeasureSpecMode.Exactly : MeasureSpecMode.AtMost;
			}
			else
			{
				if (target.Width != -1 && target.WidthRequest != -1)
				{
					requestWidth = (int)context.ToPixels(target.Width);
					measureSpecModeH = MeasureSpecMode.Exactly;
				}
				else
				{
					if (grid.ColumnDefinitions[grid.GetColumn(target)].Width.IsStar)
					{
						double fixWidth = 0;
						double totalStar = 0;
						bool isAuto = false;
						foreach (ColumnDefinition cd in grid.ColumnDefinitions)
						{
							if (!cd.Width.IsStar && !cd.Width.IsAuto)
							{
								fixWidth += cd.Width.Value;
							}
							if (cd.Width.IsAuto)
							{
								isAuto = true;
							}
							if (cd.Width.IsStar)
							{
								totalStar += cd.Width.Value;
							}
						}

						requestWidth = (int)((width - (int)context.ToPixels(fixWidth)) / totalStar * grid.ColumnDefinitions[grid.GetColumn(target)].Width.Value);
						measureSpecModeH = isHorizontalFill ? (fixWidth == 0 && isAuto && grid.Width == -1) ? MeasureSpecMode.Unspecified : MeasureSpecMode.Exactly : MeasureSpecMode.AtMost;
					}
					else if (grid.ColumnDefinitions[grid.GetColumn(target)].Width.IsAuto)
					{
						requestWidth = width;
						measureSpecModeH = MeasureSpecMode.Unspecified;
					}
					else
					{
						requestWidth = (int)context.ToPixels(grid.ColumnDefinitions[grid.GetColumn(target)].Width.Value);
						measureSpecModeH = isHorizontalFill ? MeasureSpecMode.Exactly : MeasureSpecMode.AtMost;
					}
				}
			}

			if (grid.RowDefinitions.Count == 0)
			{
				requestHeight = ((target.Height != -1 && target.HeightRequest != -1) ? (int)context.ToPixels(target.Height) : height);
				measureSpecModeV = (target.Height != -1 && target.HeightRequest != -1) ? MeasureSpecMode.Exactly : isVerticalFill ? MeasureSpecMode.Exactly : MeasureSpecMode.AtMost;
			}
			else
			{
				if ((target.Height != -1 && target.HeightRequest != -1))
				{
					requestHeight = (int)context.ToPixels(target.Height);
					measureSpecModeV = MeasureSpecMode.Exactly;
				}
				else
				{
					if (grid.RowDefinitions[grid.GetRow(target)].Height.IsStar)
					{
						double fixHeight = 0;
						double totalStar = 0;
						bool isAuto = false;
						foreach (RowDefinition rd in grid.RowDefinitions)
						{
							if (!rd.Height.IsStar && !rd.Height.IsAuto)
							{
								fixHeight += rd.Height.Value;
							}
							if (rd.Height.IsAuto)
							{
								isAuto = true;
							}
							if (rd.Height.IsStar)
							{
								totalStar += rd.Height.Value;
							}
						}

						requestHeight = (int)((height - (int)context.ToPixels(fixHeight)) / totalStar * grid.RowDefinitions[grid.GetRow(target)].Height.Value);			
						measureSpecModeV = isVerticalFill ? (fixHeight == 0 && isAuto && grid.Height == -1) ? MeasureSpecMode.Unspecified : MeasureSpecMode.Exactly : MeasureSpecMode.AtMost;
					}
					else if (grid.RowDefinitions[grid.GetRow(target)].Height.IsAuto)
					{
						requestHeight = height;
						measureSpecModeV = MeasureSpecMode.Unspecified;
					}
					else
					{
						requestHeight = (int)context.ToPixels(grid.RowDefinitions[grid.GetRow(target)].Height.Value);
						measureSpecModeV = isVerticalFill ? MeasureSpecMode.Exactly : MeasureSpecMode.AtMost;
					}
				}
			}

			platformView.Measure(
				MeasureSpec.MakeMeasureSpec(requestWidth, measureSpecModeH),
				MeasureSpec.MakeMeasureSpec(requestHeight, measureSpecModeV)
			);
		}
		else if (target.Parent is ReorderableItemsView)
		{
			var requestWidth = ((target.Width != -1 || target.WidthRequest != -1) ? (int)context.ToPixels(target.Width) : width);
			var requestHeight = ((target.Height != -1 || target.HeightRequest != -1) ? (int)context.ToPixels(target.Height) : height);

			platformView.Measure(
				MeasureSpec.MakeMeasureSpec(requestWidth, (target.Width != -1 || target.WidthRequest != -1) ? MeasureSpecMode.Exactly : isHorizontalFill ? MeasureSpecMode.Exactly : MeasureSpecMode.AtMost),
				MeasureSpec.MakeMeasureSpec(requestHeight, (target.Height != -1 || target.HeightRequest != -1) ? MeasureSpecMode.Exactly : isVerticalFill ? MeasureSpecMode.Exactly : MeasureSpecMode.AtMost)
			);
		}
		else
		{
			var requestWidth = ((target.Width != -1 && target.WidthRequest != -1) ? (int)context.ToPixels(target.Width) : width) - paddingH;
			var requestHeight = ((target.Height != -1 && target.HeightRequest != -1) ? (int)context.ToPixels(target.Height) : height) - paddingV;

			platformView.Measure(
				MeasureSpec.MakeMeasureSpec(requestWidth, !isNanWidth ? MeasureSpecMode.Exactly : isHorizontalFill ? MeasureSpecMode.AtMost : MeasureSpecMode.AtMost),
				MeasureSpec.MakeMeasureSpec(requestHeight, !isNanHeight ? MeasureSpecMode.Exactly : isVerticalFill ? MeasureSpecMode.AtMost : MeasureSpecMode.AtMost)
			);
		}

		if (platformView is ViewGroup)
		{
			width = platformView.MeasuredWidth;
			height = platformView.MeasuredHeight;
		}

		foreach (Element element in target.LogicalChildrenInternal)
		{
			if (element is MView view)
			{
				Measure(context, view, width, height, isNanWidth, isNanHeight);
			}
		}
	}

	static bool TryCalculateSizes(
		Popup vPopup,
		IPopup pPopup,
		Context context,
		Size windowSize,
		[NotNullWhen(true)] out int? realWidth,
		[NotNullWhen(true)] out int? realHeight,
		[NotNullWhen(true)] out int? realContentWidth,
		[NotNullWhen(true)] out int? realContentHeight)
	{
		ArgumentNullException.ThrowIfNull(pPopup.Content);
		ArgumentNullException.ThrowIfNull(pPopup.Content.ToPlatform());
		ArgumentNullException.ThrowIfNull(vPopup.Content);

		realWidth = realHeight = realContentWidth = realContentHeight = null;

		var pView = pPopup.Content.ToPlatform();

		if (pPopup.Size.IsZero)
		{
			if (double.IsNaN(pPopup.Content.Width) || double.IsNaN(pPopup.Content.Height))
			{
				Measure(
					context,
					vPopup.Content,
					(int)(double.IsNaN(pPopup.Content.Width) ? windowSize.Width : (int)context.ToPixels(pPopup.Content.Width)),
					(int)(double.IsNaN(pPopup.Content.Height) ? windowSize.Height : (int)context.ToPixels(pPopup.Content.Height)),
					double.IsNaN(pPopup.Content.Width) && pPopup.HorizontalOptions != LayoutAlignment.Fill,
					double.IsNaN(pPopup.Content.Height) && pPopup.VerticalOptions != LayoutAlignment.Fill
				);
				realContentWidth = pView.MeasuredWidth;
				realContentHeight = pView.MeasuredHeight;

				if (double.IsNaN(pPopup.Content.Width))
				{
					realContentWidth = pPopup.HorizontalOptions == LayoutAlignment.Fill ? (int)windowSize.Width : realContentWidth;
				}
				if (double.IsNaN(pPopup.Content.Height))
				{
					realContentHeight = pPopup.VerticalOptions == LayoutAlignment.Fill ? (int)windowSize.Height : realContentHeight;
				}
			}
			else
			{
				realContentWidth = (int)context.ToPixels(pPopup.Content.Width);
				realContentHeight = (int)context.ToPixels(pPopup.Content.Height);
			}
		}
		else
		{
			Measure(
				context,
				vPopup.Content,
				(int)context.ToPixels(pPopup.Size.Width),
				(int)context.ToPixels(pPopup.Size.Height),
				false,
				false
			);
			realContentWidth = pView.MeasuredWidth;
			realContentHeight = pView.MeasuredHeight;
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

	static Size GetWindowSize(IWindowManager? windowManager, ViewGroup decorView)
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
			windowManager.DefaultDisplay.GetRealSize(realSize);
			windowManager.DefaultDisplay.GetSize(displaySize);
			windowManager.DefaultDisplay.GetCurrentSizeRange(displaySmallSize, displayLargeSize);

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