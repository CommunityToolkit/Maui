using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Core.Extensions;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// <see cref="UIView"/> for <see cref="Alert"/>
/// </summary>
public class AlertView : UIView
{
	const int defaultSpacing = 10;
	readonly List<UIView> children = [];

	/// <summary>
	/// Parent UIView
	/// </summary>
	public static UIView ParentView => Microsoft.Maui.Platform.UIApplicationExtensions.GetKeyWindow(UIApplication.SharedApplication) ?? throw new InvalidOperationException("KeyWindow is not found");

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
	/// <param name="shouldFillAndExpandHorizontally">Should stretch container horizontally to fit the screen</param>
	public void Setup(bool shouldFillAndExpandHorizontally = false)
	{
		Initialize();
		ConstraintInParent(shouldFillAndExpandHorizontally);
	}

	/// <inheritdoc />
	public override void LayoutSubviews()
	{
		base.LayoutSubviews();
		
		if (Container is null)
		{
			throw new InvalidOperationException($"{nameof(AlertView)}.{nameof(Initialize)} must be called before {nameof(LayoutSubviews)}");
		}

		if (AnchorView is null)
		{
			this.SafeBottomAnchor().ConstraintEqualTo(ParentView.SafeBottomAnchor(), -defaultSpacing).Active = true;
			this.SafeTopAnchor().ConstraintGreaterThanOrEqualTo(ParentView.SafeTopAnchor(), defaultSpacing).Active = true;
		}
		else if (AnchorView.Superview is not null)
		{
			var anchorViewPosition = AnchorView.Superview.ConvertRectToView(AnchorView.Frame, null);
			if (anchorViewPosition.Top < Container.Frame.Height + SafeAreaLayoutGuide.LayoutFrame.Bottom)
			{
				this.SafeTopAnchor().ConstraintEqualTo(AnchorView.SafeBottomAnchor(), defaultSpacing).Active = true;
			}
			else
			{
				this.SafeBottomAnchor().ConstraintEqualTo(AnchorView.SafeTopAnchor(), -defaultSpacing).Active = true;
			}
		}
	}

	void ConstraintInParent(bool shouldFillAndExpandHorizontally)
	{
		if (Container is null)
		{
			throw new InvalidOperationException($"{nameof(AlertView)}.{nameof(Initialize)} must be called before {nameof(LayoutSubviews)}");
		}

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