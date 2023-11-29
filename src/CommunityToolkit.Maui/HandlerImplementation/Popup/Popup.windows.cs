using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;

namespace CommunityToolkit.Maui.Views;

public partial class Popup : Element
{
	void AddHandlerChanged()
	{
		HandlerChanged += OnHandlerChanged;
	}

	void AddPropertyChanged()
	{
		PropertyChanged += OnPropertyChanged;
	}

	void RemoveHandlerChanged()
	{
		HandlerChanged -= OnHandlerChanged;

		if (Handler?.MauiContext is IMauiContext mauiContext)
		{
			var platformPopup = this.ToHandler(mauiContext);
			if (platformPopup.VirtualView is IPopup pPopup &&
				pPopup.Content?.ToPlatform(mauiContext) is Microsoft.UI.Xaml.FrameworkElement container)
			{
				var window = mauiContext.GetPlatformWindow();
				window.SizeChanged -= OnWindowSizeChanged;
				container.SizeChanged -= OnContainerSizeChanged;
			}
		}
	}

	void RemovePropertyChanged()
	{
		PropertyChanged -= OnPropertyChanged;
	}

	void OnHandlerChanged(object? sender, EventArgs e)
	{
		if (Handler?.MauiContext is IMauiContext mauiContext)
		{
			var platformPopup = this.ToHandler(mauiContext);
			if (platformPopup.VirtualView is IPopup pPopup &&
				pPopup.Content?.ToPlatform(mauiContext) is Microsoft.UI.Xaml.FrameworkElement container)
			{
				var window = mauiContext.GetPlatformWindow();
				window.SizeChanged += OnWindowSizeChanged;
				container.SizeChanged += OnContainerSizeChanged;
			}
		}
	}

	void OnContainerSizeChanged(object? sender, SizeChangedEventArgs e)
	{
		if (Handler?.MauiContext is IMauiContext mauiContext)
		{
			var platformPopup = this.ToHandler(mauiContext);
			if (platformPopup.PlatformView is Microsoft.UI.Xaml.Controls.Primitives.Popup dialog &&
				platformPopup.VirtualView is IPopup pPopup &&
				pPopup.Content?.ToPlatform(mauiContext) is Microsoft.UI.Xaml.FrameworkElement container)
			{
				SetSize(dialog, this, pPopup, mauiContext, container);
				CommunityToolkit.Maui.Core.Views.PopupExtensions.SetLayout(dialog, pPopup, mauiContext);
			}
		}

	}

	void OnWindowSizeChanged(object? sender, WindowSizeChangedEventArgs e)
	{
		if (Handler?.MauiContext is IMauiContext mauiContext)
		{
			var platformPopup = this.ToHandler(mauiContext);
			if (platformPopup.PlatformView is Microsoft.UI.Xaml.Controls.Primitives.Popup dialog &&
				platformPopup.VirtualView is IPopup pPopup &&
				pPopup.Content?.ToPlatform(mauiContext) is Microsoft.UI.Xaml.FrameworkElement container)
			{
				SetSize(dialog, this, pPopup, mauiContext, container);
				CommunityToolkit.Maui.Core.Views.PopupExtensions.SetLayout(dialog, pPopup, mauiContext);
			}
		}
	}

	void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(Size))
		{
			if (Handler?.MauiContext is IMauiContext mauiContext)
			{
				var platformPopup = this.ToHandler(mauiContext);
				if (platformPopup.PlatformView is Microsoft.UI.Xaml.Controls.Primitives.Popup dialog &&
					platformPopup.VirtualView is IPopup pPopup &&
					pPopup.Content?.ToPlatform(mauiContext) is Microsoft.UI.Xaml.FrameworkElement container)
				{
					SetSize(dialog, this, pPopup, mauiContext, container);
					CommunityToolkit.Maui.Core.Views.PopupExtensions.SetLayout(dialog, pPopup, mauiContext);
				}
			}
		}
	}

	static void SetSize(Microsoft.UI.Xaml.Controls.Primitives.Popup mauiPopup, Popup vPopup, IPopup pPopup, IMauiContext mauiContext, FrameworkElement container)
	{
		ArgumentNullException.ThrowIfNull(pPopup.Content);
		ArgumentNullException.ThrowIfNull(pPopup.Content.ToPlatform());

		var window = mauiContext.GetPlatformWindow();
		var windowSize = new Size(window.Bounds.Width, window.Bounds.Height);

		var pView = pPopup.Content.ToPlatform();
		if (pPopup.Size.IsZero)
		{
			if (double.IsNaN(pPopup.Content.Width) || double.IsNaN(pPopup.Content.Height))
			{				
				pView.Width = double.NaN;
				pView.Height = double.NaN;

				pView.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));

				if (pView.ActualWidth >= windowSize.Width &&
					pView.ActualHeight >= windowSize.Height)
				{
					pView.Width = windowSize.Width;
					pView.UpdateHeight(pPopup.Content);
				}
				else if (pView.ActualWidth >= windowSize.Width)
				{
					pView.Width = windowSize.Width;
				}
				else if (pView.ActualHeight >= windowSize.Height)
				{
					pView.Height = windowSize.Height;
				}

				if (pPopup.HorizontalOptions == LayoutAlignment.Fill)
				{
					pView.Width = windowSize.Width;
				}
				if (pPopup.VerticalOptions == LayoutAlignment.Fill)
				{
					pView.Height = windowSize.Height;
				}
			}
			else
			{
				pView.Width = pPopup.Content.Width;
				pView.Height = pPopup.Content.Height;
			}
		}
		else
		{
			pView.Width = pPopup.Size.Width;
			pView.Height = pPopup.Size.Height;
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
}
