using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The native implementation of the <see href="SemanticOrderView"/> control.
/// </summary>
/// <remarks>
/// The views of <see cref="ISemanticOrderView.ViewOrder"/> that the container holds come first in its Tab order, in that order, and the rest of its
/// content follows them in the order of the visual tree. A view of the order that a control within the container holds (a ScrollView, for example),
/// or that is outside the container, gets a <see cref="UIElement.TabIndex"/> instead, which orders it among the Tab stops of the nearest control
/// that holds it.
/// </remarks>
public partial class MauiSemanticOrderView : ContentPanel
{
	// The next update gives a view that no longer gets a tab index from the order its default tab index back, unless its tab index was changed since
	readonly List<(FrameworkElement View, int TabIndex)> viewsWithATabIndex = [];

	ISemanticOrderView? virtualView;

	/// <summary>
	/// Initialize <see cref="MauiSemanticOrderView"/>
	/// </summary>
	public MauiSemanticOrderView()
	{
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	internal ISemanticOrderView? VirtualView
	{
		get => virtualView;
		set
		{
			virtualView = value;
			UpdateViewOrder();
		}
	}

	// WinUI compares a tab index with every Tab stop of the nearest control that holds the view, and the container is a panel, not a control: a tab
	// index would also put a view that the container holds before the Tab stops around the container. GetChildrenInTabFocusOrder orders those views.
	internal void UpdateViewOrder()
	{
		if (VirtualView is null)
		{
			return;
		}

		var viewsToIndex = GetPlatformViewsInViewOrder().Where(view => GetElementsBetween(view) is null).ToList();

		foreach (var (view, tabIndex) in viewsWithATabIndex)
		{
			if (view.TabIndex == tabIndex && !viewsToIndex.Contains(view))
			{
				view.ClearValue(UIElement.TabIndexProperty);
			}
		}

		viewsWithATabIndex.Clear();
		foreach (var view in viewsToIndex)
		{
			var tabIndex = viewsWithATabIndex.Count + 1;
			view.TabIndex = tabIndex;
			viewsWithATabIndex.Add((view, tabIndex));
		}
	}

	// Null when the container lists none of the views of the order: WinUI's default order (the visual tree) applies then.
	internal List<DependencyObject>? GetTabFocusChildrenInViewOrder()
	{
		var listableViews = new List<(FrameworkElement View, List<DependencyObject> ElementsBetween)>();
		foreach (var view in GetPlatformViewsInViewOrder())
		{
			// WinUI skips a hidden element together with what it holds, which a view listed on its own would escape
			if (GetElementsBetween(view) is List<DependencyObject> elementsBetween && elementsBetween.All(IsVisible))
			{
				listableViews.Add((view, elementsBetween));
			}
		}

		var viewsOfTheOrder = listableViews.Select(static listableView => (DependencyObject)listableView.View).ToHashSet();
		var orderedViews = new HashSet<DependencyObject>();
		var holdersOfOrderedViews = new HashSet<DependencyObject>();
		var tabFocusChildren = new List<DependencyObject>();
		foreach (var (view, elementsBetween) in listableViews)
		{
			// WinUI reaches a view that another view of the order holds through that view: listed as well, it would be a Tab stop twice
			if (elementsBetween.Any(viewsOfTheOrder.Contains))
			{
				continue;
			}

			orderedViews.Add(view);
			holdersOfOrderedViews.UnionWith(elementsBetween);
			tabFocusChildren.Add(view);
		}

		if (tabFocusChildren.Count is 0)
		{
			return null;
		}

		AddTheRestOfTheContent(this);
		return tabFocusChildren;

		// WinUI looks into every element it is given, so an element that holds views of the order is replaced by what else it holds: listed itself,
		// it would make them Tab stops a second time
		void AddTheRestOfTheContent(DependencyObject element)
		{
			var childrenCount = VisualTreeHelper.GetChildrenCount(element);
			for (var i = 0; i < childrenCount; i++)
			{
				var child = VisualTreeHelper.GetChild(element, i);
				if (orderedViews.Contains(child))
				{
					continue;
				}

				if (holdersOfOrderedViews.Contains(child))
				{
					AddTheRestOfTheContent(child);
				}
				else
				{
					tabFocusChildren.Add(child);
				}
			}
		}
	}

	/// <inheritdoc/>
	protected override IEnumerable<DependencyObject> GetChildrenInTabFocusOrder()
	{
		// The CsWinRT AOT generator only sees List<DependencyObject> in a return statement; NativeAOT needs its generated vtable to hand the list to WinUI
		if (GetTabFocusChildrenInViewOrder() is List<DependencyObject> tabFocusChildren)
		{
			return tabFocusChildren;
		}

		return base.GetChildrenInTabFocusOrder();
	}

	static bool IsVisible(DependencyObject element) =>
		element is not UIElement uiElement || uiElement.Visibility is Microsoft.UI.Xaml.Visibility.Visible;

	IEnumerable<FrameworkElement> GetPlatformViewsInViewOrder()
	{
		// WinUI asks for the tab focus children whenever it moves the focus: an invalid (null) ViewOrder is nothing to order rather than an exception
		if (VirtualView?.ViewOrder is not IEnumerable<IView> viewOrder)
		{
			return [];
		}

		return viewOrder.Select(static view => view.Handler?.PlatformView).OfType<FrameworkElement>().Distinct();
	}

	// Null for a view that the container does not hold, or holds inside a control (or an element with IsTabStop): WinUI looks for the next Tab stop
	// among the children of the nearest such element that holds the focused view, whatever the container lists.
	List<DependencyObject>? GetElementsBetween(FrameworkElement view)
	{
		var elementsBetween = new List<DependencyObject>();
		for (var parent = VisualTreeHelper.GetParent(view); parent is not null; parent = VisualTreeHelper.GetParent(parent))
		{
			if (parent == this)
			{
				return elementsBetween;
			}

			if (parent is Control || (parent is UIElement element && element.IsTabStop))
			{
				return null;
			}

			elementsBetween.Add(parent);
		}

		return null;
	}

	// A view of the order may get its platform view, or another parent, after the tab indexes were given (for example when it is added to the
	// layout), and WinUI lays out only the part of the tree whose size changed: a view added below a view whose size does not change (e.g. a Border
	// with a fixed size) gets its platform view without the container being arranged again. So while the container is in the visual tree, the tab
	// indexes are given again after every layout pass.
	void OnLoaded(object? sender, RoutedEventArgs e)
	{
		// Loaded can be raised again without Unloaded in between: the handler is added once
		LayoutUpdated -= OnLayoutUpdated;
		LayoutUpdated += OnLayoutUpdated;

		UpdateViewOrder();
	}

	// Out of the visual tree there are no layout passes, and the handler must not keep the container alive. A container that moves to another
	// parent within one turn can get Loaded (for the new parent) before Unloaded (for the old one): it is still loaded then and keeps the handler
	void OnUnloaded(object? sender, RoutedEventArgs e)
	{
		if (IsLoaded)
		{
			return;
		}

		LayoutUpdated -= OnLayoutUpdated;
	}

	void OnLayoutUpdated(object? sender, object e) => UpdateViewOrder();
}