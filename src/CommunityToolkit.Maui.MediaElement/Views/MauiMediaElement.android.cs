﻿using Android.App;
using Android.Content;
using Android.Graphics.Drawables;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.CoordinatorLayout.Widget;
using AndroidX.Core.View;
using AndroidX.Media3.UI;
using CommunityToolkit.Maui.Views;
using RelativeLayout = Android.Widget.RelativeLayout;

[assembly: UsesPermission(Android.Manifest.Permission.ForegroundServiceMediaPlayback)]
[assembly: UsesPermission(Android.Manifest.Permission.ForegroundService)]
[assembly: UsesPermission(Android.Manifest.Permission.MediaContentControl)]
[assembly: UsesPermission(Android.Manifest.Permission.PostNotifications)]

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The user-interface element that represents the <see cref="MediaElement"/> on Android.
/// </summary>
public class MauiMediaElement : CoordinatorLayout
{
	readonly RelativeLayout relativeLayout;
	readonly PlayerView playerView;

	int defaultSystemUiVisibility;
	bool isSystemBarVisible;
	bool isFullScreen;

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
	/// <param name="playerView">The <see cref="AndroidX.Media3.UI.PlayerView"/> that acts as the platform media player.</param>
	public MauiMediaElement(Context context, PlayerView playerView) : base(context)
	{
		this.playerView = playerView;
		playerView.Background = new ColorDrawable(Android.Graphics.Color.Black);
		playerView.SetBackgroundColor(Android.Graphics.Color.Black);
		playerView.FullscreenButtonClick += OnFullscreenButtonClick;
		playerView.SetShowBuffering(PlayerView.ShowBufferingAlways);
		playerView.Alpha = 1.0f;
		playerView.ArtworkDisplayMode = PlayerView.ArtworkDisplayModeFit;
		playerView.DefaultArtwork = new ColorDrawable(Android.Graphics.Color.Black);

		var layout = new RelativeLayout.LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent);
		layout.AddRule(LayoutRules.CenterInParent);
		layout.AddRule(LayoutRules.CenterVertical);
		layout.AddRule(LayoutRules.CenterHorizontal);
		relativeLayout = new RelativeLayout(Platform.AppContext)
		{
			LayoutParameters = layout
		};
		SetBackgroundResource(Android.Resource.Color.Black);
	}

	public void SetView(AndroidX.Media3.Session.MediaController mediaController)
	{
		playerView.Player = mediaController;
		relativeLayout.AddView(playerView);
		AddView(relativeLayout);
	}

	public override void OnDetachedFromWindow()
	{
		if (isFullScreen)
		{
			OnFullscreenButtonClick(this, new PlayerView.FullscreenButtonClickEventArgs(!isFullScreen));
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
		if (isFullScreen && visibility is ViewStates.Visible)
		{
			SetSystemBarsVisibility();
		}
	}

	void OnFullscreenButtonClick(object? sender, PlayerView.FullscreenButtonClickEventArgs e)
	{
		// Ensure there is a player view
		if (playerView is null)
		{
			throw new InvalidOperationException("UpdatedPlayerView cannot be null when the FullScreen button is tapped");
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

			if (windowInsetsControllerCompat is not null)
			{
				windowInsetsControllerCompat.Hide(barTypes);
				windowInsetsControllerCompat.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
			}

		}
		else
		{
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

			if (windowInsetsControllerCompat is not null)
			{
				windowInsetsControllerCompat.Show(barTypes);
				windowInsetsControllerCompat.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorDefault;
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
	}
}