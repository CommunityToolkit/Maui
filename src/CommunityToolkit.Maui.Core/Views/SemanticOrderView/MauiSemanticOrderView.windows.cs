using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The native implementation of the <see href="SemanticOrderView"/> control.
/// </summary>
public partial class MauiSemanticOrderView : ContentPanel
{
	// The next update gives a view that is no longer in the order its default tab index back, unless its tab index was changed since
	readonly List<(FrameworkElement View, int TabIndex)> orderedViews = [];

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

	internal void UpdateViewOrder()
	{
		if (VirtualView is null)
		{
			return;
		}

		var viewOrder = VirtualView.ViewOrder.Select(static view => view.Handler?.PlatformView).OfType<FrameworkElement>().ToList();

		foreach (var (view, tabIndex) in orderedViews)
		{
			if (view.TabIndex == tabIndex && !viewOrder.Contains(view))
			{
				view.ClearValue(UIElement.TabIndexProperty);
			}
		}

		orderedViews.Clear();
		foreach (var platformView in viewOrder)
		{
			var tabIndex = orderedViews.Count + 1;
			platformView.TabIndex = tabIndex;
			orderedViews.Add((platformView, tabIndex));
		}
	}

	// A view of the order may get its platform view after the order was applied (for example when it is added to the layout), and WinUI lays
	// out only the part of the tree whose size changed: a view added below a view whose size does not change (e.g. a Border with a fixed size)
	// gets its platform view without the container being arranged again. So while the container is in the visual tree, the order is applied
	// again after every layout pass.
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