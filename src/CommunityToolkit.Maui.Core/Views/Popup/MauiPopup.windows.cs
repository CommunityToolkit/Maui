using CommunityToolkit.Maui.Core.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;
using WindowsThickness = Microsoft.UI.Xaml.Thickness;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The native implementation of Popup control.
/// </summary>
public class MauiPopup : FrameworkElement
{
	const double defaultBorderThickness = 0;
	const double defaultSize = 600;
	readonly IMauiContext mauiContext;

	/// <summary>
	/// Constructor of <see cref="MauiPopup"/>.
	/// </summary>
	/// <param name="mauiContext">An instance of <see cref="IMauiContext"/>.</param>
	/// <exception cref="ArgumentNullException">If <paramref name="mauiContext"/> is null an exception will be thrown. </exception>
	public MauiPopup(IMauiContext mauiContext)
	{
		this.mauiContext = mauiContext ?? throw new ArgumentNullException(nameof(mauiContext));
		var style = new Style(typeof(Popup));
		style.BasedOn = new Style(typeof(FrameworkElement));
		Control = new Popup
		{
			Style = style
		};
	}

	/// <summary>
	/// An instance of the <see cref="IPopup"/>.
	/// </summary>
	public IPopup? VirtualView { get; private set; }

	internal Popup Control { get; set; }

	/// <summary>
	/// Method to initialize the native implementation.
	/// </summary>
	/// <param name="element">An instance of <see cref="IPopup"/>.</param>
	public void SetElement(IPopup element)
	{
		VirtualView = element;
		Control.Opened += OnOpened;
	}

	/// <summary>
	/// Method to setup the Content of the Popup
	/// </summary>
	public void SetUpPlatformView()
	{
		ConfigureControl();
	}

	/// <summary>
	/// Method to update all the values of the Popup Control.
	/// </summary>
	public void ConfigureControl()
	{
		if (VirtualView is null)
		{
			return;
		}

		if (VirtualView?.Content is not null && VirtualView.Handler is PopupHandler handler)
		{
			Control.Child = handler.VirtualView.Content?.ToPlatform(mauiContext);
		}

		SetFlyoutColor();
		SetSize();
		SetLayout();
	}

	/// <summary>
	/// Method to show the Popup.
	/// </summary>
	public void Show()
	{
		if (VirtualView is null)
		{
			return;
		}

		ArgumentNullException.ThrowIfNull(VirtualView.Parent);

		if (VirtualView.Anchor is not null)
		{
			var anchor = VirtualView.Anchor.ToPlatform(mauiContext);
			Control.PlacementTarget = anchor;
		}
		
		Control.XamlRoot = VirtualView.Parent.ToPlatform(mauiContext).XamlRoot;
		Control.IsOpen = true;
		VirtualView.OnOpened();
	}

	/// <summary>
	/// Method to CleanUp the resources of the <see cref="MauiPopup"/>.
	/// </summary>
	public void CleanUp()
	{
		Control.Opened -= OnOpened;
		Control.IsOpen = false;

		VirtualView = null;
	}

	void SetSize()
	{
		_ = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} cannot be null.");
		
		var standardSize = new Size { Width = defaultSize, Height = defaultSize / 2 };

		var currentSize = VirtualView.Size != default ? VirtualView.Size : standardSize;

		if (VirtualView.Content is not null && VirtualView.Size == default)
		{
			var content = VirtualView.Content;
			// There are some situations when the Width and Height values will be NaN
			// normally when the dev doesn't set the HeightRequest and WidthRequest
			// and we can't use comparasion on those, so the only to prevent the application to crash
			// is using this try/catch
			try
			{
				currentSize = new Size(content.Width, content.Height);
			}
			catch (ArgumentException)
			{
			}
		}

