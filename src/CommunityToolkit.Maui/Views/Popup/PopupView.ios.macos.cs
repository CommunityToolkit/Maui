using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CommunityToolkit.Maui.Extensions.Internals;
using CoreAnimation;
using CoreGraphics;
using CoreGraphics;
using UIKit;
using UIKit;

namespace CommunityToolkit.Maui.Views.Popup;

class PopupViewVisualOptions
{
	public CGRect CornerRadius { get; set; }

	public UIColor BackgroundColor { get; set; } = UIColor.Gray;
}

class PopupView : UIView
{
	readonly List<UIView> _children = Array.Empty<UIView>().ToList();

	public UIView ParentView => UIApplication.SharedApplication.Windows.First(x => x.IsKeyWindow);

	public IReadOnlyList<UIView> Children => _children;

	public UIView? AnchorView { get; set; }

	public PopupViewVisualOptions VisualOptions { get; } = new();

	protected UIStackView? Container { get; set; }

	public void Dismiss() => RemoveFromSuperview();

	public void AddChild(UIView child) => _children.Add(child);

	public void Setup()
	{
		Initialize();
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

	[MemberNotNull(nameof(Container))]
	void Initialize()
	{
		Container = new RoundedStackView(
			VisualOptions.CornerRadius.X,
			VisualOptions.CornerRadius.Y,
			VisualOptions.CornerRadius.Width,
			VisualOptions.CornerRadius.Height);

		AddSubview(Container);

		Container.Axis = UILayoutConstraintAxis.Horizontal;
		Container.TranslatesAutoresizingMaskIntoConstraints = false;
		Container.BackgroundColor = VisualOptions.BackgroundColor;

		TranslatesAutoresizingMaskIntoConstraints = false;

		foreach (var view in Children)
		{
			Container.AddArrangedSubview(view);
		}
	}

	class RoundedStackView : UIStackView
	{
		public nfloat Left { get; }

		public nfloat Top { get; }

		public nfloat Right { get; }

		public nfloat Bottom { get; }

		public RoundedStackView(nfloat left, nfloat top, nfloat right, nfloat bottom)
		{
			Left = left;
			Top = top;
			Right = right;
			Bottom = bottom;
		}

		public override void Draw(CGRect rect)
		{
			ClipsToBounds = true;
			var path = GetRoundedPath(rect, Left, Top, Right, Bottom);
			var maskLayer = new CAShapeLayer { Frame = rect, Path = path };
			Layer.Mask = maskLayer;
			Layer.MasksToBounds = true;
		}

		static CGPath? GetRoundedPath(CGRect rect, nfloat left, nfloat top, nfloat right, nfloat bottom)
		{
			var path = new UIBezierPath();
			path.MoveTo(new CGPoint(rect.Width - right, rect.Y));

			path.AddArc(new CGPoint((float)rect.X + rect.Width - right, (float)rect.Y + right), (nfloat)right, (float)(Math.PI * 1.5), (float)Math.PI * 2, true);
			path.AddLineTo(new CGPoint(rect.Width, rect.Height - bottom));

			path.AddArc(new CGPoint((float)rect.X + rect.Width - bottom, (float)rect.Y + rect.Height - bottom), (nfloat)bottom, 0, (float)(Math.PI * .5), true);
			path.AddLineTo(new CGPoint(left, rect.Height));

			path.AddArc(new CGPoint((float)rect.X + left, (float)rect.Y + rect.Height - left), (nfloat)left, (float)(Math.PI * .5), (float)Math.PI, true);
			path.AddLineTo(new CGPoint(rect.X, top));

			path.AddArc(new CGPoint((float)rect.X + top, (float)rect.Y + top), (nfloat)top, (float)Math.PI, (float)(Math.PI * 1.5), true);

			path.ClosePath();

			return path.CGPath;
		}
	}
}