using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.CoordinatorLayout.Widget;
using Com.Google.Android.Exoplayer2.UI;
using CommunityToolkit.Maui.Views;
using AndroidX.Core.View;
using Android.Content.Res;
using Android.App;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The user-interface element that represents the <see cref="MediaElement"/> on Android.
/// </summary>
public class MauiMediaElement : CoordinatorLayout
{
	readonly StyledPlayerView playerView;
	readonly Context context;
	int defaultSystemUiVisibility;
	bool isSystemBarVisible;

	/// <summary>
	/// Initializes a new instance of the <see cref="MauiMediaElement"/> class.
	/// </summary>
	/// <param name="context">The application's <see cref="Context"/>.</param>
	/// <param name="playerView">The <see cref="StyledPlayerView"/> that acts as the platform media player.</param>
	public MauiMediaElement(Context context, StyledPlayerView playerView) : base(context)
	{
		this.playerView = playerView;
		this.context = context;

		playerView.FullscreenButtonClick += OnFullscreenButtonClick;
		this.playerView.SetBackgroundColor(Android.Graphics.Color.Black);

		AddView(playerView);
	}

	/// <summary>
	/// Allows the video to enter or exist a full screen mode
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	void OnFullscreenButtonClick(object? sender, StyledPlayerView.FullscreenButtonClickEventArgs e)
	{
		// Ensure there is a player view
		if (this.playerView is null)
		{
			return;
		}

		// Ensure current activity and window are available
		if (Platform.CurrentActivity is not Activity currentActivity
			|| currentActivity.Window is not Android.Views.Window currentWindow
			|| currentActivity.Resources is not Resources currentResources
			|| currentResources.Configuration is null)
		{
			return;
		}

		var windowInsetsControllerCompat = WindowCompat.GetInsetsController(currentWindow, currentWindow.DecorView);

		if (e.IsFullScreen)
		{
			// Force the landscape on the device
			currentActivity.RequestedOrientation = Android.Content.PM.ScreenOrientation.Landscape;

			// Hide the SystemBars and Status bar
			if (OperatingSystem.IsAndroidVersionAtLeast(30))
			{
				currentWindow.SetDecorFitsSystemWindows(false);

				var windowInsets = currentWindow.DecorView.RootWindowInsets;
				if (windowInsets is not null)
				{
					isSystemBarVisible = windowInsets.IsVisible(WindowInsetsCompat.Type.NavigationBars()) || windowInsets.IsVisible(WindowInsetsCompat.Type.StatusBars());
					
					if (isSystemBarVisible)
					{
						windowInsetsControllerCompat.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
						currentWindow.InsetsController?.Hide(WindowInsets.Type.SystemBars());
					}
				}
			}
			else
			{
				defaultSystemUiVisibility = (int)currentWindow.DecorView.SystemUiVisibility;
				int systemUiVisibility = defaultSystemUiVisibility | (int)SystemUiFlags.LayoutStable | (int)SystemUiFlags.LayoutHideNavigation | (int)SystemUiFlags.LayoutHideNavigation |
					(int)SystemUiFlags.LayoutFullscreen | (int)SystemUiFlags.HideNavigation | (int)SystemUiFlags.Fullscreen | (int)SystemUiFlags.Immersive;
				currentWindow.DecorView.SystemUiVisibility = (StatusBarVisibility)systemUiVisibility;
			}

			// Update the PlayerView
			if (currentWindow.DecorView is FrameLayout layout)
			{
				RemoveView(playerView);
				layout.AddView(playerView, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
			}
		}
		else
		{
			currentActivity.RequestedOrientation = Android.Content.PM.ScreenOrientation.Portrait;
			
			// Show again the SystemBars and Status bar
			if (OperatingSystem.IsAndroidVersionAtLeast(30))
			{
				currentWindow.SetDecorFitsSystemWindows(true);

				if (isSystemBarVisible)
				{
					var types = WindowInsetsCompat.Type.StatusBars() |
								WindowInsetsCompat.Type.SystemBars() |
								WindowInsetsCompat.Type.NavigationBars();
					windowInsetsControllerCompat.Show(types);
					windowInsetsControllerCompat.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorDefault;
					currentWindow.InsetsController?.Show(WindowInsets.Type.SystemBars());
				}
			}
			else
			{
				currentWindow.DecorView.SystemUiVisibility = (StatusBarVisibility)defaultSystemUiVisibility;
			}

			// Update the PlayerView
			if (currentWindow.DecorView is FrameLayout layout)
			{
				layout.RemoveView(playerView);
				AddView(playerView);
			}
		}
	}

	/// <summary>
	/// Releases the unmanaged resources used by the <see cref="MediaElement"/> and optionally releases the managed resources.
	/// </summary>
	/// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			// https://github.com/google/ExoPlayer/issues/1855#issuecomment-251041500
			playerView.Player?.Release();
			playerView.Player?.Dispose();
			playerView.Dispose();
		}

		base.Dispose(disposing);
	}
}