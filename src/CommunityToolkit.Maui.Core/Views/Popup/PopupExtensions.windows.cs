using CommunityToolkit.Maui.Core.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.Maui.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// Extension class where Helper methods for Popup lives.
/// </summary>
public static class PopupExtensions
{
	/// <summary>
	/// Method to update the <see cref="Maui.Core.IPopup.Content"/> based on the <see cref="Maui.Core.IPopup.Color"/>.
	/// </summary>
	/// <param name="mauiPopup">An instance of <see cref="Popup"/>.</param>
	/// <param name="popup">An instance of <see cref="Maui.Core.IPopup"/>.</param>
	public static void SetColor(this Popup mauiPopup, IPopup popup)
	{
		ArgumentNullException.ThrowIfNull(popup.Content);

		var color = popup.Color ?? Colors.Transparent;
		if (mauiPopup.Child is FrameworkElement content)
		{
			var backgroundProperty = content.GetType().GetProperty("Background");
			backgroundProperty?.SetValue(content, color.ToPlatform());
		}
	}

	/// <summary>
	/// Method to update the popup anchor based on the <see cref="Maui.Core.IPopup.Anchor"/>.
	/// </summary>
	/// <param name="mauiPopup">An instance of <see cref="Popup"/>.</param>
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	/// <param name="mauiContext">An instance of <see cref="IMauiContext"/>.</param>
	public static void SetAnchor(this Popup mauiPopup, IPopup popup, IMauiContext? mauiContext)
	{
		ArgumentNullException.ThrowIfNull(mauiContext);
		mauiPopup.PlacementTarget = popup.Anchor?.ToPlatform(mauiContext);
	}

	/// <summary>
	///  Method to prepare control.
	/// </summary>
	/// <param name="mauiPopup">An instance of <see cref="Popup"/>.</param>
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	/// <param name="mauiContext">An instance of <see cref="IMauiContext"/>.</param>
	public static void ConfigureControl(this Popup mauiPopup, IPopup popup, IMauiContext? mauiContext)
	{
		ArgumentNullException.ThrowIfNull(mauiContext);
		if (popup.Content is not null && popup.Handler is PopupHandler handler)
		{
			mauiPopup.Child = handler.VirtualView.Content?.ToPlatform(mauiContext);
		}
	}

