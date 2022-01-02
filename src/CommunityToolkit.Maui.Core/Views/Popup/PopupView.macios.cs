﻿using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Core.Primitives;
using UIKit;

namespace CommunityToolkit.Maui.Core.Views;

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
}