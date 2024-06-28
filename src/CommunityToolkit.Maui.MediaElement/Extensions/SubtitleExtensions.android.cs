using Android.Graphics;
using Android.Views;
using Android.Widget;
using Com.Google.Android.Exoplayer2.UI;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Primitives;
using static Android.Views.ViewGroup;
using CurrentPlatformActivity = CommunityToolkit.Maui.Extensions.PageExtensions.CurrentPlatformActivity;

namespace CommunityToolkit.Maui.Extensions;

partial class SubtitleExtensions : Java.Lang.Object
{
	readonly IDispatcher dispatcher;
	readonly RelativeLayout.LayoutParams? subtitleLayout;
	readonly StyledPlayerView styledPlayerView;
	
	TextView? subtitleView;

	public SubtitleExtensions(StyledPlayerView styledPlayerView, IDispatcher dispatcher)
	{
		this.dispatcher = dispatcher;
		this.styledPlayerView = styledPlayerView;
		Cues = [];
		subtitleLayout = new RelativeLayout.LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent);
		subtitleLayout.AddRule(LayoutRules.AlignParentBottom);
		subtitleLayout.AddRule(LayoutRules.CenterHorizontal);

		InitializeTextBlock();
		MauiMediaElement.FullScreenChanged += OnFullScreenChanged;
	}

	public void StartSubtitleDisplay()
	{
		ArgumentNullException.ThrowIfNull(subtitleView);
		ArgumentNullException.ThrowIfNull(Cues);
		if(Cues.Count == 0 || string.IsNullOrEmpty(MediaElement?.SubtitleUrl))
		{
			return;
		}

		if(styledPlayerView.Parent is not ViewGroup parent)
		{
			System.Diagnostics.Trace.TraceError("StyledPlayerView parent is not a ViewGroup");
			return;
		}
		dispatcher.Dispatch(() => parent.AddView(subtitleView));
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

		if (styledPlayerView.Parent is ViewGroup parent)
		{
			dispatcher.Dispatch(() => parent.RemoveView(subtitleView));
		}
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
			if (cue is not null)
			{
				Typeface? typeface = Typeface.CreateFromAsset(Platform.AppContext.ApplicationContext?.Assets, new Core.FontExtensions.FontFamily(MediaElement.SubtitleFont).Android) ?? Typeface.Default;
				subtitleView.SetTypeface(typeface, TypefaceStyle.Normal);
				subtitleView.Text = cue.Text;
				subtitleView.TextSize = (float)MediaElement.SubtitleFontSize;
				subtitleView.Visibility = ViewStates.Visible;
			}
			else
			{
				subtitleView.Text = string.Empty;
				subtitleView.Visibility = ViewStates.Gone;
			}
		});
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

	void OnFullScreenChanged(object? sender, FullScreenEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(subtitleView);
		ArgumentNullException.ThrowIfNull(MediaElement);

		// If the subtitle URL is empty do nothing
		if (string.IsNullOrEmpty(MediaElement.SubtitleUrl))
		{
			return;
		}
		
		if (CurrentPlatformActivity.CurrentViewGroup.Parent is not ViewGroup parent)
		{
			return;
		}

		switch (e.isFullScreen)
		{
			case true:
				CurrentPlatformActivity.CurrentViewGroup.RemoveView(subtitleView);
				InitializeTextBlock();
				parent.AddView(subtitleView);
				break;
			case false:
				parent.RemoveView(subtitleView);
				InitializeTextBlock();
				CurrentPlatformActivity.CurrentViewGroup.AddView(subtitleView);
				break;
		}
	}
}
