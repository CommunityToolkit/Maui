using System;
using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Specific = CommunityToolkit.Maui.PlatformConfiguration.WindowsSpecific.PopUp;
using UWPThickness = Microsoft.UI.Xaml.Thickness;
using XamlStyle = Microsoft.UI.Xaml.Style;

namespace CommunityToolkit.Maui.Views;

public class PopupRenderer : Flyout
{
	const double defaultBorderThickness = 2;
	const double defaultSize = 600;
	bool isDisposed = false;
	XamlStyle? flyoutStyle;
	XamlStyle? panelStyle;

	public BasePopup? Element { get; private set; }

	public PopupRenderer()
	{
	}

	void SetElement(IBasePopup element)
	{
		if (element == null)
		{
			throw new ArgumentNullException(nameof(element));
		}

		if (element is not BasePopup popup)
		{
			throw new ArgumentNullException("Element is not of type " + typeof(BasePopup), nameof(element));
		}

		Element = popup;
		CreateControl();
	}

	void CreateControl()
	{
		if (Control == null && Element?.Content != null)
		{
			Control = new ViewToRendererConverter.WrapperControl(Element.Content);
			Content = Control;
		}
	}

	void InitializeStyles()
	{
		flyoutStyle = new XamlStyle { TargetType = typeof(FlyoutPresenter) };
		panelStyle = new XamlStyle { TargetType = typeof(Panel) };
	}

	protected virtual void OnElementChanged(ElementChangedEventArgs<BasePopup?> e)
	{
		if (e.NewElement != null && !isDisposed)
		{
			ConfigureControl();
			Show();
		}

		ElementChanged?.Invoke(this, new VisualElementChangedEventArgs(e.OldElement, e.NewElement));
	}

	protected virtual void OnElementPropertyChanged(object? sender, PropertyChangedEventArgs args)
	{
		if (args.PropertyName == BasePopup.VerticalOptionsProperty.PropertyName ||
			args.PropertyName == BasePopup.HorizontalOptionsProperty.PropertyName ||
			args.PropertyName == BasePopup.SizeProperty.PropertyName ||
			args.PropertyName == BasePopup.ColorProperty.PropertyName)
		{
			ConfigureControl();
		}
	}

	void ConfigureControl()
	{
		InitializeStyles();
		SetEvents();
		SetColor();
		SetBorderColor();
		SetSize();
		SetLayout();
		ApplyStyles();
	}

	void SetEvents()
	{
		if (Element?.IsLightDismissEnabled is true)
		{
			Closing += OnClosing;
		}

		if (Element != null)
		{
			Element.Dismissed += OnDismissed;
		}

		Opened += OnOpened;
	}

	void SetSize()
	{
		_ = Element ?? throw new InvalidOperationException($"{nameof(Element)} cannot be null");
		_ = Control ?? throw new InvalidOperationException($"{nameof(Control)} cannot be null");
		_ = flyoutStyle ?? throw new InvalidOperationException($"{nameof(flyoutStyle)} cannot be null");
		var standardSize = new Size { Width = defaultSize, Height = defaultSize / 2 };

		var currentSize = Element.Size != default(Size) ? Element.Size : standardSize;
		Control.Width = currentSize.Width;
		Control.Height = currentSize.Height;

		flyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.MinHeightProperty, currentSize.Height + (defaultBorderThickness * 2)));
		flyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.MinWidthProperty, currentSize.Width + (defaultBorderThickness * 2)));
		flyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.MaxHeightProperty, currentSize.Height + (defaultBorderThickness * 2)));
		flyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.MaxWidthProperty, currentSize.Width + (defaultBorderThickness * 2)));
	}

	void SetLayout()
	{
		LightDismissOverlayMode = LightDismissOverlayMode.On;

		if (Element is not null)
		{
			SetDialogPosition(Element.VerticalOptions, Element.HorizontalOptions);
		}
	}

	void SetBorderColor()
	{
		_ = flyoutStyle ?? throw new NullReferenceException();

		flyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.PaddingProperty, 0));
		flyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.BorderThicknessProperty, new UWPThickness(defaultBorderThickness)));

		if (Element == null)
		{
			//Log.Warning("warning", "The PopUpView is null.");
			return;
		}

		var borderColor = Specific.GetBorderColor(Element);
		if (borderColor == default(Color))
		{
			flyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.BorderBrushProperty, Color.FromHex("#2e6da0").ToNative()));
		}
		else
		{
			flyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.BorderBrushProperty, borderColor.ToNative()));
		}
	}

	void SetColor()
	{
		_ = Element?.Content ?? throw new NullReferenceException();
		_ = panelStyle ?? throw new NullReferenceException();
		_ = flyoutStyle ?? throw new NullReferenceException();

		if (Element.Content.BackgroundColor == default(Color))
		{
			panelStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(Panel.BackgroundProperty, Element.Color.ToWindowsColor()));
		}

		flyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.BackgroundProperty, Element.Color.ToWindowsColor()));

