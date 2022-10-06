using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using ViewExtensions = Microsoft.Maui.Controls.ViewExtensions;

namespace CommunityToolkit.Maui.Layouts;

/// <summary>
/// StateLayout Controller
/// </summary>
class StateLayoutController : IDisposable
{
	readonly WeakReference<Layout> layoutWeakReference;

	bool layoutIsGrid, isDisposed;
	LayoutState previousState = LayoutState.None;
	List<View> originalContent = Enumerable.Empty<View>().ToList();
	CancellationTokenSource? animationTokenSource;

	/// <summary>
	/// StateLayout Controller constructor
	/// </summary>
	/// <param name="layout"></param>
	public StateLayoutController(Layout layout) => layoutWeakReference = new WeakReference<Layout>(layout);

	/// <summary>
	/// The StateViews defined in the StateLayout.
	/// </summary>
	public IList<StateView> StateViews { get; set; } = Enumerable.Empty<StateView>().ToList();

	/// <summary>Dispose <see cref="StateLayoutController"/>.</summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Display the default content.
	/// </summary>
	/// <param name="shouldAnimate"></param>
	public async Task SwitchToContent(bool shouldAnimate)
	{
		var layout = GetLayout();
		var token = RebuildAnimationTokenSource(layout);

		previousState = LayoutState.None;
		await FadeLayoutChildren(layout, shouldAnimate, true);

		token.ThrowIfCancellationRequested();

		// Put the original content back in.
		layout.Children.Clear();

		foreach (var item in originalContent)
		{
			item.Opacity = shouldAnimate ? 0 : 1;
			layout.Children.Add(item);
		}

		await FadeLayoutChildren(layout, shouldAnimate, false);
	}

	/// <summary>
	/// Display the StateView template for the given custom state key.
	/// </summary>
	/// <param name="customState"></param>
	/// <param name="animate"></param>
	public Task SwitchToTemplate(string customState, bool animate)
		=> SwitchToTemplate(LayoutState.Custom, customState, animate);

	/// <summary>
	/// Display the StateView template for the given LayoutState option.
	/// </summary>
	/// <param name="state"></param>
	/// <param name="customState"></param>
	/// <param name="animate"></param>
	/// <exception cref="ArgumentException"></exception>
	public async Task SwitchToTemplate(LayoutState state, string? customState, bool animate)
	{
		var layout = GetLayout();
		var token = RebuildAnimationTokenSource(layout);

		// Put the original content somewhere where we can restore it.
		if (previousState is LayoutState.None)
		{
			originalContent = new List<View>();

			foreach (var item in layout.Children)
			{
				originalContent.Add((View)item);
			}
		}

		var view = GetViewForState(state, customState);

		if (view is not null)
		{
			previousState = state;

			await FadeLayoutChildren(layout, animate, true);

			if (token.IsCancellationRequested)
			{
				return;
			}


			layout.Children.Clear();

			var repeatCount = GetRepeatCount(state, customState);
			var template = GetTemplate(state, customState);

			if (template is not null)
			{
				// We have a template we can use.
				var items = new List<int>(repeatCount);

				for (var i = 0; i < repeatCount; i++)
				{
					items.Add(i);
				}

				// We create a VerticalStackLayout to stack repeating items.
				// It takes VerticalOptions and HorizontalOptions from the
				// StateView to allow for more control over how it layouts.
				var s = new VerticalStackLayout
				{
					Opacity = animate ? 0 : 1,
					VerticalOptions = view.VerticalOptions,
					HorizontalOptions = view.HorizontalOptions
				};

				// If the layout we're applying StateLayout to is a Grid,
				// we want to have the StateLayout span the entire Grid surface.
				// Otherwise it would just end up in row 0 : column 0.
				if (layout is Grid grid)
				{
					if (grid.RowDefinitions.Count > 0)
					{
						Grid.SetRowSpan(s, grid.RowDefinitions.Count);
					}

					if (grid.ColumnDefinitions.Count > 0)
					{
						Grid.SetColumnSpan(s, grid.ColumnDefinitions.Count);
					}

				}

				BindableLayout.SetItemTemplate(s, template);
				BindableLayout.SetItemsSource(s, items);

				layout.Children.Add(s);
			}
			else
			{
				if (repeatCount > 1)
				{
					throw new ArgumentException("Please use a Template instead of directly defining content when using a RepeatCount > 1.");
				}


				// No template, so we use the children of the StateView.
				// We create a VerticalStackLayout to stack repeating items.
				// It takes VerticalOptions and HorizontalOptions from the
				// StateView to allow for more control over how it layouts.
				var s = new VerticalStackLayout
				{
					Opacity = animate ? 0 : 1,
					VerticalOptions = view.VerticalOptions,
					HorizontalOptions = view.HorizontalOptions
				};

				// If the layout we're applying StateLayout to is a Grid,
				// we want to have the StateLayout span the entire Grid surface.
				// Otherwise it would just end up in row 0 : column 0.
				if (layout is Grid grid)
				{
					if (grid.RowDefinitions.Count > 0)
					{
						Grid.SetRowSpan(s, grid.RowDefinitions.Count);
					}

					if (grid.ColumnDefinitions.Count > 0)
					{
						Grid.SetColumnSpan(s, grid.ColumnDefinitions.Count);
					}


					layout.Children.Add(s);
					layoutIsGrid = true;
				}

				var itemView = CreateItemView(state, customState);

				if (itemView is not null)
				{
					if (layoutIsGrid)
					{
						s.Children.Add(itemView);
					}
					else
					{
						layout.Children.Add(itemView);
					}

				}
			}

			await FadeLayoutChildren(layout, animate, false);
		}
	}

