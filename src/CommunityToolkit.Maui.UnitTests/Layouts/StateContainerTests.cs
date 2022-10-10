using CommunityToolkit.Maui.Layouts;
using FluentAssertions;
using Nito.AsyncEx;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Layouts;

public class StateContainerTests : BaseTest
{
	readonly IReadOnlyList<View> stateViews = new List<View>
	{
		new Label() { Text = "Loading" },
		new Label() { Text = "Error" },
		new Label() { Text = "Anything", HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.End },
	};

	class StateKey
	{
		public const string Loading = "LoadingStateKey";
		public const string Error = "ErrorStateKey";
		public const string Anything = "AnythingStateKey";
	}

	readonly VerticalStackLayout layout = new()
	{
		Children =
		{
			new Label { Text = "Default" }
		}
	};

	readonly Grid grid = new()
	{
		RowDefinitions =
		{
			new RowDefinition(),
			new RowDefinition(),
		},
		ColumnDefinitions =
		{
			new ColumnDefinition(),
			new ColumnDefinition()
		}
	};

	public StateContainerTests()
	{
		StateView.SetStateKey(stateViews[0], StateKey.Loading);
		StateView.SetStateKey(stateViews[1], StateKey.Error);
		StateView.SetStateKey(stateViews[2], StateKey.Anything);

		StateContainer.SetCurrentState(layout, string.Empty);
		StateContainer.SetShouldAnimateOnStateChange(layout, false);
		StateContainer.SetStateViews(layout, stateViews);

		StateContainer.SetCurrentState(grid, string.Empty);
		StateContainer.SetShouldAnimateOnStateChange(grid, false);
		StateContainer.SetStateViews(grid, stateViews);
	}

	[Fact]
	public void StateView_HasStateKey()
	{
		var stateView = StateContainer.GetStateViews(layout).First();
		var stateKey = StateView.GetStateKey(stateView);
		Assert.Equal(StateKey.Loading, stateKey);
	}

	[Fact]
	public void StateContainer_ElementNotInheritsLayoutThrowsException()
	{
		var invalidElement = new View();

		var exception = Assert.Throws<InvalidOperationException>(() =>
		{
			// Use AsyncContext to test `async void` methods https://stackoverflow.com/a/14207615/5953643
			AsyncContext.Run(() => StateContainer.SetCurrentState(invalidElement, "abc"));
		});

		exception.Message.Should().StartWith("Cannot create the StateContainerController.");
	}

	[Fact]
	public void StateContainer_CreatesControllerWithLayout()
	{
		var controller = StateContainer.GetContainerController(layout);

		Assert.NotNull(controller);
		Assert.IsType<VerticalStackLayout>(controller.GetLayout());
	}

	[Fact]
	public void Controller_CancelsAnimationTokenOnRebuild()
	{
		var controller = new StateContainerController(layout)
		{
			StateViews = StateContainer.GetStateViews(layout) ?? new List<View>()
		};
		var token = controller.RebuildAnimationTokenSource(layout);
		var newToken = controller.RebuildAnimationTokenSource(layout);

		Assert.True(token.IsCancellationRequested);
		Assert.False(newToken.IsCancellationRequested);
	}

	[Fact]
	public async Task Controller_CanceledSwitchToStateThrowsException()
	{
		var controller = new StateContainerController(layout)
		{
			StateViews = StateContainer.GetStateViews(layout) ?? new List<View>()
		};
		var cts = new CancellationTokenSource();
		cts.Cancel();

		await Assert.ThrowsAsync<OperationCanceledException>(() => controller.SwitchToState(StateKey.Loading, false, cts.Token));
	}

	[Fact]
	public async Task Controller_CanceledSwitchToContentThrowsException()
	{
		var controller = new StateContainerController(layout)
		{
			StateViews = StateContainer.GetStateViews(layout) ?? new List<View>()
		};
		var cts = new CancellationTokenSource();
		cts.Cancel();

		await Assert.ThrowsAsync<OperationCanceledException>(() => controller.SwitchToContent(false, cts.Token));
	}

