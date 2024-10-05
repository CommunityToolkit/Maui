using System.Runtime.InteropServices;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using Application = Microsoft.Maui.Controls.Application;
using Button = Microsoft.UI.Xaml.Controls.Button;
using Colors = Microsoft.UI.Colors;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using Page = Microsoft.Maui.Controls.Page;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using Thickness = Microsoft.UI.Xaml.Thickness;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The user-interface element that represents the <see cref="MediaElement"/> on Windows.
/// </summary>
public class MauiMediaElement : Grid, IDisposable
{
	[DllImport("user32.dll")]
	static extern IntPtr GetForegroundWindow();

	SizeInt32? windowSize;
	PointInt32? windowPosition;
	readonly Popup popup = new();
	readonly Grid fullScreenGrid = new();
	readonly Grid buttonContainer;
	readonly Button fullScreenButton;
	readonly MediaPlayerElement mediaPlayerElement;
	readonly FontIcon fullScreenIcon = new() { Glyph = "\uE740", FontFamily = new FontFamily("Segoe Fluent Icons") };
	readonly FontIcon exitFullScreenIcon = new() { Glyph = "\uE73F", FontFamily = new FontFamily("Segoe Fluent Icons") };
	bool doesNavigationBarExistBeforeFullScreen;
	bool isDisposed;

	/// <summary>
	/// Initializes a new instance of the <see cref="MauiMediaElement"/> class.
	/// </summary>
	/// <param name="mediaPlayerElement"></param>
	public MauiMediaElement(MediaPlayerElement mediaPlayerElement)
	{
		this.mediaPlayerElement = mediaPlayerElement;

		fullScreenButton = new Button
		{
			Content = fullScreenIcon,
			Background = new SolidColorBrush(Colors.Transparent),
			Width = 45,
			Height = 45
		};

		buttonContainer = new Grid
		{
			HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Right,
			VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Top,
			Visibility = mediaPlayerElement.TransportControls.Visibility,
			Width = 45,
			Height = 45,
			Margin = new Thickness(0, 20, 30, 0)
		};

		fullScreenButton.Click += OnFullScreenButtonClick;
		buttonContainer.Children.Add(fullScreenButton);

		Children.Add(this.mediaPlayerElement);
		Children.Add(buttonContainer);

		mediaPlayerElement.PointerMoved += OnMediaPlayerElementPointerMoved;
	}

	/// <summary>
	/// Finalizer
	/// </summary>
	~MauiMediaElement() => Dispose(false);

	/// <summary>
	/// Releases the managed and unmanaged resources used by the <see cref="MauiMediaElement"/>.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Gets the presented page.
	/// </summary>
	protected static Page CurrentPage =>
		PageExtensions.GetCurrentPage(Application.Current?.MainPage ?? throw new InvalidOperationException($"{nameof(Application.Current.MainPage)} cannot be null."));

	/// <summary>
	/// Releases the managed and unmanaged resources used by the <see cref="MauiMediaElement"/>.
	/// </summary>
	protected virtual void Dispose(bool disposing)
	{
		if (isDisposed)
		{
			return;
		}

		fullScreenButton.Click -= OnFullScreenButtonClick;
		mediaPlayerElement.PointerMoved -= OnMediaPlayerElementPointerMoved;

		if (disposing)
		{
			mediaPlayerElement.MediaPlayer.Dispose();
		}

		isDisposed = true;
	}

	async void OnMediaPlayerElementPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		e.Handled = true;
		buttonContainer.Visibility = mediaPlayerElement.TransportControls.Visibility;

		if (mediaPlayerElement.TransportControls.Visibility == Microsoft.UI.Xaml.Visibility.Collapsed)
		{
			buttonContainer.Visibility = mediaPlayerElement.TransportControls.Visibility;
			return;
		}

		mediaPlayerElement.PointerMoved -= OnMediaPlayerElementPointerMoved;
		buttonContainer.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
		await Task.Delay(TimeSpan.FromSeconds(5));

		buttonContainer.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
		mediaPlayerElement.PointerMoved += OnMediaPlayerElementPointerMoved;
	}

	void OnFullScreenButtonClick(object sender, RoutedEventArgs e)
	{
		var currentPage = CurrentPage;
		var windowHandle = GetForegroundWindow();
		var appWindow = GetAppWindowForWindowHandle(windowHandle);

		if (appWindow.Presenter.Kind is AppWindowPresenterKind.FullScreen)
		{
			ExitFullScreen(appWindow, currentPage);
		}
		else
		{
			EnterFullScreen(appWindow, currentPage);
		}
	}

	static AppWindow GetAppWindowForWindowHandle(IntPtr hwnd)
	{
		var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
		return AppWindow.GetFromWindowId(windowId);
	}

	void ExitFullScreen(AppWindow window, Page currentPage)
	{
		window.SetPresenter(AppWindowPresenterKind.Default);
		Shell.SetNavBarIsVisible(currentPage, doesNavigationBarExistBeforeFullScreen);

		if (popup.IsOpen)
		{
			popup.IsOpen = false;
			popup.Child = null;
			fullScreenGrid.Children.Clear();
		}

		fullScreenButton.Content = fullScreenIcon;
		Children.Add(mediaPlayerElement);
		Children.Add(buttonContainer);
		var parent = mediaPlayerElement.Parent as FrameworkElement;
		mediaPlayerElement.Width = parent?.Width ?? mediaPlayerElement.Width;
		mediaPlayerElement.Height = parent?.Height ?? mediaPlayerElement.Height;
		window.Move(windowPosition ?? new PointInt32(0, 0));
		window.Resize(windowSize ?? new SizeInt32((int)mediaPlayerElement.Width, (int)mediaPlayerElement.Height));
	}

	void EnterFullScreen(AppWindow window, Page currentPage)
	{

		doesNavigationBarExistBeforeFullScreen = Shell.GetNavBarIsVisible(currentPage);
		Shell.SetNavBarIsVisible(currentPage, false);
		var (screenWidth, screenHeight) = GetScreenSize();
		
		windowSize = window.Size;
		windowPosition = new PointInt32(window.Position.X, window.Position.Y);
		window.Resize(new SizeInt32((int)screenWidth, (int)screenHeight));
		window.Move(new PointInt32(0, 0));
		window.SetPresenter(AppWindowPresenterKind.FullScreen);

		fullScreenButton.Content = exitFullScreenIcon;
		mediaPlayerElement.Width = screenWidth;
		mediaPlayerElement.Height = screenHeight;
		Children.Clear();
		fullScreenGrid.Children.Add(mediaPlayerElement);
		fullScreenGrid.Children.Add(buttonContainer);
	
		ConfigureAndShowPopup();
	}

	static (double Width, double Height) GetScreenSize()
	{
		var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
		double screenWidth = displayInfo.Width / displayInfo.Density;
		double screenHeight = displayInfo.Height / displayInfo.Density;
		return (screenWidth, screenHeight);
	}
	void ConfigureAndShowPopup()
	{
		popup.XamlRoot = mediaPlayerElement.XamlRoot;
		popup.HorizontalOffset = 0;
		popup.VerticalOffset = 0;
		popup.ShouldConstrainToRootBounds = false;
		popup.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center;
		popup.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center;
		popup.Child = fullScreenGrid;

		if (!popup.IsOpen)
		{
			popup.IsOpen = true;
		}
	}
}