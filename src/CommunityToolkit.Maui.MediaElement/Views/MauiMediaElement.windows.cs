using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using WinRT.Interop;
using Application = Microsoft.Maui.Controls.Application;
using Button = Microsoft.UI.Xaml.Controls.Button;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using Image = Microsoft.UI.Xaml.Controls.Image;
using ImageSource = Microsoft.UI.Xaml.Media.ImageSource;
using Page = Microsoft.Maui.Controls.Page;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The user-interface element that represents the <see cref="MediaElement"/> on Windows.
/// </summary>
public class MauiMediaElement : Grid, IDisposable
{
	bool navbarExist;
	bool isDisposed;

	static readonly AppWindow? appWindow = GetAppWindowForCurrentWindow();
	readonly MediaPlayerElement mediaPlayerElement;

	static ImageSource source = new BitmapImage(new Uri("ms-appx:///fullscreen.png"));
	Button btn;
	Grid grid = new();
	Grid buttonContainer;
	Popup popup = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="MauiMediaElement"/> class.
	/// </summary>
	/// <param name="mediaPlayerElement"></param>
	public MauiMediaElement(MediaPlayerElement mediaPlayerElement)
	{
		this.mediaPlayerElement = mediaPlayerElement;

		btn = new Button
		{
			Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
			Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
			Width = 40,
			Height = 40
		};
		btn.Click += ButtonSetFullScreenStatus;

		Image image = new()
		{
			Width = 40,
			Height = 40,
			HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Right,
			VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Top,
			Source = source,
		};

		buttonContainer = new()
		{
			HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Right,
			VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Top,
			Width = 40,
			Height = 40
		};
		buttonContainer.Children.Add(image);
		buttonContainer.Children.Add(btn);

		Children.Add(this.mediaPlayerElement);
		Children.Add(buttonContainer);

		mediaPlayerElement.PointerMoved += OnMediaPlayerElementPointerMoved;
	}

	async void OnMediaPlayerElementPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		e.Handled = true;
		mediaPlayerElement.PointerMoved -= OnMediaPlayerElementPointerMoved;
		buttonContainer.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
		await Task.Delay(TimeSpan.FromSeconds(5));
		buttonContainer.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
		mediaPlayerElement.PointerMoved += OnMediaPlayerElementPointerMoved;
	}

	void ButtonSetFullScreenStatus(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		var currentPage = CurrentPage;
		if (appWindow is null)
		{
			return;
		}

		if (appWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen)
		{
			appWindow.SetPresenter(AppWindowPresenterKind.Default);
			Shell.SetNavBarIsVisible(CurrentPage, navbarExist);

			if (popup.IsOpen)
			{
				popup.IsOpen = false;
				popup.Child = null;
				grid.Children.Clear();
			}

			Children.Add(this.mediaPlayerElement);
			Children.Add(this.buttonContainer);

			var parent = mediaPlayerElement.Parent as FrameworkElement;
			mediaPlayerElement.Width = parent?.Width ?? mediaPlayerElement.Width;
			mediaPlayerElement.Height = parent?.Height ?? mediaPlayerElement.Height;
		}
		else
		{
			appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
			navbarExist = Shell.GetNavBarIsVisible(currentPage);
			Shell.SetNavBarIsVisible(CurrentPage, false);

			var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
			mediaPlayerElement.Width = displayInfo.Width / displayInfo.Density;
			mediaPlayerElement.Height = displayInfo.Height / displayInfo.Density;

			Children.Clear();
			grid.Children.Add(mediaPlayerElement);
			grid.Children.Add(buttonContainer);

			popup.XamlRoot = mediaPlayerElement.XamlRoot;
			popup.HorizontalOffset = 0;
			popup.VerticalOffset = 0;
			popup.ShouldConstrainToRootBounds = false;
			popup.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center;
			popup.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center;
			popup.Child = grid;

			if (!popup.IsOpen)
			{
				popup.IsOpen = true;
			}
		}
	}

	static AppWindow GetAppWindowForCurrentWindow()
	{
		// let's cache the CurrentPage here, since the user can navigate or background the app
		// while this method is running
		var currentPage = CurrentPage;

		if (currentPage?.GetParentWindow().Handler.PlatformView is not MauiWinUIWindow window)
		{
			throw new InvalidOperationException($"{nameof(window)} cannot be null.");
		}
		var handle = WindowNative.GetWindowHandle(window);
		var id = Win32Interop.GetWindowIdFromWindow(handle);
		return AppWindow.GetFromWindowId(id);
	}

	/// <summary>
	/// Gets the presented page.
	/// </summary>
	protected static Page CurrentPage =>
		PageExtensions.GetCurrentPage(Application.Current?.MainPage ?? throw new InvalidOperationException($"{nameof(Application.Current.MainPage)} cannot be null."));

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
	/// Releases the managed and unmanaged resources used by the <see cref="MauiMediaElement"/>.
	/// </summary>
	protected virtual void Dispose(bool disposing)
	{
		if (isDisposed)
		{
			return;
		}

		if (disposing)
		{
			mediaPlayerElement.MediaPlayer.Dispose();
		}

		isDisposed = true;
	}
}