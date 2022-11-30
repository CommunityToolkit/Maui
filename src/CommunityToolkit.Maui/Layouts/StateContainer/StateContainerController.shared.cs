using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Maui.Layouts;

/// <summary>
/// StateContainer Controller
/// </summary>
sealed class StateContainerController : IDisposable
{
	readonly WeakReference<Layout> layoutWeakReference;
	string? previousState = null;
	List<View> originalContent = Enumerable.Empty<View>().ToList();
	CancellationTokenSource? animationTokenSource;

	/// <summary>
	/// Initialize <see cref="StateContainerController"/> with a <see cref="Layout"/>
	/// </summary>
	/// <param name="layout"></param>
	public StateContainerController(Layout layout) => layoutWeakReference = new WeakReference<Layout>(layout);

	/// <summary>
	/// The StateViews defined in the StateContainer.
	/// </summary>
	public IList<View> StateViews { get; set; } = Enumerable.Empty<View>().ToList();

	/// <summary>
	/// Dispose <see cref="StateContainerController"/>
	/// </summary>
	public void Dispose() => animationTokenSource?.Dispose();

	/// <summary>
	/// Display the default content.
	/// </summary>
	/// <param name="shouldAnimate"></param>
	/// <param name="cancellationToken"></param>
	public async Task SwitchToContent(bool shouldAnimate, CancellationToken? cancellationToken = null)
	{
		var layout = GetLayout();
		var token = cancellationToken ?? RebuildAnimationTokenSource(layout);

		await FadeLayoutChildren(layout, shouldAnimate, true, token);
		token.ThrowIfCancellationRequested();

		previousState = null;
		layout.Children.Clear();

		// Put the original content back in.
		foreach (var item in originalContent)
		{
			item.Opacity = shouldAnimate ? 0 : 1;
			layout.Children.Add(item);
		}

		token.ThrowIfCancellationRequested();
		await FadeLayoutChildren(layout, shouldAnimate, false, token);
	}

	/// <summary>
	/// Display the <see cref="View"/> for the given StateKey.
	/// </summary>
	/// <param name="state"></param>
	/// <param name="shouldAnimate"></param>
	/// <param name="cancellationToken"></param>
	public async Task SwitchToState(string state, bool shouldAnimate, CancellationToken? cancellationToken = null)
	{
		var layout = GetLayout();
		var token = cancellationToken ?? RebuildAnimationTokenSource(layout);
		var view = GetViewForState(state);

		await FadeLayoutChildren(layout, shouldAnimate, true, token);
		token.ThrowIfCancellationRequested();

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
		if (layout is Grid grid)
		{
			// We create a VerticalStackLayout spanning the Grid.
			// It takes VerticalOptions and HorizontalOptions from the
			// view to allow for more control over how it layouts.
			var innerLayout = new VerticalStackLayout
			{
				Opacity = shouldAnimate ? 0 : 1,
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
			((Layout)view.Parent)?.Remove(view);

			innerLayout.Children.Add(view);
			layout.Children.Add(innerLayout);
		}
		else
		{
			layout.Children.Add(view);
		}

		token.ThrowIfCancellationRequested();
		await FadeLayoutChildren(layout, shouldAnimate, false, token);
	}

	internal Layout GetLayout()
	{
		layoutWeakReference.TryGetTarget(out var layout);
		return layout ?? throw new ObjectDisposedException("Layout Disposed");
	}

	static async ValueTask FadeLayoutChildren(Layout layout, bool shouldAnimate, bool isHidden, CancellationToken token)
	{
		if (shouldAnimate && layout.Children.Count > 0)
		{
			var opacity = 1;
			var length = 500u;

			if (isHidden)
			{
				opacity = 0;
				length = 100u;
			}

			await Task.WhenAll(layout.Children.OfType<View>().Select(view => view.FadeTo(opacity, length))).WaitAsync(token);
		}
	}

	View GetViewForState(string state)
	{
		var view = StateViews.FirstOrDefault(x => StateView.GetStateKey(x) == state);
		return view ?? throw new StateContainerException($"View for {state} not defined.");
	}

	[MemberNotNull(nameof(animationTokenSource))]
	internal CancellationToken RebuildAnimationTokenSource(Layout layout)
	{
		animationTokenSource?.Cancel();
		animationTokenSource?.Dispose();

		foreach (var child in layout.Children)
		{
			((View)child).CancelAnimations();
		}

		animationTokenSource = new CancellationTokenSource();
		return animationTokenSource.Token;
	}
}