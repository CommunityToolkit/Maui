namespace CommunityToolkit.Maui.Layouts;

/// <summary>
/// StateContainer Controller
/// </summary>
sealed class StateContainerController
{
	readonly WeakReference<Layout> layoutWeakReference;

	string? previousState;
	List<View> originalContent = Enumerable.Empty<View>().ToList();

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
			originalContent = new List<View>();

			foreach (var item in layout.Children)
			{
				originalContent.Add((View)item);
			}
		}

		previousState = state;
		layout.Children.Clear();

		// If the layout we're applying StateContainer to is a Grid,
		// we want to have the StateContainer span the entire Grid surface.
		// Otherwise it would just end up in row 0 : column 0.
		if (layout is IGridLayout grid)
		{
			// We create a VerticalStackLayout spanning the Grid.
			// It takes VerticalOptions and HorizontalOptions from the
			// view to allow for more control over how it layouts.
			var innerLayout = new VerticalStackLayout
			{
				VerticalOptions = view.VerticalOptions,
				HorizontalOptions = view.HorizontalOptions
			};

			if (grid.RowDefinitions.Count > 0)
			{
				Grid.SetRowSpan(innerLayout, grid.RowDefinitions.Count);
			}

			if (grid.ColumnDefinitions.Count > 0)
			{
				Grid.SetColumnSpan(innerLayout, grid.ColumnDefinitions.Count);
			}

			// We need to delete the view reference from its parent if it was previously added.
			((Layout?)view.Parent)?.Remove(view);

			innerLayout.Children.Add(view);
			layout.Children.Add(innerLayout);
		}
		else
		{
			layout.Children.Add(view);
		}
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