	/// <summary>
	/// Method to update the popup size based on the <see cref="Maui.Core.IPopup.Size"/>.
	/// </summary>
	/// <param name="mauiPopup">An instance of <see cref="Popup"/>.</param>
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	/// <param name="mauiContext">An instance of <see cref="IMauiContext"/>.</param>
	public static void SetSize(this Popup mauiPopup, IPopup popup, IMauiContext? mauiContext)
	{
		ArgumentNullException.ThrowIfNull(mauiContext);
		ArgumentNullException.ThrowIfNull(popup.Content);
		ArgumentNullException.ThrowIfNull(popup.Content.ToPlatform());

		var window = mauiContext.GetPlatformWindow();
		var windowSize = new Size(window.Bounds.Width, window.Bounds.Height);

		var pView = popup.Content.ToPlatform();
		if (popup.Size.IsZero)
		{
			if (double.IsNaN(popup.Content.Width) || double.IsNaN(popup.Content.Height))
			{
				pView.Width = double.NaN;
				pView.Height = double.NaN;

				pView.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));

				if (pView.ActualWidth >= windowSize.Width &&
					pView.ActualHeight >= windowSize.Height)
				{
					pView.Width = windowSize.Width;
					pView.UpdateHeight(popup.Content);
				}
				else if (pView.ActualWidth >= windowSize.Width)
				{
					pView.Width = windowSize.Width;
				}
				else if (pView.ActualHeight >= windowSize.Height)
				{
					pView.Height = windowSize.Height;
				}

				if (popup.HorizontalOptions == LayoutAlignment.Fill)
				{
					pView.Width = windowSize.Width;
				}
				if (popup.VerticalOptions == LayoutAlignment.Fill)
				{
					pView.Height = windowSize.Height;
				}
			}
			else
			{
				pView.Width = popup.Content.Width;
				pView.Height = popup.Content.Height;
			}
		}
		else
		{
			pView.Width = popup.Size.Width;
			pView.Height = popup.Size.Height;
		}

		if (!double.IsNaN(pView.Width))
		{
			pView.Width = Math.Min(pView.Width, window.Bounds.Width);
		}
		if (!double.IsNaN(pView.Height))
		{
			pView.Height = Math.Min(pView.Height, window.Bounds.Height);
		}
	}

	/// <summary>
	///  Method to update the popup layout.
	/// </summary>
	/// <param name="mauiPopup">An instance of <see cref="Popup"/>.</param>
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	/// <param name="mauiContext">An instance of <see cref="IMauiContext"/>.</param>
	public static void SetLayout(this Popup mauiPopup, IPopup popup, IMauiContext? mauiContext)
	{
		ArgumentNullException.ThrowIfNull(mauiContext);
		ArgumentNullException.ThrowIfNull(popup.Content);

		var popupParent = mauiContext.GetPlatformWindow();
		popup.Content.Measure(double.PositiveInfinity, double.PositiveInfinity);
		var contentSize = popup.Content.ToPlatform(mauiContext).DesiredSize;
		var popupParentFrame = popupParent.Bounds;

		var isFlowDirectionRightToLeft = popup.Content?.FlowDirection == Microsoft.Maui.FlowDirection.RightToLeft;
		var horizontalOptionsPositiveNegativeMultiplier = isFlowDirectionRightToLeft ? -1 : 1;

		var verticalOptions = popup.VerticalOptions;
		var horizontalOptions = popup.HorizontalOptions;
		if (popup.Anchor is not null)
		{
			mauiPopup.DesiredPlacement = PopupPlacementMode.Top;
		}
		else if (IsTopLeft(verticalOptions, horizontalOptions))
		{
			mauiPopup.DesiredPlacement = PopupPlacementMode.TopEdgeAlignedLeft;
			mauiPopup.HorizontalOffset = (popupParentFrame.Width - popupParentFrame.Width * horizontalOptionsPositiveNegativeMultiplier) / 2;
			mauiPopup.VerticalOffset = 0;
		}
		else if (IsTop(verticalOptions, horizontalOptions))
		{
			mauiPopup.DesiredPlacement = PopupPlacementMode.Top;
			mauiPopup.HorizontalOffset = (popupParentFrame.Width - contentSize.Width * horizontalOptionsPositiveNegativeMultiplier) / 2;
			mauiPopup.VerticalOffset = 0;
		}
		else if (IsTopRight(verticalOptions, horizontalOptions))
		{
			mauiPopup.DesiredPlacement = PopupPlacementMode.TopEdgeAlignedRight;
			mauiPopup.HorizontalOffset = (popupParentFrame.Width + popupParentFrame.Width * horizontalOptionsPositiveNegativeMultiplier) / 2 - contentSize.Width * horizontalOptionsPositiveNegativeMultiplier;
			mauiPopup.VerticalOffset = 0;
		}
		else if (IsRight(verticalOptions, horizontalOptions))
		{
			mauiPopup.DesiredPlacement = PopupPlacementMode.Right;
			mauiPopup.HorizontalOffset = (popupParentFrame.Width + popupParentFrame.Width * horizontalOptionsPositiveNegativeMultiplier) / 2 - contentSize.Width * horizontalOptionsPositiveNegativeMultiplier;
			mauiPopup.VerticalOffset = (popupParentFrame.Height - contentSize.Height) / 2;
		}
		else if (IsBottomRight(verticalOptions, horizontalOptions))
		{
			mauiPopup.DesiredPlacement = PopupPlacementMode.BottomEdgeAlignedRight;
			mauiPopup.HorizontalOffset = (popupParentFrame.Width + popupParentFrame.Width * horizontalOptionsPositiveNegativeMultiplier) / 2 - contentSize.Width * horizontalOptionsPositiveNegativeMultiplier;
			mauiPopup.VerticalOffset = popupParentFrame.Height - contentSize.Height;
		}
		else if (IsBottom(verticalOptions, horizontalOptions))
		{
			mauiPopup.DesiredPlacement = PopupPlacementMode.Bottom;
			mauiPopup.HorizontalOffset = (popupParentFrame.Width - contentSize.Width * horizontalOptionsPositiveNegativeMultiplier) / 2;
			mauiPopup.VerticalOffset = popupParentFrame.Height - contentSize.Height;
		}
		else if (IsBottomLeft(verticalOptions, horizontalOptions))
		{
			mauiPopup.DesiredPlacement = PopupPlacementMode.BottomEdgeAlignedLeft;
			mauiPopup.HorizontalOffset = (popupParentFrame.Width - popupParentFrame.Width * horizontalOptionsPositiveNegativeMultiplier) / 2;
			mauiPopup.VerticalOffset = popupParentFrame.Height - contentSize.Height;
		}
		else if (IsLeft(verticalOptions, horizontalOptions))
		{
			mauiPopup.DesiredPlacement = PopupPlacementMode.Left;
			mauiPopup.HorizontalOffset = (popupParentFrame.Width - popupParentFrame.Width * horizontalOptionsPositiveNegativeMultiplier) / 2;
			mauiPopup.VerticalOffset = (popupParentFrame.Height - contentSize.Height) / 2;
		}
		else if (IsCenter(verticalOptions, horizontalOptions))
		{
			mauiPopup.DesiredPlacement = PopupPlacementMode.Auto;
			mauiPopup.HorizontalOffset = (popupParentFrame.Width - contentSize.Width * horizontalOptionsPositiveNegativeMultiplier) / 2;
			mauiPopup.VerticalOffset = (popupParentFrame.Height - contentSize.Height) / 2;
		}
		else if (IsFillLeft(verticalOptions, horizontalOptions))
		{
			mauiPopup.DesiredPlacement = PopupPlacementMode.Auto;
			mauiPopup.HorizontalOffset = (popupParentFrame.Width - popupParentFrame.Width * horizontalOptionsPositiveNegativeMultiplier) / 2;
			mauiPopup.VerticalOffset = (popupParentFrame.Height - contentSize.Height) / 2;
		}
		else if (IsFillCenter(verticalOptions, horizontalOptions))
		{
			mauiPopup.DesiredPlacement = PopupPlacementMode.Auto;
			mauiPopup.HorizontalOffset = (popupParentFrame.Width - contentSize.Width * horizontalOptionsPositiveNegativeMultiplier) / 2;
			mauiPopup.VerticalOffset = (popupParentFrame.Height - contentSize.Height) / 2;
		}
		else if (IsFillRight(verticalOptions, horizontalOptions))
		{
			mauiPopup.DesiredPlacement = PopupPlacementMode.Auto;
			mauiPopup.HorizontalOffset = (popupParentFrame.Width + popupParentFrame.Width * horizontalOptionsPositiveNegativeMultiplier) / 2 - contentSize.Width * horizontalOptionsPositiveNegativeMultiplier;
			mauiPopup.VerticalOffset = (popupParentFrame.Height - contentSize.Height) / 2;
		}
		else if (IsTopFill(verticalOptions, horizontalOptions))
		{
			mauiPopup.DesiredPlacement = PopupPlacementMode.Auto;
			mauiPopup.HorizontalOffset = (popupParentFrame.Width - contentSize.Width * horizontalOptionsPositiveNegativeMultiplier) / 2;
			mauiPopup.VerticalOffset = 0;
		}
		else if (IsCenterFill(verticalOptions, horizontalOptions))
		{
			mauiPopup.DesiredPlacement = PopupPlacementMode.Auto;
			mauiPopup.HorizontalOffset = (popupParentFrame.Width - contentSize.Width * horizontalOptionsPositiveNegativeMultiplier) / 2;
			mauiPopup.VerticalOffset = (popupParentFrame.Height - contentSize.Height) / 2;
		}
		else if (IsBottomFill(verticalOptions, horizontalOptions))
		{
			mauiPopup.DesiredPlacement = PopupPlacementMode.Auto;
			mauiPopup.HorizontalOffset = (popupParentFrame.Width - contentSize.Width * horizontalOptionsPositiveNegativeMultiplier) / 2;
			mauiPopup.VerticalOffset = popupParentFrame.Height - contentSize.Height;
		}
		else if (IsFill(verticalOptions, horizontalOptions))
		{
			mauiPopup.DesiredPlacement = PopupPlacementMode.Auto;
			mauiPopup.HorizontalOffset = (popupParentFrame.Width - contentSize.Width * horizontalOptionsPositiveNegativeMultiplier) / 2;
			mauiPopup.VerticalOffset = (popupParentFrame.Height - contentSize.Height) / 2;
		}
		else
		{
			mauiPopup.DesiredPlacement = PopupPlacementMode.Auto;
			mauiPopup.HorizontalOffset = (popupParentFrame.Width - contentSize.Width * horizontalOptionsPositiveNegativeMultiplier) / 2;
			mauiPopup.VerticalOffset = (popupParentFrame.Height - contentSize.Height) / 2;
		}

		static bool IsTopLeft(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Start && horizontalOptions == LayoutAlignment.Start;
		static bool IsTop(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Start && horizontalOptions == LayoutAlignment.Center;
		static bool IsTopRight(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Start && horizontalOptions == LayoutAlignment.End;
		static bool IsRight(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Center && horizontalOptions == LayoutAlignment.End;
		static bool IsBottomRight(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.End && horizontalOptions == LayoutAlignment.End;
		static bool IsBottom(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.End && horizontalOptions == LayoutAlignment.Center;
		static bool IsBottomLeft(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.End && horizontalOptions == LayoutAlignment.Start;
		static bool IsLeft(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Center && horizontalOptions == LayoutAlignment.Start;
		static bool IsCenter(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Center && horizontalOptions == LayoutAlignment.Center;
		static bool IsFillLeft(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Fill && horizontalOptions == LayoutAlignment.Start;
		static bool IsFillCenter(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Fill && horizontalOptions == LayoutAlignment.Center;
		static bool IsFillRight(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Fill && horizontalOptions == LayoutAlignment.End;
		static bool IsTopFill(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Start && horizontalOptions == LayoutAlignment.Fill;
		static bool IsCenterFill(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Center && horizontalOptions == LayoutAlignment.Fill;
		static bool IsBottomFill(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.End && horizontalOptions == LayoutAlignment.Fill;
		static bool IsFill(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Fill && horizontalOptions == LayoutAlignment.Fill;
	}
}