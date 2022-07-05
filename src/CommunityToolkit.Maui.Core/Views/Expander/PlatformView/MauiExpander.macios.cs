namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// iOS/macOS Platform Expander
/// </summary>
public partial class MauiExpander : UIStackView
{
	UIView? header;
	UIView? content;
	bool isExpanded;
	ExpandDirection expandDirection;
	readonly WeakEventManager weakEventManager = new();

	/// <summary>
	/// Event invoked when IsExpanded changed
	/// </summary>
	public event EventHandler<ExpanderCollapsedEventArgs> Collapsed
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Expander header
	/// </summary>
	public UIView? Header
	{
		get => header;
		set
		{
			header = value;
			Draw();
		}
	}

	/// <summary>
	/// Expander content
	/// </summary>
	public UIView? Content
	{
		get => content;
		set
		{
			content = value;
			Draw();
		}
	}

	/// <summary>
	/// Returns true if expander is expanded
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
	/// Gets or sets Expander expand direction
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

		foreach (var subView in ArrangedSubviews)
		{
			RemoveArrangedSubview(subView);
		}

		Axis = UILayoutConstraintAxis.Vertical;

		ConfigureHeader();
		if (ExpandDirection == ExpandDirection.Down)
		{
			AddArrangedSubview(Header);
		}

		ConfigureContent();
		AddArrangedSubview(Content);

		if (ExpandDirection == ExpandDirection.Up)
		{
			AddArrangedSubview(Header);
		}
	}

	void ConfigureHeader()
	{
		if (Header is null)
		{
			return;
		}

		var expanderGesture = new UITapGestureRecognizer();
		expanderGesture.AddTarget(() => {
			if (Content is null)
			{
				return;
			}

			Transition(Content, 0.3, UIViewAnimationOptions.CurveLinear,
				() => IsExpanded = !IsExpanded,
				() => { });
		});
		Header.GestureRecognizers = new UIGestureRecognizer[]
		{
			expanderGesture
		};
	}

	void UpdateContentVisibility(bool isVisible)
	{
		if (Content is not null)
		{
			Content.Hidden = !isVisible;	
		}
	}

	void ConfigureContent()
	{
		if (Content is null)
		{
			return;
		}

		UpdateContentVisibility(IsExpanded);
		Content.ClipsToBounds = true;
	}
}