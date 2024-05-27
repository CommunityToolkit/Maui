using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Core.Extensions;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// <see cref="UIView"/> for <see cref="Alert"/>
/// </summary>
public class AlertView : UIView
{
	readonly List<UIView> children = Enumerable.Empty<UIView>().ToList();

	/// <summary>
	/// Parent UIView
	/// </summary>
	public static UIView ParentView => Microsoft.Maui.Platform.UIApplicationExtensions.GetKeyWindow(UIApplication.SharedApplication) ?? throw new InvalidOperationException("KeyWindow is not found");

	/// <summary>
	/// PopupView Children
	/// </summary>
	public IReadOnlyList<UIView> Children => children;

	/// <summary>
	/// <see cref="UIView"/> on which Alert will appear. When null, <see cref="AlertView"/> will appear at bottom of screen.
	/// </summary>
	public UIView? AnchorView { get; set; }

	/// <summary>
	/// <see cref="AlertViewVisualOptions"/>
	/// </summary>
	public AlertViewVisualOptions VisualOptions { get; } = new();

	/// <summary>
	/// Container of <see cref="AlertView"/>
	/// </summary>
	protected UIStackView? Container { get; set; }

	/// <summary>
	/// Dismisses the Popup from the screen
	/// </summary>
	public void Dismiss() => RemoveFromSuperview();

	/// <summary>
	/// Adds a <see cref="UIView"/> to <see cref="Children"/>
	/// </summary>
	/// <param name="child"></param>
	public void AddChild(UIView child) => children.Add(child);

	/// <summary>
	/// Initializes <see cref="AlertView"/>
	/// </summary>
	public void Setup()
	{
		Initialize();
		ConstraintInParent();
	}

	void ConstraintInParent()
	{
		_ = Container ?? throw new InvalidOperationException($"{nameof(AlertView)}.{nameof(Initialize)} not called");

		const int defaultSpacing = 10;
		if (AnchorView is null)
		{
			this.SafeBottomAnchor().ConstraintEqualTo(ParentView.SafeBottomAnchor(), -defaultSpacing).Active = true;
			this.SafeTopAnchor().ConstraintGreaterThanOrEqualTo(ParentView.SafeTopAnchor(), defaultSpacing).Active = true;
		}
		else
		{
			this.SafeBottomAnchor().ConstraintEqualTo(AnchorView.SafeTopAnchor(), -defaultSpacing).Active = true;
		}

		this.SafeLeadingAnchor().ConstraintGreaterThanOrEqualTo(ParentView.SafeLeadingAnchor(), defaultSpacing).Active = true;
		this.SafeTrailingAnchor().ConstraintLessThanOrEqualTo(ParentView.SafeTrailingAnchor(), -defaultSpacing).Active = true;
		this.SafeCenterXAnchor().ConstraintEqualTo(ParentView.SafeCenterXAnchor()).Active = true;

		Container.SafeLeadingAnchor().ConstraintEqualTo(this.SafeLeadingAnchor(), defaultSpacing).Active = true;
		Container.SafeTrailingAnchor().ConstraintEqualTo(this.SafeTrailingAnchor(), -defaultSpacing).Active = true;
		Container.SafeBottomAnchor().ConstraintEqualTo(this.SafeBottomAnchor(), -defaultSpacing).Active = true;
		Container.SafeTopAnchor().ConstraintEqualTo(this.SafeTopAnchor(), defaultSpacing).Active = true;
	}

	[MemberNotNull(nameof(Container))]
	void Initialize()
	{
		Container = new UIStackView
		{
			Alignment = UIStackViewAlignment.Fill,
			Distribution = UIStackViewDistribution.EqualSpacing,
			Axis = UILayoutConstraintAxis.Horizontal,
			TranslatesAutoresizingMaskIntoConstraints = false
		};

		foreach (var view in Children)
		{
			Container.AddArrangedSubview(view);
		}

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