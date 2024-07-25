using Android.Graphics;
using Android.Views;
using Android.Widget;
using Com.Google.Android.Exoplayer2.UI;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Primitives;
using static Android.Views.ViewGroup;
using static CommunityToolkit.Maui.Core.Views.MauiMediaElement;
using CurrentPlatformActivity = CommunityToolkit.Maui.Core.Views.MauiMediaElement.CurrentPlatformContext;

namespace CommunityToolkit.Maui.Extensions;

partial class SubtitleExtensions : Java.Lang.Object
{
	readonly IDispatcher dispatcher;
	FrameLayout.LayoutParams? subtitleLayout;
	readonly StyledPlayerView styledPlayerView;
	TextView? subtitleView;
	MediaElementScreenState screenState;

	public SubtitleExtensions(StyledPlayerView styledPlayerView, IDispatcher dispatcher)
	{
		screenState = MediaElementScreenState.Default;
		this.dispatcher = dispatcher;
		this.styledPlayerView = styledPlayerView;
		Cues = [];
		InitializeLayout();
		InitializeTextBlock();
		MediaManager.FullScreenEvents.WindowsChanged += OnFullScreenChanged;
	}

	~SubtitleExtensions()
	{
		if (Timer is not null)
		{
			Timer.Stop();
			Timer.Elapsed -= UpdateSubtitle;
		}
		MediaManager.FullScreenEvents.WindowsChanged -= OnFullScreenChanged;
	}
	public void StartSubtitleDisplay()
	{
		ArgumentNullException.ThrowIfNull(subtitleView);
		ArgumentNullException.ThrowIfNull(Cues);
		if(Cues.Count == 0 || string.IsNullOrEmpty(MediaElement?.SubtitleUrl))
		{
			return;
		}

		InitializeText();
		dispatcher.Dispatch(() => styledPlayerView.AddView(subtitleView));

		Timer = new System.Timers.Timer(1000);
		Timer.Elapsed += UpdateSubtitle;
		Timer.Start();
	}

	public void StopSubtitleDisplay()
	{
		ArgumentNullException.ThrowIfNull(Cues);
		Cues.Clear();
		if(Timer is not null)
		{
			Timer.Stop();
			Timer.Elapsed -= UpdateSubtitle;
		}
		if (subtitleView is null)
		{
			return;
		}
		subtitleView.Text = string.Empty;
		dispatcher.Dispatch(() => styledPlayerView.RemoveView(subtitleView));
	}

	void UpdateSubtitle(object? sender, System.Timers.ElapsedEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(subtitleView);
		ArgumentNullException.ThrowIfNull(MediaElement);
		ArgumentNullException.ThrowIfNull(Cues);

		if (Cues.Count == 0)
		{
			return;
		}
		
		var cue = Cues.Find(c => c.StartTime <= MediaElement.Position && c.EndTime >= MediaElement.Position);
		dispatcher.Dispatch(() =>
		{

			SetHeight();
			if (cue is not null)
			{
				subtitleView.Text = cue.Text;
				subtitleView.Visibility = ViewStates.Visible;
			}
			else
			{
				subtitleView.Text = string.Empty;
				subtitleView.Visibility = ViewStates.Gone;
			}
		});
	}

	void OnFullScreenChanged(object? sender, FullScreenStateChangedEventArgs e)
	{
		var layout = CurrentPlatformContext.CurrentWindow.DecorView as ViewGroup;
		ArgumentNullException.ThrowIfNull(layout);
		if (e.NewState == MediaElementScreenState.FullScreen)
		{
			screenState = MediaElementScreenState.FullScreen;
			styledPlayerView.RemoveView(subtitleView);
			InitializeLayout();
			InitializeTextBlock();
			InitializeText();
			layout.AddView(subtitleView);
				
		}
		else
		{
			screenState = MediaElementScreenState.Default;
			layout.RemoveView(subtitleView);
			InitializeLayout();
			InitializeTextBlock();
			InitializeText();
			styledPlayerView.AddView(subtitleView);
		}
	}
	void SetHeight()
	{
		int height = styledPlayerView.Height;
		switch (screenState)
		{
			case MediaElementScreenState.Default:
				height = (int)(height * 0.1);
				break;
			case MediaElementScreenState.FullScreen:
				height = (int)(height * 0.2);
				break;
		}
		dispatcher.Dispatch(() => subtitleLayout?.SetMargins(20, 0, 20, height));
	}
	void InitializeText()
	{
		ArgumentNullException.ThrowIfNull(subtitleView);
		ArgumentNullException.ThrowIfNull(MediaElement);
		Typeface? typeface = Typeface.CreateFromAsset(Platform.AppContext.ApplicationContext?.Assets, new Core.FontExtensions.FontFamily(MediaElement.SubtitleFont).Android) ?? Typeface.Default;
		subtitleView.TextSize = (float)MediaElement.SubtitleFontSize;
		subtitleView.SetTypeface(typeface, TypefaceStyle.Normal);
	}
	void InitializeTextBlock()
	{
		subtitleView = new(CurrentPlatformActivity.CurrentActivity.ApplicationContext)
		{
			Text = string.Empty,
			HorizontalScrollBarEnabled = false,
			VerticalScrollBarEnabled = false,
			TextAlignment = Android.Views.TextAlignment.Center,
			Visibility = Android.Views.ViewStates.Gone,
			LayoutParameters = subtitleLayout
		};
		subtitleView.SetBackgroundColor(Android.Graphics.Color.Argb(150, 0, 0, 0));
		subtitleView.SetTextColor(Android.Graphics.Color.White);
		subtitleView.SetPaddingRelative(10, 10, 10, 20);
	}
	void InitializeLayout()
	{
		subtitleLayout = new FrameLayout.LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent)
		{
			Gravity = GravityFlags.Center | GravityFlags.Bottom,
		};
	}
}
