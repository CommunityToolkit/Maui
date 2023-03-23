using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
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
		Assert.True(StateContainer.GetCanStateChange(layout));
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
		Assert.True(StateContainer.GetCanStateChange(layout));
	}

	[Fact]
	public async Task StateContainer_DefaultAnimation()
	{
		layout.EnableAnimations();
		foreach (var child in layout.Children)
		{
			child.EnableAnimations();
		}

		var changeStateWithAnimationTask = StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, CancellationToken.None);

		Assert.False(StateContainer.GetCanStateChange(layout));
		await changeStateWithAnimationTask;

		Assert.True(StateContainer.GetCanStateChange(layout));
		Assert.Equal(StateKey.Error, StateContainer.GetCurrentState(layout));
	}

	[Fact]
	public async Task StateContainer_CustomAnimation()
	{
		layout.EnableAnimations();
		foreach (var child in layout.Children)
		{
			child.EnableAnimations();
		}

		var beforeStateChangeAnimation = new Animation
		{
			Duration = 1
		};

		var afterStateChangeAnimation = new Animation
		{
			Duration = 1
		};

		var changeStateWithAnimationTask = StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, beforeStateChangeAnimation, afterStateChangeAnimation, CancellationToken.None);

		Assert.False(StateContainer.GetCanStateChange(layout));
		await changeStateWithAnimationTask;

		Assert.True(StateContainer.GetCanStateChange(layout));
		Assert.Equal(StateKey.Error, StateContainer.GetCurrentState(layout));
	}

	[Fact]
	public async Task StateContainer_FuncAnimation()
	{
		layout.EnableAnimations();
		foreach (var child in layout.Children)
		{
			child.EnableAnimations();
		}

		var changeStateWithAnimationTask = StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, null, CustomAnimation, CancellationToken.None);

		Assert.False(StateContainer.GetCanStateChange(layout));

		await changeStateWithAnimationTask;

		Assert.True(StateContainer.GetCanStateChange(layout));
		Assert.Equal(StateKey.Error, StateContainer.GetCurrentState(layout));

		static Task CustomAnimation(VisualElement element, CancellationToken token) => element.RotateTo(0.75, 1000).WaitAsync(token);
	}

	[Fact]
	public async Task StateContainer_DefaultAnimation_Timeout()
	{
		layout.EnableAnimations();
		foreach (var child in layout.Children)
		{
			child.EnableAnimations();
		}

		var cancelledTokenSource = new CancellationTokenSource(TimeSpan.FromMicroseconds(1));
		await Assert.ThrowsAsync<TaskCanceledException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, cancelledTokenSource.Token));
	}

	[Fact]
	public async Task StateContainer_CustomAnimation_Timeout()
	{
		layout.EnableAnimations();
		foreach (var child in layout.Children)
		{
			child.EnableAnimations();
		}

		var beforeStateChangeAnimation = new Animation
		{
			Duration = 1
		};

		var afterStateChangeAnimation = new Animation
		{
			Duration = 1
		};

		var cancelledTokenSource = new CancellationTokenSource(TimeSpan.FromMicroseconds(1));
		await Assert.ThrowsAsync<TaskCanceledException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, beforeStateChangeAnimation, afterStateChangeAnimation, cancelledTokenSource.Token));
	}

	[Fact]
	public async Task StateContainer_FuncAnimation_Timeout()
	{
		layout.EnableAnimations();
		foreach (var child in layout.Children)
		{
			child.EnableAnimations();
		}

		var cancelledTokenSource = new CancellationTokenSource(TimeSpan.FromMicroseconds(1));
		await Assert.ThrowsAsync<TaskCanceledException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, null, CustomAnimation, cancelledTokenSource.Token));

		static Task CustomAnimation(VisualElement element, CancellationToken token) => element.RotateTo(0.75, 1000).WaitAsync(token);
	}

	[Fact]
	public async Task StateContainer_ChangingStateWhenCanStateChangePropertyIsFalse_DefaultAnimation()
	{
		layout.EnableAnimations();
		foreach (var child in layout.Children)
		{
			child.EnableAnimations();
		}

		var changeStateWithAnimationTask = StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, CancellationToken.None);

		Assert.False(StateContainer.GetCanStateChange(layout));
		var exception = Assert.Throws<StateContainerException>(() => StateContainer.SetCurrentState(layout, StateKey.Anything));
		var exception2 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, CancellationToken.None));
		var exception3 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, new Animation(), null, CancellationToken.None));
		var exception4 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, (layout, _) => layout.FadeTo(1), null, CancellationToken.None));

		await changeStateWithAnimationTask;

		Assert.Equal("CanStateChange is false. CurrentState cannot be changed while a state change is in progress. To avoid this exception, first verify CanStateChange is True before changing CurrentState.", exception.Message);
		Assert.Equal(exception.Message, exception2.Message);
		Assert.Equal(exception.Message, exception3.Message);
		Assert.Equal(exception.Message, exception4.Message);

		Assert.True(StateContainer.GetCanStateChange(layout));
	}

	[Fact]
	public async Task StateContainer_ChangingStateWhenCanStateChangePropertyIsFalse_CustomBeforeAnimation()
	{
		layout.EnableAnimations();
		foreach (var child in layout.Children)
		{
			child.EnableAnimations();
		}

		var beforeStateChangeAnimation = new Animation
		{
			Duration = 1
		};

		var changeStateWithAnimationTask = StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, beforeStateChangeAnimation, null, CancellationToken.None);

		Assert.False(StateContainer.GetCanStateChange(layout));
		var exception = Assert.Throws<StateContainerException>(() => StateContainer.SetCurrentState(layout, StateKey.Anything));
		var exception2 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, CancellationToken.None));
		var exception3 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, new Animation(), null, CancellationToken.None));
		var exception4 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, (layout, _) => layout.FadeTo(1), null, CancellationToken.None));

		await changeStateWithAnimationTask;

		Assert.Equal("CanStateChange is false. CurrentState cannot be changed while a state change is in progress. To avoid this exception, first verify CanStateChange is True before changing CurrentState.", exception.Message);
		Assert.Equal(exception.Message, exception2.Message);
		Assert.Equal(exception.Message, exception3.Message);
		Assert.Equal(exception.Message, exception4.Message);

		Assert.True(StateContainer.GetCanStateChange(layout));
	}

	[Fact]
	public async Task StateContainer_ChangingStateWhenCanStateChangePropertyIsFalse_CustomAfterAnimation()
	{
		layout.EnableAnimations();
		foreach (var child in layout.Children)
		{
			child.EnableAnimations();
		}

		var afterStateChangeAnimation = new Animation
		{
			Duration = 1
		};

		var changeStateWithAnimationTask = StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, null, afterStateChangeAnimation, CancellationToken.None);

		Assert.False(StateContainer.GetCanStateChange(layout));
		var exception = Assert.Throws<StateContainerException>(() => StateContainer.SetCurrentState(layout, StateKey.Anything));
		var exception2 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, CancellationToken.None));
		var exception3 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, new Animation(), null, CancellationToken.None));
		var exception4 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, (layout, _) => layout.FadeTo(1), null, CancellationToken.None));

		await changeStateWithAnimationTask;

		Assert.Equal("CanStateChange is false. CurrentState cannot be changed while a state change is in progress. To avoid this exception, first verify CanStateChange is True before changing CurrentState.", exception.Message);
		Assert.Equal(exception.Message, exception2.Message);
		Assert.Equal(exception.Message, exception3.Message);
		Assert.Equal(exception.Message, exception4.Message);

		Assert.True(StateContainer.GetCanStateChange(layout));
	}

	[Fact]
	public async Task StateContainer_ChangingStateWhenCanStateChangePropertyIsFalse_CustomBeforeAndAfterAnimation()
	{
		layout.EnableAnimations();
		foreach (var child in layout.Children)
		{
			child.EnableAnimations();
		}

		var beforeStateChangeAnimation = new Animation
		{
			Duration = 1
		};

		var afterStateChangeAnimation = new Animation
		{
			Duration = 1
		};

		var changeStateWithAnimationTask = StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, beforeStateChangeAnimation, afterStateChangeAnimation, CancellationToken.None);

		Assert.False(StateContainer.GetCanStateChange(layout));
		var exception = Assert.Throws<StateContainerException>(() => StateContainer.SetCurrentState(layout, StateKey.Anything));
		var exception2 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, CancellationToken.None));
		var exception3 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, new Animation(), null, CancellationToken.None));
		var exception4 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, (layout, _) => layout.FadeTo(1), null, CancellationToken.None));

		await changeStateWithAnimationTask;

		Assert.Equal("CanStateChange is false. CurrentState cannot be changed while a state change is in progress. To avoid this exception, first verify CanStateChange is True before changing CurrentState.", exception.Message);
		Assert.Equal(exception.Message, exception2.Message);
		Assert.Equal(exception.Message, exception3.Message);
		Assert.Equal(exception.Message, exception4.Message);

		Assert.True(StateContainer.GetCanStateChange(layout));
	}

	[Fact]
	public async Task StateContainer_ChangingStateWhenCanStateChangePropertyIsFalse_NullAnimations()
	{
		var exception = await Assert.ThrowsAsync<ArgumentException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, (Animation?)null, null, CancellationToken.None));

		Assert.Equal("Anmiation required. Parameters beforeStateChange and afterStateChange cannot both be null", exception.Message);
		Assert.True(StateContainer.GetCanStateChange(layout));
	}

	[Fact]
	public async Task StateContainer_ChangingStateWhenCanStateChangePropertyIsFalse_NullFuncs()
	{
		var exception = await Assert.ThrowsAsync<ArgumentException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, (Func<VisualElement, CancellationToken, Task>?)null, null, CancellationToken.None));

		Assert.Equal("Anmiation required. Parameters beforeStateChange and afterStateChange cannot both be null", exception.Message);
		Assert.True(StateContainer.GetCanStateChange(layout));
	}

	[Fact]
	public async Task StateContainer_ChangingStateWhenCanStateChangePropertyIsFalse_CustomBeforeAnimationFuncs()
	{
		layout.EnableAnimations();
		foreach (var child in layout.Children)
		{
			child.EnableAnimations();
		}

		var changeStateWithAnimationTask = StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, CustomAnimation, null, CancellationToken.None);

		Assert.False(StateContainer.GetCanStateChange(layout));
		var exception = Assert.Throws<StateContainerException>(() => StateContainer.SetCurrentState(layout, StateKey.Anything));
		var exception2 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, CancellationToken.None));
		var exception3 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, new Animation(), null, CancellationToken.None));
		var exception4 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, (layout, _) => layout.FadeTo(1), null, CancellationToken.None));

		await changeStateWithAnimationTask;

		Assert.Equal("CanStateChange is false. CurrentState cannot be changed while a state change is in progress. To avoid this exception, first verify CanStateChange is True before changing CurrentState.", exception.Message);
		Assert.Equal(exception.Message, exception2.Message);
		Assert.Equal(exception.Message, exception3.Message);
		Assert.Equal(exception.Message, exception4.Message);

		Assert.True(StateContainer.GetCanStateChange(layout));

		static Task CustomAnimation(VisualElement element, CancellationToken token) => element.RotateTo(0.75, 1000).WaitAsync(token);
	}

	[Fact]
	public async Task StateContainer_ChangingStateWhenCanStateChangePropertyIsFalse_CustomAfterAnimationFuncs()
	{
		layout.EnableAnimations();
		foreach (var child in layout.Children)
		{
			child.EnableAnimations();
		}

		var changeStateWithAnimationTask = StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, null, CustomAnimation, CancellationToken.None);

		Assert.False(StateContainer.GetCanStateChange(layout));
		var exception = Assert.Throws<StateContainerException>(() => StateContainer.SetCurrentState(layout, StateKey.Anything));
		var exception2 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, CancellationToken.None));
		var exception3 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, new Animation(), null, CancellationToken.None));
		var exception4 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, (layout, _) => layout.FadeTo(1), null, CancellationToken.None));

		await changeStateWithAnimationTask;

		Assert.Equal("CanStateChange is false. CurrentState cannot be changed while a state change is in progress. To avoid this exception, first verify CanStateChange is True before changing CurrentState.", exception.Message);
		Assert.Equal(exception.Message, exception2.Message);
		Assert.Equal(exception.Message, exception3.Message);
		Assert.Equal(exception.Message, exception4.Message);

		Assert.True(StateContainer.GetCanStateChange(layout));

		static Task CustomAnimation(VisualElement element, CancellationToken token) => element.RotateTo(0.75, 1000).WaitAsync(token);
	}

	[Fact]
	public async Task StateContainer_ChangingStateWhenCanStateChangePropertyIsFalse_CustomBeforeAndAfterAnimationFuncs()
	{
		layout.EnableAnimations();
		foreach (var child in layout.Children)
		{
			child.EnableAnimations();
		}

		var changeStateWithAnimationTask = StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, CustomAnimation, CustomAnimation, CancellationToken.None);

		Assert.False(StateContainer.GetCanStateChange(layout));
		var exception = Assert.Throws<StateContainerException>(() => StateContainer.SetCurrentState(layout, StateKey.Anything));
		var exception2 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, CancellationToken.None));
		var exception3 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, new Animation(), null, CancellationToken.None));
		var exception4 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, (layout, _) => layout.FadeTo(1), null, CancellationToken.None));

		await changeStateWithAnimationTask;

		Assert.Equal("CanStateChange is false. CurrentState cannot be changed while a state change is in progress. To avoid this exception, first verify CanStateChange is True before changing CurrentState.", exception.Message);
		Assert.Equal(exception.Message, exception2.Message);
		Assert.Equal(exception.Message, exception3.Message);
		Assert.Equal(exception.Message, exception4.Message);

		Assert.True(StateContainer.GetCanStateChange(layout));

		static Task CustomAnimation(VisualElement element, CancellationToken token) => element.RotateTo(0.75, 1000).WaitAsync(token);
	}

	[Fact]
	public void StateContainer_ElementNotInheritsLayoutThrowsException()
	{
		var invalidElement = new View();

		var exception = Assert.Throws<StateContainerException>(() => StateContainer.SetCurrentState(invalidElement, "abc"));

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