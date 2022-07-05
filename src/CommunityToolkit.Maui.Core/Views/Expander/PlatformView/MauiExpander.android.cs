using Android.Content;
using Android.Views;
using CommunityToolkit.Maui.Core.Extensions;
using Java.Lang;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// 
/// </summary>
public partial class MauiExpander : LinearLayout
{
	View? content;
	View? header;
	bool isExpanded;
	ExpandDirection expandDirection;
	readonly WeakEventManager weakEventManager = new();


	/// <summary>
	/// Initialize a new instance of <see cref="MauiExpander" />.
	/// </summary>
	public MauiExpander(Context context) : base(context)
	{
	}

	/// <summary>
	/// 
	/// </summary>
	public event EventHandler<ExpanderCollapsedEventArgs> Collapsed
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}
	/// <summary>
	/// 
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
	/// 
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

	/// <summary>
	/// 
	/// </summary>
	public bool IsExpanded
	{
		get => isExpanded;
		set
		{
			isExpanded = value;
			UpdateContentVisibility(value);
			weakEventManager.HandleEvent(this, new ExpanderCollapsedEventArgs(!value), nameof(Collapsed));
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public ExpandDirection ExpandDirection
	{
		get => expandDirection;
		set
		{
			expandDirection = value;
			Draw();
		}
	}

	void Draw()
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

	void UpdateContentVisibility(bool isVisible)
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

		public void OnClick(Android.Views.View? v)
		{
			if (expander.Content is null)
			{
				return;
			}

			var animation = expander.Content.Animate()?.Alpha(expander.IsExpanded ? 0.0f : 1.0f)?.SetDuration(300);
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