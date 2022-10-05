using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Layouts;
using FluentAssertions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Layouts;

public class StateLayoutTests : BaseTest
{
	readonly List<StateView> stateViews;
	readonly VerticalStackLayout layout;
	readonly Grid grid;

	public StateLayoutTests()
	{
		stateViews = new List<StateView>()
		{
			new StateView()
			{
				StateKey = LayoutState.Loading,
				Content = new Label() { Text = "Loading" }
			},
			new StateView()
			{
				StateKey = LayoutState.Error,
				Content = new Label() { Text = "Error" }
			},
			new StateView()
			{
				StateKey = LayoutState.Custom,
				CustomStateKey = "MyCustomState",
				Content = new Label() { Text = "Custom" }
			}
		};

		layout = new VerticalStackLayout()
		{
			Children = { new Label() { Text = "Default" } }
		};

		StateLayout.SetCurrentState(layout, LayoutState.None);
		StateLayout.SetAnimateStateChanges(layout, false);
		StateLayout.SetStateViews(layout, stateViews);

		grid = new Grid()
		{
			RowDefinitions = { new RowDefinition() },
			ColumnDefinitions = { new ColumnDefinition() }
		};

		grid.Add(new Label() { Text = "Default" });

		StateLayout.SetCurrentState(grid, LayoutState.None);
		StateLayout.SetAnimateStateChanges(grid, false);
		StateLayout.SetStateViews(grid, stateViews);
	}

	[Fact]
	public void StateLayout_ElementNotInheritsLayoutThrowsException()
	{
		var invalidElement = new View();
		var exception = Assert.Throws<InvalidOperationException>(() => StateLayout.SetCurrentState(invalidElement, LayoutState.Loading));
		exception.Message.Should().StartWith("Cannot create the StateLayoutController.");
	}

	[Fact]
	public void StateLayout_CreatesControllerWithLayout()
	{
		StateLayout.SetCurrentState(layout, LayoutState.Loading);
		var controller = StateLayout.GetLayoutController(layout);
		Assert.NotNull(controller);
		Assert.NotNull(controller.GetLayout());
		Assert.IsType<VerticalStackLayout>(controller.GetLayout());
	}

	[Fact]
	public void StateLayoutController_SwitchesToTemplateSuccess()
	{
		var controller = new StateLayoutController(layout)
		{
			StateViews = StateLayout.GetStateViews(layout) ?? new List<StateView>()
		};
		controller.SwitchToTemplate(LayoutState.Loading, null, false);
		var state = controller.GetLayout()?.Children.First();

		Assert.IsType<StateView>(state);
		var label = ((StateView)state).Content;
		Assert.IsType<Label>(label);
		Assert.Equal("Loading", ((Label)label).Text);
	}

	[Fact]
	public void StateLayoutController_SwitchesToContentSuccess()
	{
		var controller = new StateLayoutController(layout)
		{
			StateViews = StateLayout.GetStateViews(layout) ?? new List<StateView>()
		};
		controller.SwitchToTemplate(LayoutState.Loading, null, false);
		controller.SwitchToContent(false);
		var label = controller.GetLayout()?.Children.First();

		Assert.IsType<Label>(label);
		Assert.Equal("Default", ((Label)label).Text);
	}

}