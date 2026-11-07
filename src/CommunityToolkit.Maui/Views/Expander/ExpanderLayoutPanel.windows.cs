using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Input;
using Windows.System;
// Microsoft.Maui.Controls.AutomationProperties comes in through the implicit global using, so the unqualified name is ambiguous (CS0104)
using WAutomationProperties = Microsoft.UI.Xaml.Automation.AutomationProperties;
using WPoint = Windows.Foundation.Point;
using WSize = Windows.Foundation.Size;
using WThickness = Microsoft.UI.Xaml.Thickness;
using WVisualTreeHelper = Microsoft.UI.Xaml.Media.VisualTreeHelper;

namespace CommunityToolkit.Maui.Views;

// The header's own platform view (a TextBlock for a Label) can neither take the focus from Tab nor offer ExpandCollapse and Invoke, so keyboard
// and UI Automation users reach the header through the Expander's layout, as they reach the header of WinUI's Expander through the automation
// peer of the whole Expander.
// CsWinRT requires partial on Windows (CsWinRT1028).
sealed partial class ExpanderLayoutPanel : LayoutPanel
{
	bool isEnterOrSpacePressed;

	public ExpanderLayoutPanel()
	{
		UseSystemFocusVisuals = true;
		KeyDown += OnKeyDown;
		KeyUp += OnKeyUp;
		LostFocus += OnLostFocus;
	}

	Expander? ParentExpander => CrossPlatformLayout is Expander.ExpanderLayout { Parent: Expander expander } ? expander : null;

	internal void UpdateHeaderAccessibility()
	{
		IsTabStop = ParentExpander?.IsHeaderActivatable is true;
		(FrameworkElementAutomationPeer.FromElement(this) as ExpanderLayoutAutomationPeer)?.RaiseExpandCollapseStateChanged();
	}

	// Like a Button, Enter and Space toggle the Expander when they are released, and another key or the focus leaving the header drops the press.
	// Unlike a Button, a key already held when the header got the focus (a repeat) does not press it, so holding Enter on a control that moves
	// the focus to the header does not also toggle the header.
	internal bool HandleKey(VirtualKey key, bool isKeyDown, bool isRepeat)
	{
		if (key is not (VirtualKey.Enter or VirtualKey.Space))
		{
			if (isKeyDown)
			{
				isEnterOrSpacePressed = false;
			}

			return false;
		}

		if (isKeyDown)
		{
			isEnterOrSpacePressed |= !isRepeat;
			return true;
		}

		if (!isEnterOrSpacePressed)
		{
			return false;
		}

		isEnterOrSpacePressed = false;
		ActivateHeader();
		return true;
	}

	protected override AutomationPeer OnCreateAutomationPeer() => new ExpanderLayoutAutomationPeer(this);

	protected override WSize ArrangeOverride(WSize finalSize)
	{
		var arrangedSize = base.ArrangeOverride(finalSize);
		FocusVisualMargin = GetFocusVisualMarginAroundHeader(arrangedSize.Height);
		return arrangedSize;
	}

	// The panel also holds the content, so the focus visual is moved in to frame only the row of the header.
	WThickness GetFocusVisualMarginAroundHeader(double height)
	{
		if (ParentExpander?.Header is not IView header)
		{
			return default;
		}

		var headerRowTop = header.Frame.Top - header.Margin.Top;
		var headerRowBottom = header.Frame.Bottom + header.Margin.Bottom;
		return new WThickness(0, Math.Max(0, headerRowTop), 0, Math.Max(0, height - headerRowBottom));
	}

	void ActivateHeader()
	{
		if (ParentExpander is Expander { IsHeaderActivatable: true } expander)
		{
			expander.HandleHeaderTap(HeaderCenterTappedEventArgs.Create(expander));
		}
	}

	// The keys of a focused element in the header or in the content reach the panel too, and belong to that element.
	void OnKeyDown(object sender, KeyRoutedEventArgs e)
	{
		if (ReferenceEquals(e.OriginalSource, this) && HandleKey(e.Key, isKeyDown: true, e.KeyStatus.WasKeyDown))
		{
			e.Handled = true;
		}
	}

	void OnKeyUp(object sender, KeyRoutedEventArgs e)
	{
		if (ReferenceEquals(e.OriginalSource, this) && HandleKey(e.Key, isKeyDown: false, isRepeat: false))
		{
			e.Handled = true;
		}
	}

	// Like a Button, a press is dropped when the focus leaves the header, so a later key-up on it does not toggle it.
	void OnLostFocus(object sender, RoutedEventArgs e)
	{
		if (ReferenceEquals(e.OriginalSource, this))
		{
			isEnterOrSpacePressed = false;
		}
	}

	// CsWinRT requires partial on Windows (CsWinRT1028).
	sealed partial class ExpanderLayoutAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider, IInvokeProvider
	{
		ExpandCollapseState raisedExpandCollapseState;

