using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.CoordinatorLayout.Widget;
using AndroidX.Core.View;
using Com.Google.Android.Exoplayer2.UI;
using CommunityToolkit.Maui.Views;

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
	int playerHeight;
	int playerWidth;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
#pragma warning disable IDE0060 // Remove unused parameter
	public MauiMediaElement(nint ptr, JniHandleOwnership jni) : base(Platform.AppContext)
	{
		//Fixes no constructor found exception: https://github.com/CommunityToolkit/Maui/pull/1692#issuecomment-1955099758
	}
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
#pragma warning restore IDE0060 // Remove unused parameter

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

	public override void OnDetachedFromWindow()
	{
		if (isFullScreen)
		{
			OnFullscreenButtonClick(this, new StyledPlayerView.FullscreenButtonClickEventArgs(!isFullScreen));
		}
		base.OnDetachedFromWindow();
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
			try
			{
				// https://github.com/google/ExoPlayer/issues/1855#issuecomment-251041500
				playerView.Player?.Release();
				playerView.Player?.Dispose();
				playerView.Dispose();
			}
			catch (ObjectDisposedException)
			{
				// playerView already disposed
			}
		}

		base.Dispose(disposing);
	}

	static (Activity CurrentActivity, Android.Views.Window CurrentWindow, Resources CurrentWindowResources, Configuration CurrentWindowConfiguration) VerifyAndRetrieveCurrentWindowResources()
	{
		// Ensure current activity and window are available
		if (Platform.CurrentActivity is not Activity currentActivity)
		{
			throw new InvalidOperationException("CurrentActivity cannot be null when the FullScreen button is tapped");
		}
		if (currentActivity.Window is not Android.Views.Window currentWindow)
		{
			throw new InvalidOperationException("CurrentActivity Window cannot be null when the FullScreen button is tapped");
		}

		if (currentActivity.Resources is not Resources currentResources)
		{
			throw new InvalidOperationException("CurrentActivity Resources cannot be null when the FullScreen button is tapped");
		}

		if (currentResources.Configuration is not Configuration configuration)
		{
			throw new InvalidOperationException("CurrentActivity Configuration cannot be null when the FullScreen button is tapped");
		}

		return (currentActivity, currentWindow, currentResources, configuration);
	}

	void OnFullscreenButtonClick(object? sender, StyledPlayerView.FullscreenButtonClickEventArgs e)
	{
		// Ensure there is a player view
		if (playerView is null)
		{
			throw new InvalidOperationException("PlayerView cannot be null when the FullScreen button is tapped");
		}

		var (_, currentWindow, _, _) = VerifyAndRetrieveCurrentWindowResources();

		// Hide the SystemBars and Status bar
		if (e.IsFullScreen)
		{
			isFullScreen = true;
			playerHeight = playerView.Height;
			playerWidth = playerView.Width;
			DisplayMetrics displayMetrics = new DisplayMetrics();
			currentWindow?.WindowManager?.DefaultDisplay?.GetMetrics(displayMetrics);
			var layout = currentWindow?.DecorView as ViewGroup;

			RemoveView(playerView);
			RelativeLayout.LayoutParams item = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
			item.Width = displayMetrics.WidthPixels;
			item.Height = displayMetrics.HeightPixels;
			layout?.AddView(playerView, item);
			SetSystemBarsVisibility();
		}
		else
		{
			isFullScreen = false;
			var layout = currentWindow?.DecorView as ViewGroup;
			RelativeLayout.LayoutParams item = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
			item.Width = playerWidth;
			item.Height = playerHeight;

			layout?.RemoveView(playerView);
			AddView(playerView, item);
			SetSystemBarsVisibility();
		}
	}

	void SetSystemBarsVisibility()
	{
		var (_, currentWindow, _, _) = VerifyAndRetrieveCurrentWindowResources();

		var windowInsetsControllerCompat = WindowCompat.GetInsetsController(currentWindow, currentWindow.DecorView);

		var barTypes = WindowInsetsCompat.Type.StatusBars()
			| WindowInsetsCompat.Type.SystemBars()
			| WindowInsetsCompat.Type.NavigationBars();

		if (isFullScreen)
		{
			WindowCompat.SetDecorFitsSystemWindows(currentWindow, false);
			if (OperatingSystem.IsAndroidVersionAtLeast(30))
			{
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
			WindowCompat.SetDecorFitsSystemWindows(currentWindow, true);
			if (OperatingSystem.IsAndroidVersionAtLeast(30))
			{
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