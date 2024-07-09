using Android.App;
using Android.Content;
using Android.Runtime;
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
	readonly RelativeLayout relativeLayout;

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

		var layout = new RelativeLayout.LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent);
		layout.AddRule(LayoutRules.CenterInParent);
		layout.AddRule(LayoutRules.CenterVertical);
		layout.AddRule(LayoutRules.CenterHorizontal);
		relativeLayout = new RelativeLayout(Platform.AppContext)
		{
			LayoutParameters = layout,
		};
		relativeLayout.AddView(playerView);

		AddView(relativeLayout);
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

	void OnFullscreenButtonClick(object? sender, StyledPlayerView.FullscreenButtonClickEventArgs e)
	{
		// Ensure there is a player view
		if (playerView is null)
		{
			throw new InvalidOperationException("PlayerView cannot be null when the FullScreen button is tapped");
		}
		var layout = CurrentPlatformContext.CurrentWindow.DecorView as ViewGroup;

		if (e.IsFullScreen)
		{
			isFullScreen = true;
			RemoveView(relativeLayout);
			layout?.AddView(relativeLayout);
		}
		else
		{
			isFullScreen = false;
			layout?.RemoveView(relativeLayout);
			AddView(relativeLayout);
		}
		// Hide/Show the SystemBars and Status bar
		SetSystemBarsVisibility();
	}

	void SetSystemBarsVisibility()
	{
		var currentWindow = CurrentPlatformContext.CurrentWindow;
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

	readonly record struct CurrentPlatformContext
	{
		public static Activity CurrentActivity
		{
			get
			{
				if (Platform.CurrentActivity is null)
				{
					throw new InvalidOperationException("CurrentActivity cannot be null");
				}

				return Platform.CurrentActivity;
			}
		}

		public static Android.Views.Window CurrentWindow
		{
			get
			{
				if (CurrentActivity.Window is null)
				{
					throw new InvalidOperationException("Window cannot be null");
				}

				return CurrentActivity.Window;
			}
		}

		public static ViewGroup CurrentViewGroup
		{
			get
			{
				if (CurrentWindow.DecorView is not ViewGroup viewGroup)
				{
					throw new InvalidOperationException("DecorView cannot be null");
				}

				return viewGroup;
			}
		}
	}
}