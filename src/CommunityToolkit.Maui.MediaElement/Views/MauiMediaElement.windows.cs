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
public partial class MauiMediaElement : Grid, IDisposable
{
	[LibraryImport("user32.dll")]
	internal static partial IntPtr GetForegroundWindow();

	readonly Popup popup = new();
	readonly Grid fullScreenGrid = new();
	readonly Grid buttonContainer;
	readonly Button fullScreenButton;
	readonly MediaPlayerElement mediaPlayerElement;
	// Cannot be static readonly because we need to be able to add icon to multiple instances of the button
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

		fullScreenGrid.Children.Add(this.mediaPlayerElement);
		fullScreenGrid.Children.Add(buttonContainer);
		Children.Add(fullScreenGrid);

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
		PageExtensions.GetCurrentPage(Application.Current?.Windows[^1]?.Page ?? throw new InvalidOperationException($"{nameof(Page)} cannot be null."));

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

	static AppWindow GetAppWindowForWindowHandle(IntPtr hwnd)
	{
		var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
		return AppWindow.GetFromWindowId(windowId);
	}
	async void OnMediaPlayerElementPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		e.Handled = true;
		if(buttonContainer is null)
		{
			return;
		}
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
			SetDefaultScreen(appWindow, currentPage);
		}
		else
		{
			SetFullScreen(appWindow, currentPage);
		}
	}

	void SetFullScreen(AppWindow appWindow, Page currentPage)
	{
		if (popup.IsOpen)
		{
			return;
		}
		
		doesNavigationBarExistBeforeFullScreen = Shell.GetNavBarIsVisible(currentPage);
		Shell.SetNavBarIsVisible(currentPage, false);

		Children.Remove(fullScreenGrid);
		fullScreenButton.Content = exitFullScreenIcon;
		
		var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
		mediaPlayerElement.Height = displayInfo.Height / displayInfo.Density;

		appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
		popup.XamlRoot = XamlRoot;
		popup.ShouldConstrainToRootBounds = true;
		popup.Child = fullScreenGrid;
		popup.IsOpen = true;
	}

	void SetDefaultScreen(AppWindow appWindow, Page currentPage)
	{
		if(!popup.IsOpen)
		{
			return;
		}
		
		popup.IsOpen = false;
		popup.Child = null;

		appWindow.SetPresenter(AppWindowPresenterKind.Default);
		Shell.SetNavBarIsVisible(currentPage, doesNavigationBarExistBeforeFullScreen);
		
		fullScreenButton.Content = fullScreenIcon;
		Children.Add(fullScreenGrid);
		mediaPlayerElement.Height = double.NaN;

	}
}