		public ExpanderLayoutAutomationPeer(ExpanderLayoutPanel owner) : base(owner)
		{
			raisedExpandCollapseState = ExpandCollapseState;
		}

		public ExpandCollapseState ExpandCollapseState =>
			Panel.ParentExpander?.IsExpanded is true ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

		ExpanderLayoutPanel Panel => (ExpanderLayoutPanel)Owner;

		UIElement? HeaderPlatformView => Panel.ParentExpander?.Header?.Handler?.PlatformView as UIElement;

		public void Collapse() => SetIsExpanded(false);

		public void Expand() => SetIsExpanded(true);

		public void Invoke()
		{
			var expander = GetActivatableExpander();
			expander.HandleHeaderTap(HeaderCenterTappedEventArgs.Create(expander));
			RaiseAutomationEvent(AutomationEvents.InvokePatternOnInvoked);
		}

		internal void RaiseExpandCollapseStateChanged()
		{
			var expandCollapseState = ExpandCollapseState;
			if (expandCollapseState == raisedExpandCollapseState)
			{
				return;
			}

			var previousExpandCollapseState = raisedExpandCollapseState;
			raisedExpandCollapseState = expandCollapseState;

			if (ListenerExists(AutomationEvents.PropertyChanged))
			{
				RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, previousExpandCollapseState, expandCollapseState);
			}
		}

		protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

		protected override string GetClassNameCore() => nameof(Expander);

		// A screen reader reads the help text of the focused element, which is the Expander and not its header.
		protected override string GetHelpTextCore()
		{
			var helpText = base.GetHelpTextCore();
			if (string.IsNullOrEmpty(helpText) && HeaderPlatformView is UIElement header)
			{
				return WAutomationProperties.GetHelpText(header);
			}

			return helpText;
		}

		protected override string GetNameCore()
		{
			var name = base.GetNameCore();
			if (string.IsNullOrEmpty(name) && HeaderPlatformView is UIElement header)
			{
				return GetNameOf(header);
			}

			return name;
		}

		protected override object GetPatternCore(PatternInterface patternInterface) => patternInterface switch
		{
			PatternInterface.ExpandCollapse or PatternInterface.Invoke => this,
			_ => base.GetPatternCore(patternInterface)
		};

		protected override bool IsEnabledCore() => Panel.ParentExpander?.IsHeaderActivatable is true;

		// The header is often a layout, whose own automation peer has no name: its name is then read from what the layout holds.
		static string GetNameOf(UIElement element) => FrameworkElementAutomationPeer.CreatePeerForElement(element) is AutomationPeer peer
			? GetNameOf(peer)
			: JoinNames(Enumerable.Range(0, WVisualTreeHelper.GetChildrenCount(element))
				.Select(index => WVisualTreeHelper.GetChild(element, index))
				.OfType<UIElement>()
				.Select(GetNameOf));

		static string GetNameOf(AutomationPeer peer)
		{
			var name = peer.GetName();
			return string.IsNullOrWhiteSpace(name) ? JoinNames((peer.GetChildren() ?? []).Select(GetNameOf)) : name;
		}

		static string JoinNames(IEnumerable<string> names) => string.Join(", ", names.Where(static name => !string.IsNullOrWhiteSpace(name)));

		void SetIsExpanded(bool isExpanded)
		{
			var expander = GetActivatableExpander();
			expander.SetIsExpandedFromHeader(isExpanded, HeaderCenterTappedEventArgs.Create(expander));
		}

		Expander GetActivatableExpander() => Panel.ParentExpander is Expander { IsHeaderActivatable: true } expander
			? expander
			: throw new ElementNotEnabledException();
	}

	// An Expander in a CollectionView resizes the item container that holds the position of the tap (ForceUpdateCellSize), so an activation
	// without a pointer reports the center of the header as its position.
	sealed class HeaderCenterTappedEventArgs(FrameworkElement header) : TappedEventArgs(null)
	{
		public static TappedEventArgs Create(Expander expander) => expander.Header?.Handler?.PlatformView is FrameworkElement headerView
			? new HeaderCenterTappedEventArgs(headerView)
			: new TappedEventArgs(null);

		public override Point? GetPosition(Element? relativeTo)
		{
			UIElement? target = null;
			if (relativeTo is not null)
			{
				if (relativeTo.Handler?.PlatformView is not UIElement relativeToPlatformView)
				{
					return null;
				}

				target = relativeToPlatformView;
			}

			// TransformToVisual throws for an element of another XAML tree.
			if (header.XamlRoot is null || (target is not null && target.XamlRoot != header.XamlRoot))
			{
				return null;
			}

			var center = header.TransformToVisual(target).TransformPoint(new WPoint(header.ActualWidth / 2, header.ActualHeight / 2));
			return new Point(center.X, center.Y);
		}
	}
}
