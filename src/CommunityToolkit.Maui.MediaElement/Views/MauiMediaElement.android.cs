using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.CoordinatorLayout.Widget;
using AndroidX.Core.View;
using AndroidX.Media3.UI;
using CommunityToolkit.Maui.Views;
using View = Android.Views.View;
using Window = Android.Views.Window;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The user-interface element that represents the <see cref="MediaElement"/> on Android.
/// </summary>
public class MauiMediaElement : CoordinatorLayout
{
	readonly RelativeLayout relativeLayout;
	readonly PlayerView playerView;

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
	/// <param name="playerView">The <see cref="PlayerView"/> that acts as the platform media player.</param>
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
	protected override void OnVisibilityChanged(global::Android.Views.View changedView, [GeneratedEnum] ViewStates visibility)
	{
		base.OnVisibilityChanged(changedView, visibility);
		if (isFullScreen && visibility is ViewStates.Visible)
		{
			SetStatusBarsHidden(isFullScreen);
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
			relativeLayout.Invalidate();
		}
		// Hide/Show the SystemBars and Status bar
		SetStatusBarsHidden(isFullScreen);
	}

	public static void SetStatusBarsHidden(bool hidden)
	{
		Window window = Platform.CurrentActivity?.Window ?? throw new InvalidOperationException("Current activity is null");
		View decorView = window.DecorView ?? throw new InvalidOperationException("DecorView is null");
		AndroidX.Core.View.WindowInsetsControllerCompat insetsController = WindowCompat.GetInsetsController(window, decorView) ?? throw new InvalidOperationException("InsetsController is null");
		if (OperatingSystem.IsAndroidVersionAtLeast(26))
		{
			if (hidden)
			{
				window.ClearFlags(WindowManagerFlags.LayoutNoLimits);
				window.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
				insetsController.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
				insetsController.Hide(WindowInsetsCompat.Type.SystemBars());
			}
			else
			{
				window.ClearFlags(WindowManagerFlags.Fullscreen);
				window.SetFlags(WindowManagerFlags.DrawsSystemBarBackgrounds, WindowManagerFlags.DrawsSystemBarBackgrounds);
				insetsController.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorDefault;
				insetsController.Show(WindowInsetsCompat.Type.SystemBars());
			}
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