#if UWP_18362
			if (Element.Color == Color.Transparent)
				flyoutStyle.Setters.Add(new Windows.UI.Xaml.Setter(FlyoutPresenter.IsDefaultShadowEnabledProperty, false));
#endif
	}

	void ApplyStyles()
	{
		_ = Control ?? throw new NullReferenceException();
		Control.Style = panelStyle;
		FlyoutPresenterStyle = flyoutStyle;
	}

	void Show()
	{
		if (Element?.Anchor is not null)
		{
			var anchor = Platform.GetRenderer(Element.Anchor).ContainerElement;
			FlyoutBase.SetAttachedFlyout(anchor, this);
			FlyoutBase.ShowAttachedFlyout(anchor);
		}
		else
		{
			var frameworkElement = Platform.GetRenderer(Element?.Parent as VisualElement)?.ContainerElement;
			FlyoutBase.SetAttachedFlyout(frameworkElement, this);
			FlyoutBase.ShowAttachedFlyout(frameworkElement);
		}

		Element?.OnOpened();
	}

	void SetDialogPosition(LayoutOptions verticalOptions, LayoutOptions horizontalOptions)
	{
		if (IsTopLeft())
		{
			Placement = FlyoutPlacementMode.TopEdgeAlignedLeft;
		}
		else if (IsTop())
		{
			Placement = FlyoutPlacementMode.Top;
		}
		else if (IsTopRight())
		{
			Placement = FlyoutPlacementMode.TopEdgeAlignedRight;
		}
		else if (IsRight())
		{
			Placement = FlyoutPlacementMode.Right;
		}
		else if (IsBottomRight())
		{
			Placement = FlyoutPlacementMode.BottomEdgeAlignedRight;
		}
		else if (IsBottom())
		{
			Placement = FlyoutPlacementMode.Bottom;
		}
		else if (IsBottomLeft())
		{
			Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft;
		}
		else if (IsLeft())
		{
			Placement = FlyoutPlacementMode.Left;
		}
		else if (Element != null && Element.Anchor == null)
		{
			Placement = FlyoutPlacementMode.Full;
		}
		else
		{
			Placement = FlyoutPlacementMode.Top;
		}

		bool IsTopLeft() => verticalOptions.Alignment == LayoutAlignment.Start && horizontalOptions.Alignment == LayoutAlignment.Start;
		bool IsTop() => verticalOptions.Alignment == LayoutAlignment.Start && horizontalOptions.Alignment == LayoutAlignment.Center;
		bool IsTopRight() => verticalOptions.Alignment == LayoutAlignment.Start && horizontalOptions.Alignment == LayoutAlignment.End;
		bool IsRight() => verticalOptions.Alignment == LayoutAlignment.Center && horizontalOptions.Alignment == LayoutAlignment.End;
		bool IsBottomRight() => verticalOptions.Alignment == LayoutAlignment.End && horizontalOptions.Alignment == LayoutAlignment.End;
		bool IsBottom() => verticalOptions.Alignment == LayoutAlignment.End && horizontalOptions.Alignment == LayoutAlignment.Center;
		bool IsBottomLeft() => verticalOptions.Alignment == LayoutAlignment.End && horizontalOptions.Alignment == LayoutAlignment.Start;
		bool IsLeft() => verticalOptions.Alignment == LayoutAlignment.Center && horizontalOptions.Alignment == LayoutAlignment.Start;
	}

	SizeRequest GetDesiredSize(double widthConstraint, double heightConstraint)
	{
		if (isDisposed || Control == null)
		{
			return default(SizeRequest);
		}

		var constraint = new Windows.Foundation.Size(widthConstraint, heightConstraint);
		Control.Measure(constraint);

		var size = new Size(Math.Ceiling(Control.DesiredSize.Width), Math.Ceiling(Control.DesiredSize.Height));
		return new SizeRequest(size);
	}

	// The UWP PopupRenderer needs to maintain it's own version of
	// `isOpen` because our popup lifecycle differs slightly from
	// the UWP version of `IsOpen`. Without this variable usages
	// in OnDismissed and OnClosing will not work as expected.
	bool isOpen = true;

	void OnDismissed(object? sender, PopupDismissedEventArgs e)
	{
		if (!isOpen)
		{
			return;
		}

		isOpen = false;
		Hide();
	}

	void OnClosing(object? sender, object e)
	{
		if (isOpen && Element?.IsLightDismissEnabled is true)
		{
			Element.LightDismiss();
		}
	}

	void OnOpened(object sender, object e) =>
		isOpen = true;

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!isDisposed && disposing)
		{
			if (Element != null)
			{
				Element.Dismissed -= OnDismissed;
			}

			if (Control != null)
			{
				Control.CleanUp();
			}

			Element = null;
			Control = null;

			Closing -= OnClosing;
			Opened -= OnOpened;
		}

		isDisposed = true;
	}
}
