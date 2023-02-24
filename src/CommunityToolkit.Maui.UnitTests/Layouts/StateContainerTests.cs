using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Nito.AsyncEx;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Layouts;

public class StateContainerTests : BaseTest
{
	readonly IList<View> stateViews = new List<View>
	{
		new Label() { Text = "Loading" },
		new Label() { Text = "Error" },
		new Label() { Text = "Anything", HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.End },
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

	readonly StateContainerController controller;
	readonly StateContainerController gridController;

	public StateContainerTests()
	{
		StateView.SetStateKey(stateViews[0], StateKey.Loading);
		StateView.SetStateKey(stateViews[1], StateKey.Error);
		StateView.SetStateKey(stateViews[2], StateKey.Anything);
		
		StateContainer.SetStateViews(layout, stateViews);
		
		StateContainer.SetStateViews(grid, stateViews);

		controller = new StateContainerController(layout)
		{
			StateViews = StateContainer.GetStateViews(layout)
		};

		gridController = new StateContainerController(grid)
		{
			StateViews = StateContainer.GetStateViews(grid)
		};
	}

	[Fact]
	public void StateView_HasStateKey()
	{
		var view = StateContainer.GetStateViews(layout).First();
		Assert.Equal(StateKey.Loading, StateView.GetStateKey(view));
	}

	[Fact]
	public void StateView_SetsStateKey()
	{
		var view = (View)layout.Children.First();
		StateView.SetStateKey(view, StateKey.Anything);
		Assert.Equal(StateKey.Anything, StateView.GetStateKey(view));
	}

	[Fact]
	public void StateContainer_SetsCurrentState()
	{
		StateContainer.SetCurrentState(layout, StateKey.Loading);
		Assert.Equal(StateKey.Loading, StateContainer.GetCurrentState(layout));
	}
	
	[Fact]
	public void StateContainer_CanStateChangePropertyReadOnly()
	{
		var viewModel = new ViewModel
		{
			CanChangeState = false
		};
		layout.BindingContext = viewModel;
		layout.SetBinding(StateContainer.CanStateChangeProperty, nameof(ViewModel.CanChangeState));

		Assert.True(StateContainer.GetCanStateChange(layout));
		Assert.True(viewModel.CanChangeState);

		StateContainer.SetCurrentState(layout, StateKey.Anything);
		Assert.Equal(StateKey.Anything, StateContainer.GetCurrentState(layout));
	}

	[Fact]
	public void StateContainer_ElementNotInheritsLayoutThrowsException()
	{
		var invalidElement = new View();

		var exception = Assert.Throws<StateContainerException>(() =>
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
	public void Controller_ReturnsErrorLabelOnInvalidState()
	{
		Assert.Throws<StateContainerException>(() => controller.SwitchToState("InvalidStateKey"));
	}

	[Fact]
	public void Controller_SwitchesToStateFromContentSuccess()
	{
		controller.SwitchToState(StateKey.Loading);
		var state = controller.GetLayout().Children.First();

		Assert.IsType<Label>(state);
		Assert.Equal("Loading", ((Label)state).Text);
	}

	[Fact]
	public void Controller_SwitchesToContentFromStateSuccess()
	{
		controller.SwitchToState(StateKey.Loading);
		var label = controller.GetLayout().Children.First();

		Assert.IsType<Label>(label);
		Assert.Equal("Loading", ((Label)label).Text);

		controller.SwitchToContent();
		label = controller.GetLayout().Children.First();

		Assert.IsType<Label>(label);
		Assert.Equal("Default", ((Label)label).Text);
	}

	[Fact]
	public void Controller_SwitchesToStateFromStateSuccess()
	{
		controller.SwitchToState(StateKey.Anything);
		var label = controller.GetLayout().Children.First();

		Assert.IsType<Label>(label);
		Assert.Equal("Anything", ((Label)label).Text);

		controller.SwitchToState(StateKey.Loading);
		label = controller.GetLayout().Children.First();

		Assert.IsType<Label>(label);
		Assert.Equal("Loading", ((Label)label).Text);
	}

	[Fact]
	public void Controller_SwitchesToStateFromSameStateSuccess()
	{
		controller.SwitchToState(StateKey.Loading);
		var label = controller.GetLayout().Children.First();

		Assert.IsType<Label>(label);
		Assert.Equal("Loading", ((Label)label).Text);

		controller.SwitchToState(StateKey.Loading);
		label = controller.GetLayout().Children.First();

		Assert.IsType<Label>(label);
		Assert.Equal("Loading", ((Label)label).Text);
	}

	[Fact]
	public void Controller_GridStateInnerLayoutSpansParent()
	{
		gridController.SwitchToState(StateKey.Loading);
		var innerLayout = gridController.GetLayout().Children.First();

		Assert.IsType<VerticalStackLayout>(innerLayout);
		Assert.Equal(Grid.GetColumnSpan((VerticalStackLayout)innerLayout), grid.ColumnDefinitions.Count);
		Assert.Equal(Grid.GetRowSpan((VerticalStackLayout)innerLayout), grid.RowDefinitions.Count);
	}

	[Fact]
	public void Controller_GridStateInnerLayoutRespectsViewOptions()
	{
		gridController.SwitchToState(StateKey.Anything);
		var innerLayout = gridController.GetLayout().Children.First();

		Assert.IsType<VerticalStackLayout>(innerLayout);

		var label = ((VerticalStackLayout)innerLayout).Children.First();

		Assert.IsType<Label>(label);
		Assert.Equal(((VerticalStackLayout)innerLayout).VerticalOptions, ((Label)label).VerticalOptions);
		Assert.Equal(((VerticalStackLayout)innerLayout).HorizontalOptions, ((Label)label).HorizontalOptions);
	}

	class ViewModel : INotifyPropertyChanged
	{
		bool canStateChange;
		Command? changeStateCommand;

		public bool CanChangeState
		{
			get => canStateChange;
			set
			{
				if (value != canStateChange)
				{
					canStateChange = value;
					OnPropertyChanged();
					ChangeStateCommand.ChangeCanExecute();
				}
			}
		}

		Command ChangeStateCommand => changeStateCommand ??= new Command(() => Debug.WriteLine("Command Tapped"), () => CanChangeState);

		public event PropertyChangedEventHandler? PropertyChanged;

		void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	static class StateKey
	{
		public const string Loading = "LoadingStateKey";
		public const string Error = "ErrorStateKey";
		public const string Anything = "AnythingStateKey";
	}
}