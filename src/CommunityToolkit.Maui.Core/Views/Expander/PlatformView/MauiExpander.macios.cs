namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// iOS/macOS Platform Expander
/// </summary>
public partial class MauiExpander : UIStackView
{
	UIView? header;
	UIView? content;

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

	partial void Draw()
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

			Transition(Content, AnimationDuration / 1000d, UIViewAnimationOptions.CurveLinear,
				() => IsExpanded = !IsExpanded,
				() => { });
		});
		Header.GestureRecognizers = new UIGestureRecognizer[]
		{
			expanderGesture
		};
	}

	partial void UpdateContentVisibility(bool isVisible)
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