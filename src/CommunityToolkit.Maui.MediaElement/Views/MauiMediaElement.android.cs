using System.Diagnostics.CodeAnalysis;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.CoordinatorLayout.Widget;
using AndroidX.Core.View;
using AndroidX.Media3.UI;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The user-interface element that represents the <see cref="MediaElement"/> on Android.
/// </summary>
public class MauiMediaElement : CoordinatorLayout
{
	readonly RelativeLayout relativeLayout;
	readonly PlayerView playerView;

	int defaultSystemUiVisibility;
	int defaultSystemBarsBehavior = WindowInsetsControllerCompat.BehaviorDefault;
	bool isStatusBarVisible;
	bool isNavigationBarVisible;
	bool isFullScreen;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
#pragma warning disable IDE0060 // Remove unused parameter
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(MediaElement))]
	public MauiMediaElement(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
	{
		// Fixes no constructor found exception: https://github.com/CommunityToolkit/Maui/issues/3114
	}

	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(MediaElement))]
	public MauiMediaElement(Context? context) : base(context)
	{
		// Fixes no constructor found exception: https://github.com/CommunityToolkit/Maui/issues/3114
	}

	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(MediaElement))]
	public MauiMediaElement(Context? context, IAttributeSet? attrs) : base(context, attrs)
	{
		// Fixes no constructor found exception: https://github.com/CommunityToolkit/Maui/issues/3114
	}

	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(MediaElement))]
	public MauiMediaElement(Context? context, IAttributeSet? attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
	{
		// Fixes no constructor found exception: https://github.com/CommunityToolkit/Maui/pull/1692#issuecomment-1955099758
	}
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
#pragma warning restore IDE0060 // Remove unused parameter

	/// <summary>
	/// Initializes a new instance of the <see cref="MauiMediaElement"/> class.
	/// </summary>
	/// <param name="context">The application's <see cref="Context"/>.</param>
	/// <param name="playerView">The <see cref="PlayerView"/> that acts as the platform media player.</param>
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(MediaElement))]
	public MauiMediaElement(Context context, PlayerView playerView) : base(context)
	{
		this.playerView = playerView;
		this.playerView.SetBackgroundColor(global::Android.Graphics.Color.Black);
		playerView.FullscreenButtonClick += OnFullscreenButtonClick;
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

	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(MediaElement))]
	public override void OnDetachedFromWindow()
	{
		if (isFullScreen)
		{
			OnFullscreenButtonClick(this, new PlayerView.FullscreenButtonClickEventArgs(!isFullScreen));

			// Full screen was left without the full-screen button: the button must leave its full-screen state too, or its next tap asks to
			// leave a full screen that the view is no longer in. Media3 reports the new state to the click listener, which ignores it.
			playerView.SetFullscreenButtonState(false);
		}
		base.OnDetachedFromWindow();
	}

	/// <summary>
	/// Checks the visibility of the view
	/// </summary>
	/// <param name="changedView"></param>
	/// <param name="visibility"></param>
	protected override void OnVisibilityChanged(global::Android.Views.View changedView, [GeneratedEnum] ViewStates visibility)
	{
		base.OnVisibilityChanged(changedView, visibility);
		if (isFullScreen && visibility is ViewStates.Visible)
		{
			// Full screen is applied again: leaving it must still restore the system bars of before it was entered
			SetSystemBarsVisibility(saveSystemBarsVisibility: false);
		}
	}

	/// <summary>
	/// Releases the unmanaged resources used by the <see cref="MediaElement"/> and optionally releases the managed resources.
	/// </summary>
	/// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
	protected override void Dispose(bool disposing)
	{
		// Only the constructor that takes a PlayerView sets playerView: a view that Android created or wrapped has no player to release
		if (disposing && playerView is not null)
		{
			try
			{
				if (playerView.Player is not null)
				{
					playerView.Player.PlayWhenReady = false;
				}
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

	void OnFullscreenButtonClick(object? sender, PlayerView.FullscreenButtonClickEventArgs e)
	{
		// Ensure there is a player view
		if (playerView is null)
		{
			throw new InvalidOperationException("UpdatedPlayerView cannot be null when the FullScreen button is tapped");
		}

		// The view is in this state already: PlayerView.SetFullscreenButtonState reports the state that OnDetachedFromWindow gives the button
		if (e.P0 == isFullScreen)
		{
			return;
		}

		var layout = CurrentPlatformContext.CurrentWindow.DecorView as ViewGroup;
		// `p0` is the boolean value of isFullScreen being passed into the method. 
		// This is a binding issue that will not be fixed as it is now part of shipped API.
		if (e.P0)
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
		SetSystemBarsVisibility(saveSystemBarsVisibility: e.P0);
	}

	void SetSystemBarsVisibility(bool saveSystemBarsVisibility)
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
					// Each bar is saved on its own: an app can hide one of them alone
					var isStatusBarVisibleNow = windowInsets.IsVisible(WindowInsetsCompat.Type.StatusBars());
					var isNavigationBarVisibleNow = windowInsets.IsVisible(WindowInsetsCompat.Type.NavigationBars());
					if (saveSystemBarsVisibility)
					{
						isStatusBarVisible = isStatusBarVisibleNow;
						isNavigationBarVisible = isNavigationBarVisibleNow;
					}

					if (isStatusBarVisibleNow || isNavigationBarVisibleNow)
					{
						currentWindow.InsetsController?.Hide(WindowInsets.Type.SystemBars());
					}
				}
			}
			else
			{
				if (saveSystemBarsVisibility)
				{
					defaultSystemUiVisibility = (int)currentWindow.DecorView.SystemUiFlags;
				}

				currentWindow.DecorView.SystemUiFlags = currentWindow.DecorView.SystemUiFlags
					| SystemUiFlags.LayoutStable
					| SystemUiFlags.LayoutHideNavigation
					| SystemUiFlags.LayoutFullscreen
					| SystemUiFlags.HideNavigation
					| SystemUiFlags.Fullscreen
					| SystemUiFlags.Immersive;
			}

			if (windowInsetsControllerCompat is not null)
			{
				if (saveSystemBarsVisibility)
				{
					defaultSystemBarsBehavior = windowInsetsControllerCompat.SystemBarsBehavior;
				}

				windowInsetsControllerCompat.Hide(barTypes);
				windowInsetsControllerCompat.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
			}

		}
		else
		{
			// The behavior is restored first: before Android 11 it is applied through the system UI flags, which are then restored exactly.
			if (windowInsetsControllerCompat is not null)
			{
				windowInsetsControllerCompat.SystemBarsBehavior = defaultSystemBarsBehavior;
			}

			if (OperatingSystem.IsAndroidVersionAtLeast(30))
			{
				if (isStatusBarVisible || isNavigationBarVisible)
				{
					var hiddenBars = (isStatusBarVisible ? 0 : WindowInsets.Type.StatusBars()) | (isNavigationBarVisible ? 0 : WindowInsets.Type.NavigationBars());
					currentWindow.InsetsController?.Show(WindowInsets.Type.SystemBars() & ~hiddenBars);
				}
			}
			else
			{
				currentWindow.DecorView.SystemUiFlags = (SystemUiFlags)defaultSystemUiVisibility;
			}

			WindowCompat.SetDecorFitsSystemWindows(currentWindow, true);
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

		public static global::Android.Views.Window CurrentWindow
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
	}
}