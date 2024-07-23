using Android.Graphics;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Widget;
using Com.Google.Android.Exoplayer2.UI;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Primitives;
using static Android.Views.ViewGroup;
using Color = Android.Graphics.Color;
using CurrentPlatformActivity = CommunityToolkit.Maui.Core.Views.MauiMediaElement.CurrentPlatformContext;

namespace CommunityToolkit.Maui.Extensions;

partial class SubtitleExtensions : Java.Lang.Object
{
	readonly IDispatcher dispatcher;
	readonly RelativeLayout.LayoutParams? subtitleLayout;
	readonly StyledPlayerView styledPlayerView;
	TextView? subtitleView;
	public List<SubtitleCue>? Cues { get; set; }

	public SubtitleExtensions(StyledPlayerView styledPlayerView, IDispatcher dispatcher)
	{
		this.dispatcher = dispatcher;
		this.styledPlayerView = styledPlayerView;
		Cues = [];
		subtitleLayout = new RelativeLayout.LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent);
		subtitleLayout.AddRule(LayoutRules.AlignParentBottom);
		subtitleLayout.AddRule(LayoutRules.CenterHorizontal);
		InitializeTextBlock();
		MediaManager.FullScreenEvents.WindowsChanged += OnFullScreenChanged;
	}

	public void StartSubtitleDisplay()
	{
		ArgumentNullException.ThrowIfNull(subtitleView);
		ArgumentNullException.ThrowIfNull(Cues);
		ArgumentNullException.ThrowIfNull(MediaElement);
		if (MediaElement.SubtitleUrl is null)
		{
			return;
		}

		Cues = Document?.Cues;
		
		if (styledPlayerView.Parent is not ViewGroup parent)
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
		if (Timer is not null)
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
				DisplayCue(cue);
			}
			else
			{
				subtitleView.Text = string.Empty;
				subtitleView.Visibility = ViewStates.Gone;
			}
		});
	}

	void DisplayCue(SubtitleCue cue)
	{
		ArgumentNullException.ThrowIfNull(MediaElement);
		ArgumentNullException.ThrowIfNull(subtitleView);
		if (cue.ParsedCueText is null)
		{
			return;
		}

		var spannableString = new SpannableStringBuilder();
		ProcessCueText(spannableString, cue.ParsedCueText);
		subtitleView.TextFormatted = spannableString;

		ApplyStyles(cue);
		subtitleView.Visibility = ViewStates.Visible;
	}

	static void ProcessCueText(SpannableStringBuilder spannableString, SubtitleNode node)
	{
		foreach (var child in node.Children)
		{
			if (child.NodeType == "text")
			{
				string? text = child.TextContent;
				if (!string.IsNullOrEmpty(text))
				{
					int start = spannableString.Length();
					spannableString.Append(text);
					int end = spannableString.Length();
					ApplyStyleToSpan(spannableString, child.NodeType, start, end);
				}
			}
			else if (child.NodeType is not null)
			{
				int start = spannableString.Length();
				ProcessCueText(spannableString, child);
				int end = spannableString.Length();
				ApplyStyleToSpan(spannableString, child.NodeType, start, end);
			}
		}
	}

	static void ApplyStyleToSpan(SpannableStringBuilder spannableString, string nodeType, int start, int end)
	{
		switch (nodeType.ToLower())
		{
			case "b":
				spannableString.SetSpan(new StyleSpan(TypefaceStyle.Bold), start, end, SpanTypes.ExclusiveExclusive);
				break;
			case "i":
				spannableString.SetSpan(new StyleSpan(TypefaceStyle.Italic), start, end, SpanTypes.ExclusiveExclusive);
				break;
			case "u":
				spannableString.SetSpan(new UnderlineSpan(), start, end, SpanTypes.ExclusiveExclusive);
				break;
			case "v":
				spannableString.SetSpan(new ForegroundColorSpan(Color.Yellow), start, end, SpanTypes.ExclusiveExclusive);
				break;
		}
	}

	void ApplyStyles(SubtitleCue cue)
	{
		ArgumentNullException.ThrowIfNull(MediaElement);
		ArgumentNullException.ThrowIfNull(subtitleView);
		ArgumentNullException.ThrowIfNull(subtitleLayout);

		subtitleView.Gravity = GetTextAlignment(cue.Align);

		Typeface? typeface = Typeface.CreateFromAsset(Platform.AppContext.ApplicationContext?.Assets, new Core.FontExtensions.FontFamily(MediaElement.SubtitleFont).Android) ?? Typeface.Default;
		subtitleView.SetTypeface(typeface, TypefaceStyle.Normal);
		subtitleView.TextSize = (float)MediaElement.SubtitleFontSize;

		if (!string.IsNullOrEmpty(cue.Position))
		{
			var parts = cue.Position.Split(',');
			if (parts.Length > 0 && float.TryParse(parts[0].TrimEnd('%'), out float horizontalPosition))
			{
				subtitleLayout.LeftMargin = (int)(horizontalPosition * styledPlayerView.Width / 100);
			}
		}

		if (!string.IsNullOrEmpty(cue.Line) && float.TryParse(cue.Line.TrimEnd('%'), out float verticalPosition))
		{
			subtitleLayout.BottomMargin = (int)(verticalPosition * styledPlayerView.Height / 100);
		}

		subtitleView.LayoutParameters = subtitleLayout;

		if (cue.Vertical is not null)
		{
			ApplyVerticalWriting(cue.Vertical);
		}
	}

	static GravityFlags GetTextAlignment(string align)
	{
		return align?.ToLower() switch
		{
			"left" => GravityFlags.Left,
			"right" => GravityFlags.Right,
			"center" => GravityFlags.Center,
			_ => GravityFlags.Center,
		};
	}

	void ApplyVerticalWriting(string vertical)
	{
		ArgumentNullException.ThrowIfNull(subtitleView);
		if (vertical == "rl" || vertical == "lr")
		{
			subtitleView.Rotation = vertical == "rl" ? 90 : -90;
		}
		else
		{
			subtitleView.Rotation = 0;
		}
	}

	void InitializeTextBlock()
	{
		subtitleView = new TextView(CurrentPlatformActivity.CurrentActivity.ApplicationContext)
		{
			Text = string.Empty,
			HorizontalScrollBarEnabled = false,
			VerticalScrollBarEnabled = false,
			Gravity = GravityFlags.Center,
			Visibility = ViewStates.Gone,
			LayoutParameters = subtitleLayout
		};
		subtitleView.SetBackgroundColor(Color.Argb(150, 0, 0, 0));
		subtitleView.SetTextColor(Color.White);
		subtitleView.SetPadding(10, 10, 10, 20);
	}

	void OnFullScreenChanged(object? sender, FullScreenStateChangedEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(subtitleView);
		ArgumentNullException.ThrowIfNull(MediaElement);
		ArgumentNullException.ThrowIfNull(subtitleLayout);
		if (string.IsNullOrEmpty(MediaElement.SubtitleUrl))
		{
			return;
		}

		if (CurrentPlatformActivity.CurrentViewGroup.Parent is not ViewGroup parent)
		{
			return;
		}

		switch (e.NewState == MediaElementScreenState.FullScreen)
		{
			case true:
				CurrentPlatformActivity.CurrentViewGroup.RemoveView(subtitleView);
				InitializeTextBlock();
				subtitleView.TextSize = (float)MediaElement.SubtitleFontSize + 8.0f;
				subtitleLayout.BottomMargin = 300;
				parent.AddView(subtitleView);
				break;
			case false:
				parent.RemoveView(subtitleView);
				InitializeTextBlock();
				subtitleView.TextSize = (float)MediaElement.SubtitleFontSize;
				subtitleLayout.BottomMargin = 20;
				CurrentPlatformActivity.CurrentViewGroup.AddView(subtitleView);
				break;
		}
	}
}
