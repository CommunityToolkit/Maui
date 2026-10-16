using System.Reflection;
using System.Runtime.InteropServices;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Primitives;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Application = Microsoft.Maui.Controls.Application;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using Page = Microsoft.Maui.Controls.Page;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The user-interface element that represents the <see cref="MediaElement"/> on Windows.
/// </summary>
public partial class MauiMediaElement : Grid, IDisposable
{
	readonly Popup popup = new();
	readonly Grid fullScreenGrid = new();
	readonly MediaPlayerElement mediaPlayerElement;
	readonly CustomTransportControls? customTransportControls;
	bool doesNavigationBarExistBeforeFullScreen;
	bool isDisposed;
	/// <summary>
	/// Initializes a new instance of the <see cref="MauiMediaElement"/> class.
	/// </summary>
	/// <param name="mediaPlayerElement"></param>
	public MauiMediaElement(MediaPlayerElement mediaPlayerElement)
	{
		LoadResourceDictionary();
		this.mediaPlayerElement = mediaPlayerElement;
		customTransportControls = SetTransportControls();
		Children.Add(this.mediaPlayerElement);
	}

	/// <summary>
	/// Finalizer
	/// </summary>
	~MauiMediaElement() => Dispose(false);

	/// <summary>
	/// Gets the presented page.
	/// </summary>
	protected static Page CurrentPage =>
		PageExtensions.GetCurrentPage(Application.Current?.Windows.FirstOrDefault()?.Page ?? throw new InvalidOperationException($"{nameof(Page)} cannot be null."));

	/// <summary>
	/// Releases the managed and unmanaged resources used by the <see cref="MauiMediaElement"/>.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	[LibraryImport("user32.dll")]
	internal static partial IntPtr GetForegroundWindow();

	/// <summary>
	/// Safely gets the foreground window handle, returning null if no foreground window exists.
	/// </summary>
	internal static IntPtr? TryGetForegroundWindow()
	{
		var hwnd = GetForegroundWindow();
		return hwnd == IntPtr.Zero ? null : hwnd;
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
			// The button is a XAML object: only Dispose(true) may touch it, Dispose(false) runs on the finalizer thread
			if (customTransportControls?.FullScreenButton is not null)
			{
				customTransportControls.FullScreenButton.Click -= OnFullScreenButtonClick;
			}

			mediaPlayerElement.MediaPlayer.Pause();

			if (mediaPlayerElement.MediaPlayer.Source is Windows.Media.Core.MediaSource mediaSource)
			{
				// Dispose the MediaSource to release the resources
				// https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/play-audio-and-video-with-mediaplayer Shows how to dispose the MediaSource
				mediaSource.Dispose();
			}
			mediaPlayerElement.MediaPlayer.Source = null;
			mediaPlayerElement.MediaPlayer.Dispose();
			mediaPlayerElement.SetMediaPlayer(null);
		}

