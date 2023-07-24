using CommunityToolkit.Maui.Core.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.Maui.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
			if (backgroundProperty != null)
			{
				backgroundProperty.SetValue(content, color.ToPlatform());
			}
		}
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="mauiPopup"></param>
	/// <param name="popup"></param>
	/// <param name="mauiContext"></param>
	public static void SetAnchor(this Popup mauiPopup, IPopup popup, IMauiContext? mauiContext)
	{
		ArgumentNullException.ThrowIfNull(mauiContext);
		mauiPopup.PlacementTarget = popup.Anchor?.ToPlatform(mauiContext);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="platformPopup"></param>
	/// <param name="popup"></param>
	/// <param name="mauiContext"></param>
	public static void ConfigureControl(this Popup platformPopup, IPopup popup, IMauiContext? mauiContext)
	{
		ArgumentNullException.ThrowIfNull(mauiContext);
		if (popup.Content is not null && popup.Handler is PopupHandler handler)
		{
			platformPopup.Child = handler.VirtualView.Content?.ToPlatform(mauiContext);
		}

		platformPopup.SetSize(popup);
		platformPopup.SetLayout(popup, mauiContext);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="platformPopup"></param>
	/// <param name="popup"></param>
	public static void SetSize(this Popup platformPopup, IPopup popup)
	{
		const double defaultBorderThickness = 0;
		const double defaultSize = 600;

		var standardSize = new Size { Width = defaultSize, Height = defaultSize / 2 };

		var currentSize = popup.Size != default ? popup.Size : standardSize;

		if (popup.Content is not null && popup.Size == default)
		{
			var content = popup.Content;
			// There are some situations when the Width and Height values will be NaN
			// normally when the dev doesn't set the HeightRequest and WidthRequest
			// and we can't use comparison on those, so the only to prevent the application to crash
			// is using this try/catch
			try
			{
				currentSize = new Size(content.Width, content.Height);
			}
			catch (ArgumentException)
			{
			}
		}

		if (popup.Parent is IView popupParent)
		{
			currentSize.Width = Math.Min(currentSize.Width, popupParent.Frame.Width);
			currentSize.Height = Math.Min(currentSize.Height, popupParent.Frame.Height);
		}
		
		platformPopup.Width = currentSize.Width;
		platformPopup.Height = currentSize.Height;
		platformPopup.MinWidth = platformPopup.MaxWidth = currentSize.Width + (defaultBorderThickness * 2);
		platformPopup.MinHeight = platformPopup.MaxHeight = currentSize.Height + (defaultBorderThickness * 2);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="platformPopup"></param>
	/// <param name="popup"></param>
	/// <param name="mauiContext"></param>
	public static void SetLayout(this Popup platformPopup, IPopup popup, IMauiContext? mauiContext)
	{
		ArgumentNullException.ThrowIfNull(mauiContext);
		var popupParent = popup.Parent as IView;
		popup.Content?.Measure(double.PositiveInfinity, double.PositiveInfinity);
		var contentSize = popup.Content?.ToPlatform(mauiContext).DesiredSize ?? Windows.Foundation.Size.Empty;
		var popupParentFrame = popupParent?.Frame ?? new Rect(0, 0, contentSize.Width, contentSize.Height);

		var verticalOptions = popup.VerticalOptions;
		var horizontalOptions = popup.HorizontalOptions;
		if (IsTopLeft(verticalOptions, horizontalOptions))
		{
			platformPopup.DesiredPlacement = PopupPlacementMode.TopEdgeAlignedLeft;
			platformPopup.HorizontalOffset = 0;
			platformPopup.VerticalOffset = 0;
		}
		else if (IsTop(verticalOptions, horizontalOptions))
		{
			platformPopup.DesiredPlacement = PopupPlacementMode.Top;
			platformPopup.HorizontalOffset = (popupParentFrame.Width - contentSize.Width) / 2;
			platformPopup.VerticalOffset = 0;
		}
		else if (IsTopRight(verticalOptions, horizontalOptions))
		{
			platformPopup.DesiredPlacement = PopupPlacementMode.TopEdgeAlignedRight;
			platformPopup.HorizontalOffset = (popupParentFrame.Width - contentSize.Width);
			platformPopup.VerticalOffset = 0;
		}
		else if (IsRight(verticalOptions, horizontalOptions))
		{
			platformPopup.DesiredPlacement = PopupPlacementMode.Right;
			platformPopup.HorizontalOffset = (popupParentFrame.Width - contentSize.Width);
			platformPopup.VerticalOffset = (popupParentFrame.Height - contentSize.Height) / 2;
		}
		else if (IsBottomRight(verticalOptions, horizontalOptions))
		{
			platformPopup.DesiredPlacement = PopupPlacementMode.BottomEdgeAlignedRight;
			platformPopup.HorizontalOffset = (popupParentFrame.Width - contentSize.Width);
			platformPopup.VerticalOffset = popupParentFrame.Height + contentSize.Height/2;
		}
		else if (IsBottom(verticalOptions, horizontalOptions))
		{
			platformPopup.DesiredPlacement = PopupPlacementMode.Bottom;
			platformPopup.HorizontalOffset = (popupParentFrame.Width - contentSize.Width) / 2;
			platformPopup.VerticalOffset = popupParentFrame.Height+contentSize.Height/2;
		}
		else if (IsBottomLeft(verticalOptions, horizontalOptions))
		{
			platformPopup.DesiredPlacement = PopupPlacementMode.BottomEdgeAlignedLeft;
			platformPopup.HorizontalOffset = 0;
			platformPopup.VerticalOffset = popupParentFrame.Height + contentSize.Height / 2;
		}
		else if (IsLeft(verticalOptions, horizontalOptions))
		{
			platformPopup.DesiredPlacement = PopupPlacementMode.Left;
			platformPopup.HorizontalOffset = 0;
			platformPopup.VerticalOffset = (popupParentFrame.Height - contentSize.Height) / 2;
		}
		else if (popup.Anchor is null)
		{
			platformPopup.DesiredPlacement = PopupPlacementMode.Auto;
			platformPopup.HorizontalOffset = (popupParentFrame.Width - contentSize.Width) / 2;
			platformPopup.VerticalOffset = (popupParentFrame.Height - contentSize.Height) / 2;
		}
		else
		{
			platformPopup.DesiredPlacement = PopupPlacementMode.Top;
			platformPopup.HorizontalOffset = (popupParentFrame.Width - contentSize.Width) / 2;
			platformPopup.VerticalOffset = 0;
		}

		static bool IsTopLeft(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Start && horizontalOptions == LayoutAlignment.Start;
		static bool IsTop(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Start && horizontalOptions == LayoutAlignment.Center;
		static bool IsTopRight(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Start && horizontalOptions == LayoutAlignment.End;
		static bool IsRight(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Center && horizontalOptions == LayoutAlignment.End;
		static bool IsBottomRight(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.End && horizontalOptions == LayoutAlignment.End;
		static bool IsBottom(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.End && horizontalOptions == LayoutAlignment.Center;
		static bool IsBottomLeft(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.End && horizontalOptions == LayoutAlignment.Start;
		static bool IsLeft(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Center && horizontalOptions == LayoutAlignment.Start;
	}
}