		Control.Width = currentSize.Width;
		Control.Height = currentSize.Height;
		Control.MinWidth = Control.MaxWidth = currentSize.Width + (defaultBorderThickness * 2);
		Control.MinHeight = Control.MaxHeight = currentSize.Height + (defaultBorderThickness * 2);
		if (VirtualView.Parent is IView parent && VirtualView.Anchor is null)
		{
			VirtualView.Content?.Measure(double.PositiveInfinity, double.PositiveInfinity);
			var contentSize = VirtualView.Content?.ToPlatform(mauiContext).DesiredSize ?? Windows.Foundation.Size.Empty;
			Control.HorizontalOffset = (parent.Frame.Width - contentSize.Width) / 2;
			Control.VerticalOffset = (parent.Frame.Height - contentSize.Height) / 2;
		}
		
	}

	void SetLayout()
	{
		if (VirtualView is not null)
		{
			this.SetDialogPosition(VirtualView.VerticalOptions, VirtualView.HorizontalOptions);
		}
	}

	void SetFlyoutColor()
	{
		_ = VirtualView?.Content ?? throw new NullReferenceException(nameof(IPopup.Content));

		var color = VirtualView.Color ?? Colors.Transparent;

		//Control.Style.Setters.Add(new Microsoft.UI.Xaml.Setter(Popup.co.BackgroundProperty, color.ToWindowsColor()));

		if (Equals(VirtualView.Color, Colors.Transparent))
		{
		//	Control.Style.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.IsDefaultShadowEnabledProperty, false));
		}

		////Configure border
		
		//Control.Style.Setters.Add(new Microsoft.UI.Xaml.Setter(Microsoft.UI.Xaml.Controls.Control.PaddingProperty, 0));
		//Control.Style.Setters.Add(new Microsoft.UI.Xaml.Setter(Microsoft.UI.Xaml.Controls.Control.BorderThicknessProperty, new WindowsThickness(defaultBorderThickness)));
		//Control.Style.Setters.Add(new Microsoft.UI.Xaml.Setter(Microsoft.UI.Xaml.Controls.Control.BorderBrushProperty, Color.FromArgb("#2e6da0").ToWindowsColor()));
	}

	void SetDialogPosition(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions)
	{
		if (IsTopLeft(verticalOptions, horizontalOptions))
		{
			Control.DesiredPlacement = PopupPlacementMode.TopEdgeAlignedLeft;
		}
		else if (IsTop(verticalOptions, horizontalOptions))
		{
			Control.DesiredPlacement = PopupPlacementMode.Top;
		}
		else if (IsTopRight(verticalOptions, horizontalOptions))
		{
			Control.DesiredPlacement = PopupPlacementMode.TopEdgeAlignedRight;
		}
		else if (IsRight(verticalOptions, horizontalOptions))
		{
			Control.DesiredPlacement = PopupPlacementMode.Right;
		}
		else if (IsBottomRight(verticalOptions, horizontalOptions))
		{
			Control.DesiredPlacement = PopupPlacementMode.BottomEdgeAlignedRight;
		}
		else if (IsBottom(verticalOptions, horizontalOptions))
		{
			Control.DesiredPlacement = PopupPlacementMode.Bottom;
		}
		else if (IsBottomLeft(verticalOptions, horizontalOptions))
		{
			Control.DesiredPlacement = PopupPlacementMode.BottomEdgeAlignedLeft;
		}
		else if (IsLeft(verticalOptions, horizontalOptions))
		{
			Control.DesiredPlacement = PopupPlacementMode.Left;
		}
		else if (VirtualView is not null && VirtualView.Anchor is null)
		{
			Control.DesiredPlacement = PopupPlacementMode.Auto;
		}
		else
		{
			Control.DesiredPlacement = PopupPlacementMode.Top;
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

	void OnOpened(object? sender, object e)
	{
		var isLightDismissEnabled = VirtualView?.CanBeDismissedByTappingOutsideOfPopup is true;
		Control.IsLightDismissEnabled = isLightDismissEnabled;
		Control.LightDismissOverlayMode = isLightDismissEnabled ? LightDismissOverlayMode.On : LightDismissOverlayMode.Off;
		Control.ManipulationMode = ManipulationModes.None;
		
		if (!Control.IsOpen && isLightDismissEnabled)
		{
			VirtualView?.Handler?.Invoke(nameof(IPopup.OnDismissedByTappingOutsideOfPopup));
		}
	}
}