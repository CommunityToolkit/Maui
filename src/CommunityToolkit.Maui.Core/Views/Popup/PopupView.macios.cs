using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CoreAnimation;
using CoreGraphics;
using UIKit;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// Visual Options for <see cref="PopupView"/>
/// </summary>
public class PopupViewVisualOptions
{
	/// <summary>
	/// <see cref="PopupView"/> Border Corner Radius
	/// </summary>
	public CGRect CornerRadius { get; set; }

	/// <summary>
	/// <see cref="PopupView"/> Background Color
	/// </summary>
	public UIColor BackgroundColor { get; set; } = UIColor.Gray;
}

/// <summary>
/// <see cref="UIView"/> for <see cref="Popup"/>
/// </summary>
public class PopupView : UIView
{
	readonly List<UIView> _children = Array.Empty<UIView>().ToList();

	/// <summary>
	/// Parent UIView
	/// </summary>
	public UIView ParentView => UIApplication.SharedApplication.Windows.First(x => x.IsKeyWindow);

	/// <summary>
	/// PopupView Children
	/// </summary>
	public IReadOnlyList<UIView> Children => _children;

	/// <summary>
	/// <see cref="UIView"/> on which Popup will appear. When null, <see cref="PopupView"/> will appear at bottom of screen.
	/// </summary>
	public UIView? AnchorView { get; set; }

	/// <summary>
	/// <see cref="PopupViewVisualOptions"/>
	/// </summary>
	public PopupViewVisualOptions VisualOptions { get; } = new();

	/// <summary>
	/// Container of <see cref="PopupView"/>
	/// </summary>
	protected UIStackView? Container { get; set; }

	/// <summary>
	/// Dissmisses the Popup from the screen
	/// </summary>
	public void Dismiss() => RemoveFromSuperview();

	/// <summary>
	/// Adds a <see cref="UIView"/> to <see cref="Children"/>
	/// </summary>
	/// <param name="child"></param>
	public void AddChild(UIView child) => _children.Add(child);

	/// <summary>
	/// Initializes <see cref="PopupView"/>
	/// </summary>
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

static class UIViewExtensions
{
	/// <summary>
	/// Safe bottom edge of the guide
	/// </summary>
	public static NSLayoutYAxisAnchor SafeBottomAnchor(this UIView view) =>
		UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
			? view.SafeAreaLayoutGuide.BottomAnchor
			: view.BottomAnchor;

	/// <summary>
	/// Safe horizontal center of the guide
	/// </summary>
	public static NSLayoutXAxisAnchor SafeCenterXAnchor(this UIView view) =>
		UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
			? view.SafeAreaLayoutGuide.CenterXAnchor
			: view.CenterXAnchor;

	/// <summary>
	/// Safe vertical center of the guide
	/// </summary>
	public static NSLayoutYAxisAnchor SafeCenterYAnchor(this UIView view) =>
		UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
			? view.SafeAreaLayoutGuide.CenterYAnchor
			: view.CenterYAnchor;

	/// <summary>
	/// Safe vertical extent of the guide
	/// </summary>
	public static NSLayoutDimension SafeHeightAnchor(this UIView view) =>
		UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
			? view.SafeAreaLayoutGuide.HeightAnchor
			: view.HeightAnchor;

	/// <summary>
	/// Safe leading edge of the guide
	/// </summary>
	public static NSLayoutXAxisAnchor SafeLeadingAnchor(this UIView view) =>
		UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
			? view.SafeAreaLayoutGuide.LeadingAnchor
			: view.LeadingAnchor;

	/// <summary>
	/// Safe left edge of the guide
	/// </summary>
	public static NSLayoutXAxisAnchor SafeLeftAnchor(this UIView view) =>
		UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
			? view.SafeAreaLayoutGuide.LeftAnchor
			: view.LeftAnchor;

	/// <summary>
	/// Safe right edge of the guide
	/// </summary>
	public static NSLayoutXAxisAnchor SafeRightAnchor(this UIView view) =>
		UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
			? view.SafeAreaLayoutGuide.RightAnchor
			: view.RightAnchor;

	/// <summary>
	/// Safe top edge of the guide
	/// </summary>
	public static NSLayoutYAxisAnchor SafeTopAnchor(this UIView view) =>
		UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
			? view.SafeAreaLayoutGuide.TopAnchor
			: view.TopAnchor;

	/// <summary>
	/// Safe trailing edge of the guide
	/// </summary>
	public static NSLayoutXAxisAnchor SafeTrailingAnchor(this UIView view) =>
		UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
			? view.SafeAreaLayoutGuide.TrailingAnchor
			: view.TrailingAnchor;

	/// <summary>
	/// Safe width edge of the guide
	/// </summary>
	public static NSLayoutDimension SafeWidthAnchor(this UIView view) =>
		UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
			? view.SafeAreaLayoutGuide.WidthAnchor
			: view.WidthAnchor;
}