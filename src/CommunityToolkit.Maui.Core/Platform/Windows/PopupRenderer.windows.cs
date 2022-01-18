using CommunityToolkit.Core.Extensions.Workarounds;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;
//using Specific = CommunityToolkit.Maui.PlatformConfiguration.WindowsSpecific.PopUp;
using UWPThickness = Microsoft.UI.Xaml.Thickness;
using XamlStyle = Microsoft.UI.Xaml.Style;

namespace CommunityToolkit.Core.Platform;


public class PopupRenderer : Flyout
{
	const double defaultBorderThickness = 2;
	const double defaultSize = 600;
	readonly IMauiContext mauiContext;
	bool isDisposed = false;

	internal XamlStyle FlyoutStyle { get; set; } = new XamlStyle(typeof(FlyoutPresenter));
	internal XamlStyle PanelStyle { get; set; } = new XamlStyle(typeof(Panel));

	internal WrapperControl? Control { get; set; }
	public IBasePopup? VirtualView { get; private set; }

	public PopupRenderer(IMauiContext mauiContext)
	{
		this.mauiContext = mauiContext;
	}

	public void SetElement(IBasePopup element)
	{
		VirtualView = element;

		var result = this.Content as FlyoutPresenter;

		CreateControl();
		ConfigureControl();
		//Show();
	}

	void CreateControl()
	{
		if (Control == null && VirtualView?.Content != null)
		{
			Control = new WrapperControl((View)VirtualView.Content, mauiContext);
			Content = Control;
		}
	}

	void ConfigureControl()
	{
		//InitializeStyles();
		SetEvents();
		SetColor();
		SetBorderColor();
		SetSize();
		//SetLayout();
		ApplyStyles();
	}

	void SetEvents()
	{
		if (VirtualView?.IsLightDismissEnabled is true)
		{
			//Closing += OnClosing;
		}

		if (VirtualView != null)
		{
			//Element.Dismissed += OnDismissed;
		}

		Opened += OnOpened;
	}

	void SetSize()
	{
		_ = VirtualView ?? throw new InvalidOperationException($"{nameof(Element)} cannot be null");
		_ = Control ?? throw new InvalidOperationException($"{nameof(Element)} cannot be null");
		var standardSize = new Size { Width = defaultSize, Height = defaultSize / 2 };

		var currentSize = VirtualView.Size != default(Size) ? VirtualView.Size : standardSize;
		Control.Width = currentSize.Width;
		Control.Height = currentSize.Height;

		FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.MinHeightProperty, currentSize.Height + (defaultBorderThickness * 2)));
		FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.MinWidthProperty, currentSize.Width + (defaultBorderThickness * 2)));
		FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.MaxHeightProperty, currentSize.Height + (defaultBorderThickness * 2)));
		FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.MaxWidthProperty, currentSize.Width + (defaultBorderThickness * 2)));
	}

	void SetLayout()
	{
		this.Closing += (sender, e) =>
		  {
			  e.Cancel = true;
		  };

		LightDismissOverlayMode = LightDismissOverlayMode.On;
		if (VirtualView is not null)
		{
			this.SetDialogPosition(VirtualView.VerticalOptions, VirtualView.HorizontalOptions);
		}
	}

	void SetBorderColor()
	{
		_ = FlyoutStyle ?? throw new NullReferenceException();

		FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.PaddingProperty, 0));
		FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.BorderThicknessProperty, new UWPThickness(defaultBorderThickness)));

		if (VirtualView is null)
		{
			//Log.Warning("warning", "The PopUpView is null.");
			return;
		}

		var borderColor = Colors.Red; // Specific.GetBorderColor(Element);
		if (borderColor == default(Color))
		{
			FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.BorderBrushProperty, Color.FromHex("#2e6da0").ToNative()));
		}
		else
		{
			FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.BorderBrushProperty, borderColor.ToNative()));
		}
	}

	void SetColor()
	{
		_ = VirtualView?.Content ?? throw new NullReferenceException();

		var color = VirtualView.Color ?? Colors.Transparent;

		FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.BackgroundProperty, color.ToWindowsColor()));

		if (VirtualView.Color == Colors.Transparent)
		{
			FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.IsDefaultShadowEnabledProperty, false));
		}
	}

	public void ApplyStyles()
	{
		ArgumentNullException.ThrowIfNull(Control);
		Control.Style = PanelStyle;
		FlyoutPresenterStyle = FlyoutStyle;
	}

	public void Show()
	{
		if (VirtualView is null)
		{
			return;
		}
		ArgumentNullException.ThrowIfNull(VirtualView.Parent);

		if (VirtualView?.Anchor is not null)
		{

			var anchor = Microsoft.Maui.Platform.HandlerExtensions.ToNative(VirtualView.Anchor, mauiContext);
			SetAttachedFlyout(anchor, this);
			ShowAttachedFlyout(anchor);
		}
		else
		{
			ArgumentNullException.ThrowIfNull(VirtualView);
			var frameworkElement = Microsoft.Maui.Platform.HandlerExtensions.ToNative(VirtualView.Parent, mauiContext);
			frameworkElement.ContextFlyout = this;
			SetAttachedFlyout(frameworkElement, this);
			ShowAttachedFlyout(frameworkElement);
		}

		VirtualView?.OnOpened();
	}

	void SetDialogPosition(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions)
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
		else if (VirtualView != null && VirtualView.Anchor == null)
		{
			Placement = FlyoutPlacementMode.Full;
		}
		else
		{
			Placement = FlyoutPlacementMode.Top;
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

	//SizeRequest GetDesiredSize(double widthConstraint, double heightConstraint)
	//{
	//	if (isDisposed || Control == null)
	//	{
	//		return default(SizeRequest);
	//	}

	//	var constraint = new Windows.Foundation.Size(widthConstraint, heightConstraint);
	//	Control.Measure(constraint);

	//	var size = new Size(Math.Ceiling(Control.DesiredSize.Width), Math.Ceiling(Control.DesiredSize.Height));
	//	return new SizeRequest(size);
	//}

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
		if (isOpen && VirtualView?.IsLightDismissEnabled is true)
		{
			VirtualView.LightDismiss();
		}
	}

	void OnOpened(object? sender, object e) =>
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
			if (VirtualView != null)
			{
				//	Element.Dismissed -= OnDismissed;
			}

			if (Control != null)
			{
				Control.CleanUp();
			}

			VirtualView = null;
			Control = null;

			//Closing -= OnClosing;
			Opened -= OnOpened;
		}

		isDisposed = true;
	}
}