		isDisposed = true;
	}

	static AppWindow GetAppWindowForCurrentWindow()
	{
		var windowHandle = TryGetForegroundWindow() ?? throw new InvalidOperationException("No foreground window found.");
		var id = Win32Interop.GetWindowIdFromWindow(windowHandle);
		return AppWindow.GetFromWindowId(id);
	}

	// The window that hosts this element. The foreground window can be another one: another window of the app (or of another app) is in front
	// when the button is pressed from the keyboard or by assistive technology.
	AppWindow? GetAppWindowOfThisElement() =>
		XamlRoot?.ContentIslandEnvironment is Microsoft.UI.Content.ContentIslandEnvironment environment && environment.AppWindowId.Value is not 0
			? AppWindow.GetFromWindowId(environment.AppWindowId)
			: null;

	// The page whose navigation bar full screen hides: the current page of the application's window that is shown in appWindow, or of the first
	// window (CurrentPage) when none of them is
	static Page GetCurrentPageOfWindow(AppWindow appWindow) =>
		Application.Current?.Windows.FirstOrDefault(window => window.Handler?.PlatformView is Microsoft.UI.Xaml.Window platformWindow && platformWindow.AppWindow.Id == appWindow.Id)?.Page is Page page
			? PageExtensions.GetCurrentPage(page)
			: CurrentPage;

	void LoadResourceDictionary()
	{
		var assembly = Assembly.GetExecutingAssembly();
		using Stream? stream = assembly.GetManifestResourceStream("ResourceDictionary.windows.xaml");
		if (stream is null)
		{
			return;
		}
		using StreamReader reader = new(stream);
		var xaml = reader.ReadToEnd();
		var resourceDictionary = (Microsoft.UI.Xaml.ResourceDictionary)XamlReader.Load(xaml);
		if (resourceDictionary is null)
		{
			return;
		}
		this.Resources.MergedDictionaries.Add(resourceDictionary);
	}
	void ApplyCustomStyle()
	{
		if (this.Resources.TryGetValue("customTransportcontrols", out object styleObj) &&
			styleObj is Microsoft.UI.Xaml.Style customStyle && mediaPlayerElement is not null && mediaPlayerElement.TransportControls is not null)
		{
			mediaPlayerElement.TransportControls.Style = customStyle;
		}
	}

	CustomTransportControls SetTransportControls()
	{
		mediaPlayerElement.TransportControls.IsEnabled = false;
		var temp = new CustomTransportControls()
		{
			IsZoomButtonVisible = true,
			IsZoomEnabled = true,
			IsVolumeButtonVisible = true,
			IsVolumeEnabled = true,
			IsSeekBarVisible = true,
			IsSeekEnabled = true,
			IsEnabled = true,
			IsRepeatButtonVisible = true,
			IsRepeatEnabled = true,
			IsNextTrackButtonVisible = true,
			IsPreviousTrackButtonVisible = true,
			IsFastForwardButtonVisible = true,
			IsFastForwardEnabled = true,
			IsFastRewindButtonVisible = true,
			IsFastRewindEnabled = true,
			IsPlaybackRateButtonVisible = true,
			IsPlaybackRateEnabled = true,
			IsCompact = false,
		};
		temp.OnTemplateLoaded += (s, e) =>
		{
			if (temp.FullScreenButton is null)
			{
				return;
			}
			temp.FullScreenButton.Click += OnFullScreenButtonClick;
		};
		mediaPlayerElement.TransportControls = temp;
		ApplyCustomStyle();
		return temp;
	}

	void OnFullScreenButtonClick(object sender, RoutedEventArgs e)
	{
		var appWindow = GetAppWindowOfThisElement() ?? GetAppWindowForCurrentWindow();
		var currentPage = GetCurrentPageOfWindow(appWindow);

		// The window can also enter or leave full screen without this button (F11, the app, the user): the element is moved, and the
		// navigation bar hidden or restored, only when the element is not already where the new presenter needs it
		var isElementInFullScreenPopup = fullScreenGrid.Children.Contains(mediaPlayerElement);

		if (appWindow.Presenter.Kind is AppWindowPresenterKind.FullScreen)
		{
			appWindow.SetPresenter(AppWindowPresenterKind.Default);

			if (!isElementInFullScreenPopup)
			{
				return;
			}

			Shell.SetNavBarIsVisible(currentPage, doesNavigationBarExistBeforeFullScreen);

			popup.IsOpen = false;
			popup.Child = null;
			fullScreenGrid.Children.Clear();
			Children.Add(mediaPlayerElement);

			var parent = mediaPlayerElement.Parent as FrameworkElement;
			mediaPlayerElement.Width = parent?.Width ?? mediaPlayerElement.Width;
			mediaPlayerElement.Height = parent?.Height ?? mediaPlayerElement.Height;
		}
		else
		{
			appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);

			// The window left full screen without this button while the element was shown full screen: the element is still in the popup,
			// and the navigation bar is still hidden, so its saved state must stay
			if (isElementInFullScreenPopup)
			{
				return;
			}

			doesNavigationBarExistBeforeFullScreen = Shell.GetNavBarIsVisible(currentPage);
			Shell.SetNavBarIsVisible(currentPage, false);

			var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
			mediaPlayerElement.Width = displayInfo.Width / displayInfo.Density;
			mediaPlayerElement.Height = displayInfo.Height / displayInfo.Density;

			Children.Clear();
			fullScreenGrid.Children.Add(mediaPlayerElement);

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
}