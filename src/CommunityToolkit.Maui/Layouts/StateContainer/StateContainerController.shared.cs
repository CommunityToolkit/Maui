namespace CommunityToolkit.Maui.Layouts;

/// <summary>
/// StateContainer Controller
/// </summary>
sealed class StateContainerController
{
	readonly WeakReference<Layout> layoutWeakReference;

	string? previousState;
	List<IView> originalContent = Enumerable.Empty<IView>().ToList();

	/// <summary>
	/// Initialize <see cref="StateContainerController"/> with a <see cref="Layout"/>
	/// </summary>
	/// <param name="layout"></param>
	public StateContainerController(Layout layout) => layoutWeakReference = new WeakReference<Layout>(layout);

	/// <summary>
	/// The StateViews defined in the StateContainer.
	/// </summary>
	public required IList<View> StateViews { get; set; }

	/// <summary>
	/// Display the default content.
	/// </summary>
	public void SwitchToContent()
	{
		var layout = GetLayout();
		previousState = null;
		layout.Children.Clear();

		// Put the original content back in.
		foreach (var item in originalContent)
		{
			layout.Children.Add(item);
		}
	}

	/// <summary>
	/// Display the <see cref="View"/> for the given StateKey.
	/// </summary>
	public void SwitchToState(string state)
	{
		var layout = GetLayout();
		var view = GetViewForState(state);

		// Put the original content somewhere where we can restore it.
		if (previousState is null)
		{
			originalContent = [.. layout.Children];
		}

		previousState = state;
		layout.Children.Clear();

		// If the layout we're applying StateContainer to is a Grid,
		// we want to have the StateContainer span the entire Grid surface.
		// Otherwise it would just end up in row 0 : column 0.
		if (layout is IGridLayout grid)
		{
			if (grid.RowDefinitions.Count > 0)
			{
				Grid.SetRowSpan(view, grid.RowDefinitions.Count);
			}

			if (grid.ColumnDefinitions.Count > 0)
			{
				Grid.SetColumnSpan(view, grid.ColumnDefinitions.Count);
			}

			// We need to delete the view reference from its parent if it was previously added.
			((Layout?)view.Parent)?.Remove(view);
		}

		layout.Children.Add(view);
	}

	internal Layout GetLayout()
	{
		layoutWeakReference.TryGetTarget(out var layout);
		return layout ?? throw new ObjectDisposedException("Layout Disposed");
	}

	View GetViewForState(string state)
	{
		var view = StateViews.FirstOrDefault(x => StateView.GetStateKey(x) == state);
		return view ?? throw new StateContainerException($"View for {state} not defined.");
	}
}