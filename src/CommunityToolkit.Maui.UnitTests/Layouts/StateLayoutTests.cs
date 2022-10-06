using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Maui.Views;
using FluentAssertions;
using Nito.AsyncEx;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Layouts;

public class StateLayoutTests : BaseTest
{
	readonly IReadOnlyList<StateView> stateViews = new List<StateView>
	{
		new StateView()
		{
			StateKey = LayoutState.Loading,
			Content = new Label { Text = "Loading" }
		},
		new StateView()
		{
			StateKey = LayoutState.Error,
			Content = new Label { Text = "Error" }
		},
		new StateView()
		{
			StateKey = LayoutState.Custom,
			CustomStateKey = "MyCustomState",
			Content = new Label { Text = "Custom" }
		}
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

	public StateLayoutTests()
	{
		StateLayout.SetCurrentState(layout, LayoutState.None);
		StateLayout.SetShouldAnimateOnStateChange(layout, false);
		StateLayout.SetStateViews(layout, stateViews);

		StateLayout.SetCurrentState(grid, LayoutState.None);
		StateLayout.SetShouldAnimateOnStateChange(grid, false);
		StateLayout.SetStateViews(grid, stateViews);
	}

	[Fact]
	public void StateLayout_ElementNotInheritsLayoutThrowsException()
	{
		var invalidElement = new View();

		var exception = Assert.Throws<InvalidOperationException>(() =>
		{
			// Use AsyncContext to test `async void` methods https://stackoverflow.com/a/14207615/5953643
			AsyncContext.Run(() => StateLayout.SetCurrentState(invalidElement, LayoutState.Loading));
		});

		exception.Message.Should().StartWith("Cannot create the StateLayoutController.");
	}

	[Fact]
	public void StateLayout_CreatesControllerWithLayout()
	{
		StateLayout.SetCurrentState(layout, LayoutState.Loading);

		var controller = StateLayout.GetLayoutController(layout);

		Assert.NotNull(controller);
		Assert.IsType<VerticalStackLayout>(controller.GetLayout());
	}

	[Fact]
	public async Task StateLayoutController_SwitchesToTemplateSuccess()
	{
		var controller = new StateLayoutController(layout)
		{
			StateViews = StateLayout.GetStateViews(layout)
		};

		await controller.SwitchToTemplate(LayoutState.Loading, null, false);

		var state = controller.GetLayout().Children.First();

		Assert.IsType<StateView>(state);
		var label = ((StateView)state).Content;
		Assert.IsType<Label>(label);
		Assert.Equal("Loading", ((Label)label).Text);
	}

	[Fact]
	public async Task StateLayoutController_SwitchesToContentSuccess()
	{
		var controller = new StateLayoutController(layout)
		{
			StateViews = StateLayout.GetStateViews(layout) ?? new List<StateView>()
		};

		await controller.SwitchToTemplate(LayoutState.Loading, null, false);
		await controller.SwitchToContent(false);

		var label = controller.GetLayout().Children.First();

		Assert.IsType<Label>(label);
		Assert.Equal("Default", ((Label)label).Text);
	}

}