using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Maui.Views;
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
		new Label() { Text = "Another" },
	};

	readonly VerticalStackLayout layout = new()
	{
		Children =
		{
			new Label { Text = "Default" }
		}
	};

	readonly Grid grid = new()
	{
		Children =
		{
			new Label { Text = "Default" }
		}
	};

	public StateContainerTests()
	{
		StateView.SetStateKey(stateViews[0], "LoadingState");
		StateView.SetStateKey(stateViews[1], "ErrorState");
		StateView.SetStateKey(stateViews[2], "AnythingState");

		StateContainer.SetCurrentState(layout, string.Empty);
		StateContainer.SetShouldAnimateOnStateChange(layout, false);
		StateContainer.SetStateViews(layout, stateViews);

		StateContainer.SetCurrentState(grid, string.Empty);
		StateContainer.SetShouldAnimateOnStateChange(grid, false);
		StateContainer.SetStateViews(grid, stateViews);
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
		StateContainer.SetCurrentState(layout, "abc");

		var controller = StateContainer.GetContainerController(layout);

		Assert.NotNull(controller);
		Assert.IsType<VerticalStackLayout>(controller.GetLayout());
	}

	[Fact]
	public async Task StateContainerController_SwitchesToStateSuccess()
	{
		var controller = new StateContainerController(layout)
		{
			StateViews = StateContainer.GetStateViews(layout)
		};

		await controller.SwitchToState("LoadingState", false);

		var state = controller.GetLayout().Children.First();

		Assert.IsType<Label>(state);
		Assert.Equal("Loading", ((Label)state).Text);
	}

	[Fact]
	public async Task StateContainerController_SwitchesToContentSuccess()
	{
		var controller = new StateContainerController(layout)
		{
			StateViews = StateContainer.GetStateViews(layout) ?? new List<View>()
		};

		await controller.SwitchToState("abc", false);
		await controller.SwitchToContent(false);

		var label = controller.GetLayout().Children.First();

		Assert.IsType<Label>(label);
		Assert.Equal("Default", ((Label)label).Text);
	}

}