using System.Runtime.Versioning;
using CommunityToolkit.Maui.Core;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.Views;

// VoiceOver calls accessibilityActivate on the element it focuses, and the header's own platform view (a UILabel for a Label) cannot be made
// to toggle the Expander, so VoiceOver reaches the header through an element of the Expander's layout that stands for the whole header.
sealed class ExpanderLayoutView : LayoutView, IUIAccessibilityContainer
{
	readonly HeaderAccessibilityElement headerAccessibilityElement;

	public ExpanderLayoutView()
	{
		headerAccessibilityElement = new HeaderAccessibilityElement(this);
	}

	Expander? ParentExpander => CrossPlatformLayout is Expander.ExpanderLayout { Parent: Expander expander } ? expander : null;

	static UIView? GetPlatformView(IView view) =>
		view.Handler is IPlatformViewHandler { ContainerView: UIView containerView } ? containerView : view.Handler?.PlatformView as UIView;

	// VoiceOver cannot reach a control inside the header element, so a header that holds one keeps UIKit's default elements.
	static bool HoldsAControl(UIView view) => !view.Hidden && (view is UIControl or UITextView || view.Subviews.Any(HoldsAControl));

	// UIKit asks for the elements whenever it needs them, so they follow the header, the content and the Direction without being updated.
	// A non-nil array is the complete list of the elements of the layout, and nil leaves UIKit's default: its subviews.
	[Export("accessibilityElements")]
	NSArray? GetAccessibilityElementsWithTheHeaderAsOneElement()
	{
		if (ParentExpander is not Expander expander
			|| expander.Header is not View { IsVisible: true } header
			|| GetPlatformView(header) is not UIView headerView
			|| HoldsAControl(headerView))
		{
			return null;
		}

		List<NSObject> elements = [headerAccessibilityElement];
		if (expander.Content is IView content && GetPlatformView(content) is UIView { Hidden: false } contentView)
		{
			elements.Insert(expander.Direction is ExpandDirection.Up ? 0 : 1, contentView);
		}

		return NSArray.FromNSObjects([.. elements]);
	}

	// UIKit holds the container of an element weakly, so the element reaches the Expander through its container and keeps nothing alive.
	sealed class HeaderAccessibilityElement(ExpanderLayoutView container) : UIAccessibilityElement(container)
	{
		public override bool IsAccessibilityElement => true;

		public override string? AccessibilityLabel => ParentExpander?.Header is IView header
			? (HeaderPlatformView is UIView headerView ? GetLabel(headerView) : null) ?? GetLabel(header)
			: null;

		public override string? AccessibilityHint => HeaderPlatformView?.AccessibilityHint;

		public override string? AccessibilityIdentifier => HeaderPlatformView?.AccessibilityIdentifier;

		public override ulong AccessibilityTraits
		{
			get
			{
				var traits = UIAccessibilityTrait.Button;

				if (HeaderPlatformView is UIView headerView && headerView.AccessibilityTraits.HasFlag(UIAccessibilityTrait.Header))
				{
					traits |= UIAccessibilityTrait.Header;
				}

				if (ParentExpander?.IsHeaderActivatable is not true)
				{
					traits |= UIAccessibilityTrait.NotEnabled;
				}

				return (ulong)traits;
			}
		}

		public override CGRect AccessibilityFrame => HeaderPlatformView is UIView headerView
			? UIAccessibility.ConvertFrameToScreenCoordinates(headerView.Bounds, headerView)
			: base.AccessibilityFrame;

		[Export("accessibilityExpandedStatus")]
		[SupportedOSPlatform("ios18.0")]
		[SupportedOSPlatform("maccatalyst18.0")]
		public UIAccessibilityExpandedStatus AccessibilityExpandedStatus => ParentExpander?.IsExpanded is true
			? UIAccessibilityExpandedStatus.Expanded
			: UIAccessibilityExpandedStatus.Collapsed;

		Expander? ParentExpander => (AccessibilityContainer as ExpanderLayoutView)?.ParentExpander;

		UIView? HeaderPlatformView => ParentExpander?.Header?.Handler?.PlatformView as UIView;

		// With another TapGestureRecognizer on the header, returning false makes VoiceOver tap the header instead, so that every
		// TapGestureRecognizer of the header runs.
		[Export("accessibilityActivate")]
		public bool AccessibilityActivate()
		{
			if (ParentExpander is not Expander { IsHeaderActivatable: true } expander
				|| expander.Header is not View header
				|| header.GestureRecognizers.OfType<TapGestureRecognizer>().Any(recognizer => recognizer != expander.HeaderTapGestureRecognizer))
			{
				return false;
			}

			expander.HandleHeaderTap(new TappedEventArgs(null));

			// VoiceOver lists the content that appeared or disappeared, and reads the new state of the header.
			UIAccessibility.PostNotification(UIAccessibilityPostNotification.LayoutChanged, this);
			return true;
		}

		// Like a table view cell, the element reads the labels of the accessibility elements of the header.
		static string? GetLabel(UIView view)
		{
			if (view.Hidden || view.AccessibilityElementsHidden)
			{
				return null;
			}

			return view.IsAccessibilityElement ? view.AccessibilityLabel : JoinLabels(view.Subviews.Select(GetLabel));
		}

		// UIKit gives a UILabel its default accessibility label only while an assistive technology runs, so without a label from UIKit the
		// element reads the semantic descriptions and the texts of the header, as MAUI reads the label of a layout from its children.
		static string? GetLabel(IView view)
		{
			if (view.Visibility is not Visibility.Visible)
			{
				return null;
			}

			if (view.Semantics?.Description is string description && !string.IsNullOrWhiteSpace(description))
			{
				return description;
			}

			if (view is IText text && view is not ITextInput && !string.IsNullOrWhiteSpace(text.Text))
			{
				return text.Text;
			}

			return view is Microsoft.Maui.ILayout layout ? JoinLabels(layout.Select(GetLabel)) : null;
		}

		static string? JoinLabels(IEnumerable<string?> labels)
		{
			var label = string.Join(", ", labels.Where(static candidate => !string.IsNullOrWhiteSpace(candidate)));
			return label.Length > 0 ? label : null;
		}
	}
}