	internal Layout GetLayout()
	{
		layoutWeakReference.TryGetTarget(out var layout);
		return layout ?? throw new ObjectDisposedException("Layout Disposed");
	}

	/// <summary>Dispose <see cref="StateLayoutController"/>.</summary>
	/// <param name="isDisposing">Is disposing.</param>
	protected virtual void Dispose(bool isDisposing)
	{
		if (!isDisposed)
		{
			isDisposed = true;

			if (isDisposing)
			{
				animationTokenSource?.Dispose();
			}
		}
	}

	static async ValueTask FadeLayoutChildren(Layout layout, bool shouldAnimate, bool isHidden)
	{
		if (shouldAnimate && layout.Children.Count > 0)
		{
			var opacity = 1;
			var time = 500u;

			if (isHidden)
			{
				opacity = 0;
				time = 100u;
			}

			await Task.WhenAll(layout.Children.OfType<View>().Select(a => ViewExtensions.FadeTo(a, opacity, time)));
		}
	}

	StateView? GetViewForState(LayoutState state, string? customState)
	{
		var view = StateViews.FirstOrDefault(x => (x.StateKey == state && state != LayoutState.Custom) ||
						(state == LayoutState.Custom && x.CustomStateKey == customState));

		return view;
	}

	int GetRepeatCount(LayoutState state, string? customState)
	{
		var template = StateViews.FirstOrDefault(x => (x.StateKey == state && state != LayoutState.Custom) ||
					   (state == LayoutState.Custom && x.CustomStateKey == customState));

		return template is not null
				? template.RepeatCount
				: 1;
	}

	DataTemplate? GetTemplate(LayoutState state, string? customState)
	{
		var view = StateViews.FirstOrDefault(x => (x.StateKey == state && state != LayoutState.Custom) ||
					   (state == LayoutState.Custom && x.CustomStateKey == customState));

		return view?.Template;
	}

	View CreateItemView(LayoutState state, string? customState)
	{
		var view = StateViews.FirstOrDefault(x => (x.StateKey == state && state != LayoutState.Custom) ||
						(state == LayoutState.Custom && x.CustomStateKey == customState));

		// TODO: This only allows for a repeatcount of 1.
		// Internally in Xamarin.Forms we cannot add the same element to Children multiple times.
		return view ?? (View)(new Label { Text = $"View for {state}{customState} not defined." });
	}

	[MemberNotNull(nameof(animationTokenSource))]
	CancellationToken RebuildAnimationTokenSource(Layout layout)
	{
		animationTokenSource?.Cancel();
		animationTokenSource?.Dispose();

		foreach (var child in layout.Children)
		{
			ViewExtensions.CancelAnimations((View)child);
		}

		animationTokenSource = new CancellationTokenSource();
		return animationTokenSource.Token;
	}

}