using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.CoordinatorLayout.Widget;
using Com.Google.Android.Exoplayer2.UI;
using CommunityToolkit.Maui.Views;
using AndroidX.Core.View;
using Android.Content.Res;
using Android.App;
using Android.Runtime;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The user-interface element that represents the <see cref="MediaElement"/> on Android.
/// </summary>
public class MauiMediaElement : CoordinatorLayout
{
	readonly StyledPlayerView playerView;
	int defaultSystemUiVisibility;
	bool isSystemBarVisible;
	bool isFullScreen;

	/// <summary>
	/// Initializes a new instance of the <see cref="MauiMediaElement"/> class.
	/// </summary>
	/// <param name="context">The application's <see cref="Context"/>.</param>
	/// <param name="playerView">The <see cref="StyledPlayerView"/> that acts as the platform media player.</param>
	public MauiMediaElement(Context context, StyledPlayerView playerView) : base(context)
	{
		this.playerView = playerView;

		playerView.FullscreenButtonClick += OnFullscreenButtonClick;
		this.playerView.SetBackgroundColor(Android.Graphics.Color.Black);

		AddView(playerView);
	}

	/// <summary>
	/// Checks the visibility of the view
	/// </summary>
	/// <param name="changedView"></param>
	/// <param name="visibility"></param>
	protected override void OnVisibilityChanged(Android.Views.View changedView, [GeneratedEnum] ViewStates visibility)
	{
		base.OnVisibilityChanged(changedView, visibility);
		if (isFullScreen && visibility == ViewStates.Visible)
		{
			SetSystemBarsVisibility();
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

	void OnFullscreenButtonClick(object? sender, StyledPlayerView.FullscreenButtonClickEventArgs e)
	{
		// Ensure there is a player view
		if (playerView is null)
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

		// Hide the SystemBars and Status bar
		if (e.IsFullScreen)
		{
			isFullScreen = true;

			SetSystemBarsVisibility();

			// Update the PlayerView
			if (currentWindow.DecorView is FrameLayout layout)
			{
				RemoveView(playerView);
				layout.AddView(playerView, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
			}
		}
		// Show again the SystemBars and Status bar
		else
		{
			isFullScreen = false;
			SetSystemBarsVisibility();

			// Update the PlayerView
			if (currentWindow.DecorView is FrameLayout layout)
			{
				layout.RemoveView(playerView);
				AddView(playerView);
			}
		}
	}

	/// <summary>
	/// Sets the visibility of the navigation bar and status bar when full screen
	/// </summary>
	void SetSystemBarsVisibility()
	{
		if (Platform.CurrentActivity is not Activity currentActivity
			|| currentActivity.Window is not Android.Views.Window currentWindow
			|| currentActivity.Resources is not Resources currentResources
			|| currentResources.Configuration is null)
		{
			return;
		}

		var windowInsetsControllerCompat = WindowCompat.GetInsetsController(currentWindow, currentWindow.DecorView);

		var barTypes = WindowInsetsCompat.Type.StatusBars()
						| WindowInsetsCompat.Type.SystemBars()
						| WindowInsetsCompat.Type.NavigationBars();

		if (isFullScreen)
		{
			if (OperatingSystem.IsAndroidVersionAtLeast(30))
			{
				currentWindow.SetDecorFitsSystemWindows(false);

				var windowInsets = currentWindow.DecorView.RootWindowInsets;
				if (windowInsets is not null)
				{
					isSystemBarVisible = windowInsets.IsVisible(WindowInsetsCompat.Type.NavigationBars()) || windowInsets.IsVisible(WindowInsetsCompat.Type.StatusBars());

					if (isSystemBarVisible)
					{
						currentWindow.InsetsController?.Hide(WindowInsets.Type.SystemBars());
					}
				}
			}
			else
			{
				defaultSystemUiVisibility = (int)currentWindow.DecorView.SystemUiFlags;

				currentWindow.DecorView.SystemUiFlags = currentWindow.DecorView.SystemUiFlags
					| SystemUiFlags.LayoutStable
					| SystemUiFlags.LayoutHideNavigation
					| SystemUiFlags.LayoutFullscreen
					| SystemUiFlags.HideNavigation
					| SystemUiFlags.Fullscreen
					| SystemUiFlags.Immersive;
			}

			windowInsetsControllerCompat.Hide(barTypes);
			windowInsetsControllerCompat.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;

		}
		else
		{
			if (OperatingSystem.IsAndroidVersionAtLeast(30))
			{
				currentWindow.SetDecorFitsSystemWindows(true);

				if (isSystemBarVisible)
				{
					currentWindow.InsetsController?.Show(WindowInsets.Type.SystemBars());
				}
			}
			else
			{
				currentWindow.DecorView.SystemUiFlags = (SystemUiFlags)defaultSystemUiVisibility;
			}

			windowInsetsControllerCompat.Show(barTypes);
			windowInsetsControllerCompat.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorDefault;

		}
	}
}