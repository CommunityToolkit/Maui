using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
//using Specific = CommunityToolkit.Maui.PlatformConfiguration.WindowsSpecific.PopUp;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;
using UWPThickness = Microsoft.UI.Xaml.Thickness;
using CommunityToolkit.Maui.Core.Platform;
using XamlStyle = Microsoft.UI.Xaml.Style;

namespace CommunityToolkit.Core.Platform;

public static class PopupExtensions
{
	const double defaultBorderThickness = 2;
	const double defaultSize = 600;

	public static void SetColor(this PopupRenderer flyout, IBasePopup basePopup)
	{
		ArgumentNullException.ThrowIfNull(basePopup.Content);

		var color = basePopup.Color ?? Colors.Transparent;
		var view = (View)basePopup.Content;
		if (view.BackgroundColor is null && flyout.Control is not null)
		{
			flyout.Control.Background = color.ToNative();
		}
	}

	public static void SetSize(this PopupRenderer flyout, IBasePopup basePopup)
	{
		if (flyout.Control is null)
		{
			return;
		}

		var standardSize = new Size { Width = defaultSize, Height = defaultSize / 2 };

		var currentSize = basePopup.Size != default ? basePopup.Size : standardSize;
		flyout.Control.Width = currentSize.Width;
		flyout.Control.Height = currentSize.Height;
		var doubleDefaultBordThickness = defaultBorderThickness * 2;

		flyout.FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.MinHeightProperty, currentSize.Height + doubleDefaultBordThickness));
		flyout.FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.MinWidthProperty, currentSize.Width + doubleDefaultBordThickness));
		flyout.FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.MaxHeightProperty, currentSize.Height + doubleDefaultBordThickness));
		flyout.FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.MaxWidthProperty, currentSize.Width + doubleDefaultBordThickness));
	}

	public static void SetLayout(this PopupRenderer flyout, IBasePopup basePopup)
	{
		flyout.LightDismissOverlayMode = LightDismissOverlayMode.On;

		if (basePopup is not null)
		{
			SetDialogPosition(basePopup.VerticalOptions, basePopup.HorizontalOptions, flyout, basePopup);
		}

		static void SetDialogPosition(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions, PopupRenderer flyout, IBasePopup basePopup)
		{
			if (IsTopLeft())
			{
				flyout.Placement = FlyoutPlacementMode.TopEdgeAlignedLeft;
			}
			else if (IsTop())
			{
				flyout.Placement = FlyoutPlacementMode.Top;
			}
			else if (IsTopRight())
			{
				flyout.Placement = FlyoutPlacementMode.TopEdgeAlignedRight;
			}
			else if (IsRight())
			{
				flyout.Placement = FlyoutPlacementMode.Right;
			}
			else if (IsBottomRight())
			{
				flyout.Placement = FlyoutPlacementMode.BottomEdgeAlignedRight;
			}
			else if (IsBottom())
			{
				flyout.Placement = FlyoutPlacementMode.Bottom;
			}
			else if (IsBottomLeft())
			{
				flyout.Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft;
			}
			else if (IsLeft())
			{
				flyout.Placement = FlyoutPlacementMode.Left;
			}
			else if (basePopup.Anchor is null)
			{
				flyout.Placement = FlyoutPlacementMode.Full;
			}
			else
			{
				flyout.Placement = FlyoutPlacementMode.Top;
			}

			bool IsTopLeft() => verticalOptions == LayoutAlignment.Start && horizontalOptions == LayoutAlignment.Start;
			bool IsTop() => verticalOptions == LayoutAlignment.Start && horizontalOptions == LayoutAlignment.Center;
			bool IsTopRight() => verticalOptions == LayoutAlignment.Start && horizontalOptions == LayoutAlignment.End;
			bool IsRight() => verticalOptions == LayoutAlignment.Center && horizontalOptions == LayoutAlignment.End;
			bool IsBottomRight() => verticalOptions == LayoutAlignment.End && horizontalOptions == LayoutAlignment.End;
			bool IsBottom() => verticalOptions == LayoutAlignment.End && horizontalOptions == LayoutAlignment.Center;
			bool IsBottomLeft() => verticalOptions == LayoutAlignment.End && horizontalOptions == LayoutAlignment.Start;
			bool IsLeft() => verticalOptions == LayoutAlignment.Center && horizontalOptions == LayoutAlignment.Start;
		}
	}


	//TODO: Move to specific handler
	public static void SetBorderColor(this PopupRenderer flyout, IBasePopup basePopup)
	{
		flyout.FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.PaddingProperty, 0));
		flyout.FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.BorderThicknessProperty, new UWPThickness(defaultBorderThickness)));

		var borderColor = Colors.Red; // Specific.GetBorderColor((BindableObject)basePopup);
		if (borderColor == default(Color))
		{
			flyout.FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.BorderBrushProperty, Color.FromHex("#2e6da0").ToNative()));
		}
		else
		{
			flyout.FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.BorderBrushProperty, borderColor.ToNative()));
		}
	}

}
