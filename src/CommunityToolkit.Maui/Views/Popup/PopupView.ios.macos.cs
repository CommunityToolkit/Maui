using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Maui.Extensions.Internals;
using CoreGraphics;
using UIKit;

namespace CommunityToolkit.Maui.Views.Popup;

class PopupView : UIView
{
	public PopupView()
	{
		Children = new List<UIView>();
	}

	public IList<UIView> Children { get; }

	public UIView? AnchorView { get; set; }

	public UIView ParentView => UIApplication.SharedApplication.Windows.First(x => x.IsKeyWindow);

	protected NativeRoundedStackView? Container { get; set; }

	public void Dismiss() => RemoveFromSuperview();

	public void Setup(CGRect cornerRadius, UIColor backgroundColor)
	{
		Initialize(cornerRadius, backgroundColor);
		ConstraintInParent();
	}

	void ConstraintInParent()
	{
		_ = ParentView ?? throw new InvalidOperationException($"{nameof(PopupView)}.{nameof(Initialize)} not called");
		_ = Container ?? throw new InvalidOperationException($"{nameof(PopupView)}.{nameof(Initialize)} not called");

		const int defaultSpacing = 10;
		if (AnchorView is null)
		{
			this.SafeBottomAnchor().ConstraintEqualTo(ParentView.SafeBottomAnchor(), -defaultSpacing).Active = true;
			this.SafeTopAnchor().ConstraintGreaterThanOrEqualTo(ParentView.SafeTopAnchor(), defaultSpacing).Active = true;
		}
		else
		{
			this.SafeBottomAnchor().ConstraintEqualTo(AnchorView.SafeBottomAnchor(), -defaultSpacing).Active = true;
		}

		this.SafeLeadingAnchor().ConstraintGreaterThanOrEqualTo(ParentView.SafeLeadingAnchor(), defaultSpacing).Active = true;
		this.SafeTrailingAnchor().ConstraintLessThanOrEqualTo(ParentView.SafeTrailingAnchor(), -defaultSpacing).Active = true;
		this.SafeCenterXAnchor().ConstraintEqualTo(ParentView.SafeCenterXAnchor()).Active = true;

		Container.SafeLeadingAnchor().ConstraintEqualTo(this.SafeLeadingAnchor(), defaultSpacing).Active = true;
		Container.SafeTrailingAnchor().ConstraintEqualTo(this.SafeTrailingAnchor(), -defaultSpacing).Active = true;
		Container.SafeBottomAnchor().ConstraintEqualTo(this.SafeBottomAnchor(), -defaultSpacing).Active = true;
		Container.SafeTopAnchor().ConstraintEqualTo(this.SafeTopAnchor(), defaultSpacing).Active = true;
	}

	void Initialize(CGRect cornerRadius, UIColor backgroundColor)
	{
		Container = new NativeRoundedStackView(cornerRadius.X, cornerRadius.Y, cornerRadius.Width, cornerRadius.Height);

		AddSubview(Container);

		Container.Axis = UILayoutConstraintAxis.Horizontal;
		Container.TranslatesAutoresizingMaskIntoConstraints = false;
		Container.BackgroundColor = backgroundColor;

		TranslatesAutoresizingMaskIntoConstraints = false;

		foreach (var view in Children)
		{
			Container.AddArrangedSubview(view);
		}
	}

	public void AddChild(UIView child)
	{
		Children.Add(child);
	}
}