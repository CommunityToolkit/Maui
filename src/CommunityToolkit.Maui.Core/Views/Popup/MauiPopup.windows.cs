using CommunityToolkit.Maui.Core.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;
using WindowsThickness = Microsoft.UI.Xaml.Thickness;
using XamlStyle = Microsoft.UI.Xaml.Style;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The native implementation of Popup control.
/// </summary>
public class MauiPopup : Flyout
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
	}

	/// <summary>
	/// An instance of the <see cref="IPopup"/>.
	/// </summary>
	public IPopup? VirtualView { get; private set; }

	internal Panel? Control { get; set; }
	internal XamlStyle FlyoutStyle { get; private set; } = new(typeof(FlyoutPresenter));

	Action<Panel>? panelCleanUp;
	Func<PopupHandler, Panel?>? createControl;

	/// <summary>
	/// Method to initialize the native implementation.
	/// </summary>
	/// <param name="element">An instance of <see cref="IPopup"/>.</param>
	public void SetElement(IPopup element)
	{
		VirtualView = element;
		Closing += OnClosing;
	}

	/// <summary>
	/// Method to setup the Content of the Popup using a WrapperControl
	/// </summary>
	/// <param name="panelCleanUp">Action to be executed when the Handler discconect</param>
	/// <param name="createControl">Function to be executed during the create of the popup content</param>
	public void SetUpPlatformView(Action<Panel> panelCleanUp, Func<PopupHandler, Panel?> createControl)
	{
		ArgumentNullException.ThrowIfNull(panelCleanUp);
		ArgumentNullException.ThrowIfNull(createControl);

		this.panelCleanUp = panelCleanUp;
		this.createControl = createControl;

		CreateControl();
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
		FlyoutStyle = new(typeof(FlyoutPresenter));
		SetFlyoutColor();
		SetSize();
		SetLayout();
		ApplyStyles();
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
			SetAttachedFlyout(anchor, this);
			ShowAttachedFlyout(anchor);
		}
		else
		{
			var frameworkElement = VirtualView.Parent.ToPlatform(mauiContext);
			frameworkElement.ContextFlyout = this;
			SetAttachedFlyout(frameworkElement, this);
			ShowAttachedFlyout(frameworkElement);
		}
		VirtualView.OnOpened();
	}

	/// <summary>
	/// Method to CleanUp the resources of the <see cref="MauiPopup"/>.
	/// </summary>
	public void CleanUp()
	{
		Closing -= OnClosing;
		Hide();


		if (Control is not null)
		{
			panelCleanUp?.Invoke(Control);
		}

		VirtualView = null;
		Control = null;
	}

	void CreateControl()
	{
		if (Control is null && VirtualView?.Content is not null && createControl is not null && VirtualView.Handler is PopupHandler handler)
		{
			Control = createControl(handler);
			Content = Control;
		}
	}

	void SetSize()
	{
		_ = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} cannot be null");

		if (Control is null)
		{
			return;
		}

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

		FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.MinHeightProperty, currentSize.Height + (defaultBorderThickness * 2)));
		FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.MinWidthProperty, currentSize.Width + (defaultBorderThickness * 2)));
		FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.MaxHeightProperty, currentSize.Height + (defaultBorderThickness * 2)));
		FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.MaxWidthProperty, currentSize.Width + (defaultBorderThickness * 2)));
	}

	void SetLayout()
	{
		LightDismissOverlayMode = LightDismissOverlayMode.On;

		if (VirtualView is not null)
		{
			this.SetDialogPosition(VirtualView.VerticalOptions, VirtualView.HorizontalOptions);
		}
	}

	void SetFlyoutColor()
	{
		_ = VirtualView?.Content ?? throw new NullReferenceException(nameof(IPopup.Content));

		var color = VirtualView.Color ?? Colors.Transparent;

		FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.BackgroundProperty, color.ToWindowsColor()));

		if (VirtualView.Color == Colors.Transparent)
		{
			FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.IsDefaultShadowEnabledProperty, false));
		}

		//Configure border

		FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.PaddingProperty, 0));
		FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.BorderThicknessProperty, new WindowsThickness(defaultBorderThickness)));
		FlyoutStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(FlyoutPresenter.BorderBrushProperty, Color.FromArgb("#2e6da0").ToWindowsColor()));
	}

	void ApplyStyles()
	{
		if(Control is null)
		{
			return;
		}

		FlyoutPresenterStyle = FlyoutStyle;
	}

	void SetDialogPosition(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions)
	{
		if (IsTopLeft(verticalOptions, horizontalOptions))
		{
			Placement = FlyoutPlacementMode.TopEdgeAlignedLeft;
		}
		else if (IsTop(verticalOptions, horizontalOptions))
		{
			Placement = FlyoutPlacementMode.Top;
		}
		else if (IsTopRight(verticalOptions, horizontalOptions))
		{
			Placement = FlyoutPlacementMode.TopEdgeAlignedRight;
		}
		else if (IsRight(verticalOptions, horizontalOptions))
		{
			Placement = FlyoutPlacementMode.Right;
		}
		else if (IsBottomRight(verticalOptions, horizontalOptions))
		{
			Placement = FlyoutPlacementMode.BottomEdgeAlignedRight;
		}
		else if (IsBottom(verticalOptions, horizontalOptions))
		{
			Placement = FlyoutPlacementMode.Bottom;
		}
		else if (IsBottomLeft(verticalOptions, horizontalOptions))
		{
			Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft;
		}
		else if (IsLeft(verticalOptions, horizontalOptions))
		{
			Placement = FlyoutPlacementMode.Left;
		}
		else if (VirtualView is not null && VirtualView.Anchor is null)
		{
			Placement = FlyoutPlacementMode.Full;
		}
		else
		{
			Placement = FlyoutPlacementMode.Top;
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

	void OnClosing(object? sender, FlyoutBaseClosingEventArgs e)
	{
		var isLightDismissEnabled = VirtualView?.CanBeDismissedByTappingOutsideOfPopup is true;
		if (!isLightDismissEnabled)
		{
			e.Cancel = true;
		}

		if (IsOpen && isLightDismissEnabled)
		{
			VirtualView?.Handler?.Invoke(nameof(IPopup.OnDismissedByTappingOutsideOfPopup));
		}
	}
}
