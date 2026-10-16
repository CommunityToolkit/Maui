using CommunityToolkit.Maui.Core.Extensions;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// <see cref="UIView"/> for <see cref="Alert"/>
/// </summary>
/// <param name="shouldFillAndExpandHorizontally">Should stretch container horizontally to fit the screen</param>
public class AlertView(bool shouldFillAndExpandHorizontally) : UIView
{
	const int defaultSpacing = 10;

	readonly List<UIView> children = [];
	readonly bool shouldFillAndExpandHorizontally = shouldFillAndExpandHorizontally;

	// The constraints that place the view vertically: at the bottom of the safe area, or above or below placedAnchorView
	NSLayoutConstraint[] verticalConstraints = [];
	UIView? placedAnchorView;
	bool isPlacedBelowAnchorView;

	/// <summary>
	/// Parent UIView
	/// </summary>
	public UIView ParentView { get; } = Microsoft.Maui.Platform.UIApplicationExtensions.GetKeyWindow(UIApplication.SharedApplication) ?? throw new InvalidOperationException("KeyWindow is not found");

	/// <summary>
	/// PopupView Children
	/// </summary>
	public IReadOnlyList<UIView> Children => children;

	/// <summary>
	/// <see cref="AlertViewVisualOptions"/>
	/// </summary>
	public AlertViewVisualOptions VisualOptions { get; } = new();

	/// <summary>
	/// <see cref="UIView"/> on which Alert will appear. When null, <see cref="AlertView"/> will appear at bottom of screen.
	/// </summary>
	public UIView? AnchorView { get; set; }

	/// <summary>
	/// Container of <see cref="AlertView"/>
	/// </summary>
	protected UIStackView Container { get; } = new()
	{
		Alignment = UIStackViewAlignment.Fill,
		Distribution = UIStackViewDistribution.EqualSpacing,
		Axis = UILayoutConstraintAxis.Horizontal,
		TranslatesAutoresizingMaskIntoConstraints = false
	};

	/// <summary>
	/// Dismisses the Popup from the screen
	/// </summary>
	public void Dismiss() => RemoveFromSuperview();

	/// <summary>
	/// Adds a <see cref="UIView"/> to <see cref="Children"/>
	/// </summary>
	/// <param name="child"></param>
	public void AddChild(UIView child)
	{
		children.Add(child);
		Container.AddArrangedSubview(child);
	}

	/// <summary>
	/// Initializes <see cref="AlertView"/>
	/// </summary>
	public void Setup()
	{
		Initialize();
		SetParentConstraints();
	}

	/// <inheritdoc />
	public override void LayoutSubviews()
	{
		base.LayoutSubviews();
		UpdateVerticalConstraints();
	}

	// Places the view at the bottom of the safe area or, when it has an anchor, above the anchor if it fits between the top of the safe
	// area and the anchor, and below the anchor otherwise. This runs on every layout pass, so the constraints are only replaced when the
	// placement changes (or UIKit removed them together with a view they referred to): constraints added on every pass pile up and
	// contradict each other.
	void UpdateVerticalConstraints()
	{
		// UIKit throws (NSGenericException) when it activates a constraint between views that have no common ancestor: the view is only
		// constrained while it is in the parent view, and an anchor that is not, or no longer, in the parent view is ignored
		if (!IsDescendantOfView(ParentView))
		{
			return;
		}

		var anchorView = AnchorView?.IsDescendantOfView(ParentView) is true ? AnchorView : null;
		var isBelowAnchorView = anchorView is not null && !FitsAbove(anchorView);

		if (verticalConstraints.Length > 0
			&& anchorView == placedAnchorView
			&& isBelowAnchorView == isPlacedBelowAnchorView
			&& Array.TrueForAll(verticalConstraints, static constraint => constraint.Active))
		{
			return;
		}

		NSLayoutConstraint.DeactivateConstraints(verticalConstraints);

		verticalConstraints = anchorView switch
		{
			null =>
			[
				this.SafeBottomAnchor().ConstraintEqualTo(ParentView.SafeBottomAnchor(), -defaultSpacing),
				this.SafeTopAnchor().ConstraintGreaterThanOrEqualTo(ParentView.SafeTopAnchor(), defaultSpacing)
			],
			UIView anchor when isBelowAnchorView => [this.SafeTopAnchor().ConstraintEqualTo(anchor.SafeBottomAnchor(), defaultSpacing)],
			UIView anchor => [this.SafeBottomAnchor().ConstraintEqualTo(anchor.SafeTopAnchor())]
		};

		NSLayoutConstraint.ActivateConstraints(verticalConstraints);
		placedAnchorView = anchorView;
		isPlacedBelowAnchorView = isBelowAnchorView;
	}

	// Whether the view fits between the top of the parent's safe area and the anchor. The height comes from Container, which
	// base.LayoutSubviews has laid out: unlike the view's own frame, it does not depend on where the view is placed (the view grows by
	// the safe area insets it overlaps) and it is already known on the first layout pass.
	bool FitsAbove(UIView anchorView)
	{
		var height = Container.Frame.Height + (2 * defaultSpacing);
		var anchorTop = anchorView.ConvertRectToView(anchorView.Bounds, ParentView).Top;

		return anchorTop - height >= ParentView.SafeAreaLayoutGuide.LayoutFrame.Top;
	}

	void SetParentConstraints()
	{
		if (shouldFillAndExpandHorizontally)
		{
			this.SafeLeadingAnchor().ConstraintEqualTo(ParentView.SafeLeadingAnchor(), defaultSpacing).Active = true;
			this.SafeTrailingAnchor().ConstraintEqualTo(ParentView.SafeTrailingAnchor(), -defaultSpacing).Active = true;
		}
		else
		{
			this.SafeLeadingAnchor().ConstraintGreaterThanOrEqualTo(ParentView.SafeLeadingAnchor(), defaultSpacing).Active = true;
			this.SafeTrailingAnchor().ConstraintLessThanOrEqualTo(ParentView.SafeTrailingAnchor(), -defaultSpacing).Active = true;
		}

		this.SafeCenterXAnchor().ConstraintEqualTo(ParentView.SafeCenterXAnchor()).Active = true;

		Container.SafeLeadingAnchor().ConstraintEqualTo(this.SafeLeadingAnchor(), defaultSpacing).Active = true;
		Container.SafeTrailingAnchor().ConstraintEqualTo(this.SafeTrailingAnchor(), -defaultSpacing).Active = true;
		Container.SafeBottomAnchor().ConstraintEqualTo(this.SafeBottomAnchor(), -defaultSpacing).Active = true;
		Container.SafeTopAnchor().ConstraintEqualTo(this.SafeTopAnchor(), defaultSpacing).Active = true;
	}

	void Initialize()
	{
		TranslatesAutoresizingMaskIntoConstraints = false;
		AddSubview(Container);

		var subView = new RoundedView(
			VisualOptions.CornerRadius.X,
			VisualOptions.CornerRadius.Y,
			VisualOptions.CornerRadius.Width,
			VisualOptions.CornerRadius.Height)
		{
			BackgroundColor = VisualOptions.BackgroundColor,
			AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth
		};
		Subviews[0].InsertSubview(subView, atIndex: 0);
	}
}