using CommunityToolkit.Core.Extensions.Workarounds;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;
using XamlStyle = Microsoft.UI.Xaml.Style;
using WindowsThickness = Microsoft.UI.Xaml.Thickness;
using System.Diagnostics;

namespace CommunityToolkit.Core.Views;

/// <summary>
/// The navite implementation of Popup control.
/// </summary>
public class MauiPopup : Flyout
{
	const double defaultBorderThickness = 2;
	const double defaultSize = 600;
	readonly IMauiContext mauiContext;

	internal XamlStyle FlyoutStyle { get; set; } = new (typeof(FlyoutPresenter));
	internal WrapperControl? Control { get; set; }
	/// <summary>
	/// An instace of the <see cref="IPopup"/>.
	/// </summary>
	public IPopup? VirtualView { get; private set; }

	/// <summary>
	/// Constructor of <see cref="MauiPopup"/>.
	/// </summary>
	/// <param name="mauiContext">An instace of <see cref="IMauiContext"/>.</param>
	/// <exception cref="ArgumentNullException">If <paramref name="mauiContext"/> is null an exception will be thrown. </exception>
	public MauiPopup(IMauiContext mauiContext)
	{
		this.mauiContext = mauiContext ?? throw new ArgumentNullException(nameof(mauiContext));
	}

	/// <summary>
	/// Method to initialize the native implementation.
	/// </summary>
	/// <param name="element">An instance of <see cref="IPopup"/>.</param>
	public void SetElement(IPopup element)
	{
		VirtualView = element;

		CreateControl();
		ConfigureControl();
	}

	void CreateControl()
	{
		if (Control is null && VirtualView?.Content is not null)
		{
			Control = new WrapperControl((View)VirtualView.Content, mauiContext);
			Content = Control;
		}

		SetEvents();
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

		SetEvents();
		SetFlyoutColor();
		SetSize();
		SetLayout();
		ApplyStyles();
	}

	void SetEvents()
	{
		Closing += OnClosing;
	}

	void SetSize()
	{
		_ = VirtualView ?? throw new InvalidOperationException($"{nameof(Element)} cannot be null");
		_ = Control ?? throw new InvalidOperationException($"{nameof(Element)} cannot be null");

		var standardSize = new Size { Width = defaultSize, Height = defaultSize / 2 };

		var currentSize = VirtualView.Size != default ? VirtualView.Size : standardSize;
		
		if (VirtualView.Content is not null && VirtualView.Size == default)
		{
			var content = VirtualView.Content;
			// There are some situations when the Widht and Height values will be NaN
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
		ArgumentNullException.ThrowIfNull(Control);
		FlyoutPresenterStyle = FlyoutStyle;
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
			var anchor = VirtualView.Anchor.ToNative(mauiContext);
			SetAttachedFlyout(anchor, this);
			ShowAttachedFlyout(anchor);
		}
		else
		{
			var frameworkElement = VirtualView.Parent.ToNative(mauiContext);
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
		else if (VirtualView is not null && VirtualView.Anchor is null)
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

	void OnClosing(object? sender, FlyoutBaseClosingEventArgs e)
	{
		var isLightDismissEnabled = VirtualView?.IsLightDismissEnabled is true;
		if (!isLightDismissEnabled)
		{
			e.Cancel = true;
		}

		if (IsOpen && isLightDismissEnabled)
		{
			VirtualView?.Handler?.Invoke(nameof(IPopup.LightDismiss));
		}
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
			Control.CleanUp();
		}

		VirtualView = null;
		Control = null;
	}
}
