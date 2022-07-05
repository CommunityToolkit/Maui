using Android.Content;
using Android.Views;
using CommunityToolkit.Maui.Core.Extensions;
using Java.Lang;

namespace CommunityToolkit.Maui.Core.Views;

public partial class MauiExpander : LinearLayout
{
	View? content;
	View? header;

	/// <summary>
	/// Initialize a new instance of <see cref="MauiExpander" />.
	/// </summary>
	public MauiExpander(Context context) : base(context)
	{
	}
	
	/// <summary>
	/// Expander header
	/// </summary>
	public View? Header
	{
		get => header;
		set
		{
			header = value;
			Draw();
		}
	}

	/// <summary>
	/// Expander expandable content
	/// </summary>
	public View? Content
	{
		get => content;
		set
		{
			content = value;
			Draw();
		}
	}

	partial void Draw()
	{
		if (Header is null || Content is null)
		{
			return;
		}

		Orientation = Orientation.Vertical;
		RemoveAllViews();

		ConfigureHeader();
		if (ExpandDirection == ExpandDirection.Down)
		{
			AddView(Header);
		}

		AddView(Content);
		UpdateContentVisibility(IsExpanded);

		if (ExpandDirection == ExpandDirection.Up)
		{
			AddView(Header);
		}
	}

	partial void UpdateContentVisibility(bool isVisible)
	{
		if (Content is not null)
		{
			Content.Visibility = isVisible ? ViewStates.Visible : ViewStates.Gone;
		}
	}

	void ConfigureHeader()
	{
		if (Header is null)
		{
			return;
		}

		Header.Clickable = true;
		Header.SetOnClickListener(new HeaderClickEventListener(this));
	}

	class HeaderClickEventListener : Java.Lang.Object, IOnClickListener
	{
		readonly MauiExpander expander;

		public HeaderClickEventListener(MauiExpander expander)
		{
			this.expander = expander;
		}

		public void OnClick(View? v)
		{
			if (expander.Content is null)
			{
				return;
			}

			var animation = expander.Content.Animate()?.Alpha(expander.IsExpanded ? 0.0f : 1.0f)?.SetDuration(expander.AnimationDuration);
			if (animation is null)
			{
				return;
			}

			var runnable = new Runnable(() => expander.SetIsExpanded(!expander.IsExpanded));
			if (expander.IsExpanded)
			{
				animation.WithEndAction(runnable);
			}
			else
			{
				animation.WithStartAction(runnable);
			}

			expander.Content.Alpha = expander.IsExpanded ? 0.0f : 1.0f;
			animation.Start();
		}
	}
}