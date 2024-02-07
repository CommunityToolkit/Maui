using Mopups.Pages;
using Mopups.Services;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui.Controls.PlatformConfiguration;
using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.Pages;
/// <summary>
///  A class that acts as a manager for an <see cref="FullScreenPage"/> instance.
/// </summary>
public partial class FullScreenPage : PopupPage
{
	MediaElement mediaElement { get; set; }
	CurrentVideoState Video { get; set; }

	/// <summary>
	/// Initializes a new instance of FullScreenPage class.
	/// </summary>
	/// <param name="currentVideo">The <see cref="CurrentVideoState"/> instance that is managed through this class.</param>
	public FullScreenPage(CurrentVideoState currentVideo)
	{
		Video = currentVideo;
		setStatus();
		mediaElement = new()
		{
			Source = currentVideo.VideoUri,
			HorizontalOptions = LayoutOptions.Fill,
			VerticalOptions = LayoutOptions.Fill,
			ShouldAutoPlay = true,
			ShouldShowPlaybackControls = true,
			ShouldKeepScreenOn = true,
		};
		ImageButton fullScreen = new()
		{
			Margin = new Thickness(10),
			BackgroundColor = Colors.Black,
			HeightRequest = 30,
			HorizontalOptions = LayoutOptions.End,
			VerticalOptions = LayoutOptions.Start,
			Source = "whitefs.png",
			WidthRequest = 30,
		};
#if ANDROID
		fullScreen.IsVisible = false;
#endif
		fullScreen.Clicked += async (s, e) =>
		{
			MediaManager.FullScreenPosition = mediaElement.Position;
			mediaElement.Source = null;
			mediaElement.RevertFromFullScreen();
			await MopupService.Instance.PopAsync();
		};
		
		Content = new Grid
		{
			Children =
			{
				mediaElement, fullScreen,
			}
		};
    }
	static void setStatus()
	{
		if (PageExtensions.isFullScreen)
		{
			PageExtensions.isFullScreen = false;
			return;
		}
		PageExtensions.isFullScreen = true;
	}
	/// <summary>
	/// Sets the correct playback position on Page load.
	/// </summary>
	protected override void OnAppearing()
	{
		base.OnAppearing();
#if IOS
		mediaElement.StateChanged += IOSStart;
#else
		_ = mediaElement.SeekTo(Video.Position);
#endif
	}

	/// <summary>
	/// Cleans up resources when Page is unloaded.
	/// </summary>
	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		setStatus();
		mediaElement.Handler?.DisconnectHandler();
	}

	async void IOSStart(object? sender, MediaStateChangedEventArgs e)
	{
		if (e.NewState == MediaElementState.Playing)
		{ 
			mediaElement.StateChanged -= IOSStart;
			await mediaElement.SeekTo(Video.Position);
		}
	}
}

/// <summary>
///  A class that acts as a manager for an <see cref="CurrentVideoState"/> instance.
/// </summary>
public class CurrentVideoState
{
	/// <summary>
	/// Represents a <see cref="MediaSource"/> for Media Playback
	/// </summary>
	public MediaSource? VideoUri { get; set; }

	/// <summary>
	/// Represents a <see cref="TimeSpan"/> playback position for for <see cref="MediaElement"/>
	/// </summary>
	public TimeSpan Position { get; set; }
}