	[Fact]
	public async Task Controller_ReturnsErrorLabelOnInvalidState()
	{
		var controller = new StateContainerController(layout)
		{
			StateViews = StateContainer.GetStateViews(layout) ?? new List<View>()
		};
		await controller.SwitchToState("InvalidStateKey", false);
		var label = controller.GetLayout().Children.First();

		Assert.IsType<Label>(label);
		Assert.StartsWith("View for InvalidStateKey not defined.", ((Label)label).Text);
	}

	[Fact]
	public async Task Controller_SwitchesToStateFromContentSuccess()
	{
		var controller = new StateContainerController(layout)
		{
			StateViews = StateContainer.GetStateViews(layout)
		};
		await controller.SwitchToState(StateKey.Loading, false);
		var state = controller.GetLayout().Children.First();

		Assert.IsType<Label>(state);
		Assert.Equal("Loading", ((Label)state).Text);
	}

	[Fact]
	public async Task Controller_SwitchesToContentFromStateSuccess()
	{
		var controller = new StateContainerController(layout)
		{
			StateViews = StateContainer.GetStateViews(layout) ?? new List<View>()
		};
		await controller.SwitchToState(StateKey.Anything, false);
		var label = controller.GetLayout().Children.First();

		Assert.IsType<Label>(label);
		Assert.Equal("Anything", ((Label)label).Text);

		await controller.SwitchToContent(false);
		label = controller.GetLayout().Children.First();

		Assert.IsType<Label>(label);
		Assert.Equal("Default", ((Label)label).Text);
	}

	[Fact]
	public async Task Controller_SwitchesToStateFromStateSuccess()
	{
		var controller = new StateContainerController(layout)
		{
			StateViews = StateContainer.GetStateViews(layout) ?? new List<View>()
		};
		await controller.SwitchToState(StateKey.Anything, false);
		var label = controller.GetLayout().Children.First();

		Assert.IsType<Label>(label);
		Assert.Equal("Anything", ((Label)label).Text);

		await controller.SwitchToState(StateKey.Loading, false);
		label = controller.GetLayout().Children.First();

		Assert.IsType<Label>(label);
		Assert.Equal("Loading", ((Label)label).Text);
	}

	[Fact]
	public async Task Controller_SwitchesToStateFromSameStateSuccess()
	{
		var controller = new StateContainerController(layout)
		{
			StateViews = StateContainer.GetStateViews(layout) ?? new List<View>()
		};
		await controller.SwitchToState(StateKey.Loading, false);
		var label = controller.GetLayout().Children.First();

		Assert.IsType<Label>(label);
		Assert.Equal("Loading", ((Label)label).Text);

		await controller.SwitchToState(StateKey.Loading, false);
		label = controller.GetLayout().Children.First();

		Assert.IsType<Label>(label);
		Assert.Equal("Loading", ((Label)label).Text);
	}

	[Fact]
	public async Task Controller_GridStateInnerLayoutSpansParent()
	{
		var controller = new StateContainerController(grid)
		{
			StateViews = StateContainer.GetStateViews(grid) ?? new List<View>()
		};
		await controller.SwitchToState(StateKey.Loading, false);
		var innerLayout = controller.GetLayout().Children.First();

		Assert.IsType<VerticalStackLayout>(innerLayout);
		Assert.Equal(Grid.GetColumnSpan((VerticalStackLayout)innerLayout), grid.ColumnDefinitions.Count);
		Assert.Equal(Grid.GetRowSpan((VerticalStackLayout)innerLayout), grid.RowDefinitions.Count);
	}

	[Fact]
	public async Task Controller_GridStateInnerLayoutRespectsViewOptions()
	{
		var controller = new StateContainerController(grid)
		{
			StateViews = StateContainer.GetStateViews(grid) ?? new List<View>()
		};
		await controller.SwitchToState(StateKey.Anything, false);
		var innerLayout = controller.GetLayout().Children.First();

		Assert.IsType<VerticalStackLayout>(innerLayout);

		var label = ((VerticalStackLayout)innerLayout).Children.First();

		Assert.IsType<Label>(label);
		Assert.Equal(((VerticalStackLayout)innerLayout).VerticalOptions, ((Label)label).VerticalOptions);
		Assert.Equal(((VerticalStackLayout)innerLayout).HorizontalOptions, ((Label)label).HorizontalOptions);
	}

}