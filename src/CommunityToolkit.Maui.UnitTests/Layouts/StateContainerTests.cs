using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;
using ViewExtensions = Microsoft.Maui.Controls.ViewExtensions;

namespace CommunityToolkit.Maui.UnitTests.Layouts;

public class StateContainerTests : BaseTest
{
	readonly IList<View> stateViews =
	[
		new Label() { Text = "Loading" },
		new Label() { Text = "Error" },
		new Label() { Text = "Anything", HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.End },
	];

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
		var view = StateContainer.GetStateViews(layout)[0];
		Assert.Equal(StateKey.Loading, StateView.GetStateKey(view));
	}

	[Fact]
	public void StateView_SetsStateKey()
	{
		var view = (View)layout.Children[0];
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
	public void StateContainer_EmptyStateBeforeAnyStateKeepsTheDefaultContent()
	{
		var defaultContent = layout.Children[0];

		// For example a view model whose state property starts as string.Empty
		StateContainer.SetCurrentState(layout, string.Empty);

		Assert.Same(defaultContent, Assert.Single(layout.Children));
	}

	[Fact]
	public void StateContainer_DefaultContentAddedAfterAStateIsKept()
	{
		var defaultContent = layout.Children[0];
		var addedContent = new Label { Text = "Added" };

		StateContainer.SetCurrentState(layout, StateKey.Loading);
		StateContainer.SetCurrentState(layout, string.Empty);
		layout.Children.Add(addedContent);

		StateContainer.SetCurrentState(layout, null);

		Assert.Equal([defaultContent, addedContent], layout.Children);
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

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task StateContainer_CancellationTokenExpired()
	{
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		layout.EnableAnimations();
		foreach (var child in layout.Children)
		{
			child.EnableAnimations();
		}

		await WaitForCancellationAsync(cts.Token, TestContext.Current.CancellationToken);

		await Assert.ThrowsAsync<OperationCanceledException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task StateContainer_CancellationTokenCanceled()
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

		layout.EnableAnimations();
		foreach (var child in layout.Children)
		{
			child.EnableAnimations();
		}

		// Ensure CancellationToken has expired
		await cts.CancelAsync();

		await Assert.ThrowsAsync<OperationCanceledException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task StateContainer_DefaultAnimation()
	{
		layout.EnableAnimations();
		foreach (var child in layout.Children)
		{
			child.EnableAnimations();
		}

		var changeStateWithAnimationTask = StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, TestContext.Current.CancellationToken);

		Assert.False(StateContainer.GetCanStateChange(layout));
		await changeStateWithAnimationTask;

		Assert.True(StateContainer.GetCanStateChange(layout));
		Assert.Equal(StateKey.Error, StateContainer.GetCurrentState(layout));
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task StateContainer_DefaultAnimation_ReplacedViewsKeepTheirOpacity()
	{
		layout.EnableAnimations();
		var defaultContent = (View)layout.Children[0];
		defaultContent.EnableAnimations();
		var errorView = stateViews[1];

		await StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, TestContext.Current.CancellationToken);

		Assert.DoesNotContain(defaultContent, layout.Children);
		Assert.Equal(1, defaultContent.Opacity);
		Assert.Equal(1, errorView.Opacity);

		await StateContainer.ChangeStateWithAnimation(layout, null, TestContext.Current.CancellationToken);

		Assert.DoesNotContain(errorView, layout.Children);
		Assert.Equal(1, errorView.Opacity);
		Assert.Equal(1, defaultContent.Opacity);

		// A StateView that the fade animation replaced is visible when a state change without animation shows it again
		StateContainer.SetCurrentState(layout, StateKey.Error);

		Assert.Same(errorView, Assert.Single(layout.Children));
		Assert.Equal(1, errorView.Opacity);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task StateContainer_DefaultAnimation_FadesInTheShownViews()
	{
		layout.EnableAnimations();
		foreach (var child in layout.Children)
		{
			child.EnableAnimations();
		}

		var errorView = stateViews[1];
		List<double> errorViewOpacities = [];
		errorView.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == VisualElement.OpacityProperty.PropertyName)
			{
				errorViewOpacities.Add(errorView.Opacity);
			}
		};

		await StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, TestContext.Current.CancellationToken);

		Assert.NotEmpty(errorViewOpacities);
		Assert.Equal(0, errorViewOpacities[0]);
		Assert.Equal(1, errorViewOpacities[^1]);
		Assert.Equal(1, errorView.Opacity);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task StateContainer_DefaultAnimation_CanceledFadeOutKeepsTheReplacedViewsVisible()
	{
		layout.EnableAnimations();
		var defaultContent = (View)layout.Children[0];
		defaultContent.EnableAnimations();

		using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		var changeStateWithAnimationTask = StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, cts.Token);

		await cts.CancelAsync();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => changeStateWithAnimationTask);

		// A fade-out that went on after the state change ended would make the default content transparent again
		while (defaultContent.AnimationIsRunning(nameof(ViewExtensions.FadeToAsync)))
		{
			await Task.Delay(TimeSpan.FromMilliseconds(16), TestContext.Current.CancellationToken);
		}

		StateContainer.SetCurrentState(layout, null);

		Assert.Same(defaultContent, Assert.Single(layout.Children));
		Assert.Equal(1, defaultContent.Opacity);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task StateContainer_DefaultAnimation_CanceledFadeInLeavesTheShownViewsVisible()
	{
		layout.EnableAnimations();
		var defaultContent = (View)layout.Children[0];
		defaultContent.EnableAnimations();
		var errorView = stateViews[1];
		errorView.EnableAnimations();

		using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

		// Cancel when the error view starts to fade in: the state change sets its Opacity to 0 first
		errorView.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == VisualElement.OpacityProperty.PropertyName && errorView.Opacity == 0 && !cts.IsCancellationRequested)
			{
				cts.Cancel();
			}
		};

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, cts.Token));

		Assert.False(errorView.AnimationIsRunning(nameof(ViewExtensions.FadeToAsync)));
		Assert.Equal(1, errorView.Opacity);

		// The next state change gives the error view the Opacity it had when that state change started, so it must not be partly transparent
		await StateContainer.ChangeStateWithAnimation(layout, null, TestContext.Current.CancellationToken);
		StateContainer.SetCurrentState(layout, StateKey.Error);

		Assert.Same(errorView, Assert.Single(layout.Children));
		Assert.Equal(1, errorView.Opacity);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task StateContainer_DefaultAnimation_UndefinedStateKeepsTheCurrentViewsVisible()
	{
		layout.EnableAnimations();
		var defaultContent = (View)layout.Children[0];
		defaultContent.EnableAnimations();

		await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, "UndefinedStateKey", TestContext.Current.CancellationToken));

		Assert.Same(defaultContent, Assert.Single(layout.Children));
		Assert.Equal(1, defaultContent.Opacity);
		Assert.True(StateContainer.GetCanStateChange(layout));
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task StateContainer_DefaultAnimation_NullStateBeforeAnyStateKeepsTheDefaultContent()
	{
		layout.EnableAnimations();
		var defaultContent = (View)layout.Children[0];
		defaultContent.EnableAnimations();

		await StateContainer.ChangeStateWithAnimation(layout, null, TestContext.Current.CancellationToken);

		Assert.Same(defaultContent, Assert.Single(layout.Children));
		Assert.Equal(1, defaultContent.Opacity);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task StateContainer_CustomAnimation()
	{
		layout.EnableAnimations();
		foreach (var child in layout.Children)
		{
			child.EnableAnimations();
		}

		var beforeStateChangeAnimation = new Animation
		{
			Duration = 0.5
		};

		var afterStateChangeAnimation = new Animation
		{
			Duration = 0.5
		};

		var changeStateWithAnimationTask = StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, beforeStateChangeAnimation, afterStateChangeAnimation, TestContext.Current.CancellationToken);

		Assert.False(StateContainer.GetCanStateChange(layout));
		await changeStateWithAnimationTask;

		Assert.True(StateContainer.GetCanStateChange(layout));
		Assert.Equal(StateKey.Error, StateContainer.GetCurrentState(layout));
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task StateContainer_FuncAnimation()
	{
		layout.EnableAnimations();
		foreach (var child in layout.Children)
		{
			child.EnableAnimations();
		}

		var changeStateWithAnimationTask = StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, null, CustomAnimation, TestContext.Current.CancellationToken);

		Assert.False(StateContainer.GetCanStateChange(layout));

		await changeStateWithAnimationTask;

		Assert.True(StateContainer.GetCanStateChange(layout));
		Assert.Equal(StateKey.Error, StateContainer.GetCurrentState(layout));

		static Task CustomAnimation(VisualElement element, CancellationToken token) => RotateToAsync(element, 0.75, 500, token);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task StateContainer_DefaultAnimation_Timeout()
	{
		layout.EnableAnimations();
		foreach (var child in layout.Children)
		{
			child.EnableAnimations();
		}

		var cancelledTokenSource = new CancellationTokenSource(TimeSpan.FromMicroseconds(1));
		await WaitForCancellationAsync(cancelledTokenSource.Token, TestContext.Current.CancellationToken);
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, cancelledTokenSource.Token));
	}

	[Fact(Timeout = (int)TestDuration.Long)]
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
		await WaitForCancellationAsync(cancelledTokenSource.Token, TestContext.Current.CancellationToken);
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, beforeStateChangeAnimation, afterStateChangeAnimation, cancelledTokenSource.Token));
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task StateContainer_FuncAnimation_Timeout()
	{
		layout.EnableAnimations();
		foreach (var child in layout.Children)
		{
			child.EnableAnimations();
		}

		var cancelledTokenSource = new CancellationTokenSource(TimeSpan.FromMicroseconds(1));
		await WaitForCancellationAsync(cancelledTokenSource.Token, TestContext.Current.CancellationToken);
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, null, CustomAnimation, cancelledTokenSource.Token));

		static Task CustomAnimation(VisualElement element, CancellationToken token) => RotateToAsync(element, 0.75, 1000, token);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task StateContainer_ChangingStateWhenCanStateChangePropertyIsFalse_DefaultAnimation()
	{
		layout.EnableAnimations();
		foreach (var child in layout.Children)
		{
			child.EnableAnimations();
		}

		var changeStateWithAnimationTask = StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, TestContext.Current.CancellationToken);

		Assert.False(StateContainer.GetCanStateChange(layout));
		var exception = Assert.Throws<StateContainerException>(() => StateContainer.SetCurrentState(layout, StateKey.Anything));
		var exception2 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, TestContext.Current.CancellationToken));
		var exception3 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, [], null, TestContext.Current.CancellationToken));
		var exception4 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, FadeToAsync, null, TestContext.Current.CancellationToken));

		await changeStateWithAnimationTask;

		Assert.Equal("CanStateChange is false. CurrentState cannot be changed while a state change is in progress. To avoid this exception, first verify CanStateChange is True before changing CurrentState.", exception.Message);
		Assert.Equal(exception.Message, exception2.Message);
		Assert.Equal(exception.Message, exception3.Message);
		Assert.Equal(exception.Message, exception4.Message);

		Assert.True(StateContainer.GetCanStateChange(layout));
	}

	[Fact(Timeout = (int)TestDuration.Long)]
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

		var changeStateWithAnimationTask = StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, beforeStateChangeAnimation, null, TestContext.Current.CancellationToken);

		Assert.False(StateContainer.GetCanStateChange(layout));
		var exception = Assert.Throws<StateContainerException>(() => StateContainer.SetCurrentState(layout, StateKey.Anything));
		var exception2 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, TestContext.Current.CancellationToken));
		var exception3 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, [], null, TestContext.Current.CancellationToken));
		var exception4 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, FadeToAsync, null, TestContext.Current.CancellationToken));

		await changeStateWithAnimationTask;

		Assert.Equal("CanStateChange is false. CurrentState cannot be changed while a state change is in progress. To avoid this exception, first verify CanStateChange is True before changing CurrentState.", exception.Message);
		Assert.Equal(exception.Message, exception2.Message);
		Assert.Equal(exception.Message, exception3.Message);
		Assert.Equal(exception.Message, exception4.Message);

		Assert.True(StateContainer.GetCanStateChange(layout));
	}

	[Fact(Timeout = (int)TestDuration.Long)]
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

		var changeStateWithAnimationTask = StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, null, afterStateChangeAnimation, TestContext.Current.CancellationToken);

		Assert.False(StateContainer.GetCanStateChange(layout));
		var exception = Assert.Throws<StateContainerException>(() => StateContainer.SetCurrentState(layout, StateKey.Anything));
		var exception2 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, TestContext.Current.CancellationToken));
		var exception3 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, [], null, TestContext.Current.CancellationToken));
		var exception4 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, FadeToAsync, null, TestContext.Current.CancellationToken));

		await changeStateWithAnimationTask;

		Assert.Equal("CanStateChange is false. CurrentState cannot be changed while a state change is in progress. To avoid this exception, first verify CanStateChange is True before changing CurrentState.", exception.Message);
		Assert.Equal(exception.Message, exception2.Message);
		Assert.Equal(exception.Message, exception3.Message);
		Assert.Equal(exception.Message, exception4.Message);

		Assert.True(StateContainer.GetCanStateChange(layout));
	}

	[Fact(Timeout = (int)TestDuration.Long)]
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

		var changeStateWithAnimationTask = StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, beforeStateChangeAnimation, afterStateChangeAnimation, TestContext.Current.CancellationToken);

		Assert.False(StateContainer.GetCanStateChange(layout));
		var exception = Assert.Throws<StateContainerException>(() => StateContainer.SetCurrentState(layout, StateKey.Anything));
		var exception2 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, TestContext.Current.CancellationToken));
		var exception3 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, [], null, TestContext.Current.CancellationToken));
		var exception4 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, FadeToAsync, null, TestContext.Current.CancellationToken));

		await changeStateWithAnimationTask;

		Assert.Equal("CanStateChange is false. CurrentState cannot be changed while a state change is in progress. To avoid this exception, first verify CanStateChange is True before changing CurrentState.", exception.Message);
		Assert.Equal(exception.Message, exception2.Message);
		Assert.Equal(exception.Message, exception3.Message);
		Assert.Equal(exception.Message, exception4.Message);

		Assert.True(StateContainer.GetCanStateChange(layout));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task StateContainer_ChangingStateWhenCanStateChangePropertyIsFalse_NullAnimations()
	{
		var exception = await Assert.ThrowsAsync<ArgumentException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, (Animation?)null, null, TestContext.Current.CancellationToken));

		Assert.Equal("Animation required. Parameters beforeStateChange and afterStateChange cannot both be null", exception.Message);
		Assert.True(StateContainer.GetCanStateChange(layout));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task StateContainer_ChangingStateWhenCanStateChangePropertyIsFalse_NullFuncs()
	{
		var exception = await Assert.ThrowsAsync<ArgumentException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, (Func<VisualElement, CancellationToken, Task>?)null, null, TestContext.Current.CancellationToken));

		Assert.Equal("Animation required. Parameters beforeStateChange and afterStateChange cannot both be null", exception.Message);
		Assert.True(StateContainer.GetCanStateChange(layout));
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task StateContainer_ChangingStateWhenCanStateChangePropertyIsFalse_CustomBeforeAnimationFuncs()
	{
		layout.EnableAnimations();
		foreach (var child in layout.Children)
		{
			child.EnableAnimations();
		}

		var changeStateWithAnimationTask = StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, CustomAnimation, null, TestContext.Current.CancellationToken);

		Assert.False(StateContainer.GetCanStateChange(layout));
		var exception = Assert.Throws<StateContainerException>(() => StateContainer.SetCurrentState(layout, StateKey.Anything));
		var exception2 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, TestContext.Current.CancellationToken));
		var exception3 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, [], null, TestContext.Current.CancellationToken));
		var exception4 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, FadeToAsync, null, TestContext.Current.CancellationToken));

		await changeStateWithAnimationTask;

		Assert.Equal("CanStateChange is false. CurrentState cannot be changed while a state change is in progress. To avoid this exception, first verify CanStateChange is True before changing CurrentState.", exception.Message);
		Assert.Equal(exception.Message, exception2.Message);
		Assert.Equal(exception.Message, exception3.Message);
		Assert.Equal(exception.Message, exception4.Message);

		Assert.True(StateContainer.GetCanStateChange(layout));

		static Task CustomAnimation(VisualElement element, CancellationToken token) => RotateToAsync(element, 0.75, 500, token);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task StateContainer_ChangingStateWhenCanStateChangePropertyIsFalse_CustomAfterAnimationFuncs()
	{
		layout.EnableAnimations();
		foreach (var child in layout.Children)
		{
			child.EnableAnimations();
		}

		var changeStateWithAnimationTask = StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, null, CustomAnimation, TestContext.Current.CancellationToken);

		Assert.False(StateContainer.GetCanStateChange(layout));
		var exception = Assert.Throws<StateContainerException>(() => StateContainer.SetCurrentState(layout, StateKey.Anything));
		var exception2 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, TestContext.Current.CancellationToken));
		var exception3 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, [], null, TestContext.Current.CancellationToken));
		var exception4 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, FadeToAsync, null, TestContext.Current.CancellationToken));

		await changeStateWithAnimationTask;

		Assert.Equal("CanStateChange is false. CurrentState cannot be changed while a state change is in progress. To avoid this exception, first verify CanStateChange is True before changing CurrentState.", exception.Message);
		Assert.Equal(exception.Message, exception2.Message);
		Assert.Equal(exception.Message, exception3.Message);
		Assert.Equal(exception.Message, exception4.Message);

		Assert.True(StateContainer.GetCanStateChange(layout));

		static Task CustomAnimation(VisualElement element, CancellationToken token) => RotateToAsync(element, 0.75, 500, token);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task StateContainer_ChangingStateWhenCanStateChangePropertyIsFalse_CustomBeforeAndAfterAnimationFuncs()
	{
		layout.EnableAnimations();
		foreach (var child in layout.Children)
		{
			child.EnableAnimations();
		}

		var changeStateWithAnimationTask = StateContainer.ChangeStateWithAnimation(layout, StateKey.Error, CustomAnimation, CustomAnimation, TestContext.Current.CancellationToken);

		Assert.False(StateContainer.GetCanStateChange(layout));
		var exception = Assert.Throws<StateContainerException>(() => StateContainer.SetCurrentState(layout, StateKey.Anything));
		var exception2 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, TestContext.Current.CancellationToken));
		var exception3 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, [], null, TestContext.Current.CancellationToken));
		var exception4 = await Assert.ThrowsAsync<StateContainerException>(() => StateContainer.ChangeStateWithAnimation(layout, StateKey.Anything, FadeToAsync, null, TestContext.Current.CancellationToken));

		await changeStateWithAnimationTask;

		Assert.Equal("CanStateChange is false. CurrentState cannot be changed while a state change is in progress. To avoid this exception, first verify CanStateChange is True before changing CurrentState.", exception.Message);
		Assert.Equal(exception.Message, exception2.Message);
		Assert.Equal(exception.Message, exception3.Message);
		Assert.Equal(exception.Message, exception4.Message);

		Assert.True(StateContainer.GetCanStateChange(layout));

		static Task CustomAnimation(VisualElement element, CancellationToken token) => RotateToAsync(element, 0.75, 500, token);
	}

	[Fact]
	public void StateContainer_ElementNotInheritsLayoutThrowsException()
	{
		var invalidElement = new MockView();

		var exception = Assert.Throws<StateContainerException>(() => StateContainer.SetCurrentState(invalidElement, "abc"));

		exception.Message.Should().StartWith("Cannot create the StateContainerController.");
	}

	[Fact]
	public void StateContainer_CreatesControllerWithLayout()
	{
		var containerController = StateContainer.GetLayoutController(layout);

		Assert.NotNull(containerController);
		Assert.IsType<VerticalStackLayout>(containerController.GetLayout());
	}

	[Fact]
	public void Controller_ThrowsStateContainerExceptionInvalidStateKey()
	{
		var exception = Assert.Throws<StateContainerException>(() => controller.SwitchToState("InvalidStateKey"));

		Assert.Equal("StateView for InvalidStateKey not defined.", exception.Message);
		Assert.Null(exception.InnerException);
	}

	[Fact]
	public void Controller_ThrowsStateContainerExceptionOnDuplicateStateKey()
	{
		// Arrange
		var stackLayout = new StackLayout();
		var label = new Label();
		var button = new Button();

		StateView.SetStateKey(label, StateKey.Anything);
		StateView.SetStateKey(button, StateKey.Anything);
		StateContainer.SetStateViews(stackLayout, [label, button]);

		// Assert
		var exception = Assert.Throws<StateContainerException>(() => StateContainer.SetCurrentState(stackLayout, StateKey.Anything));
		Assert.IsType<InvalidOperationException>(exception.InnerException);
		exception.Message.Should().Contain("multiple");
		exception.InnerException.Message.Should().Contain("Sequence contains more than one matching element");
	}

	[Fact]
	public void Controller_SwitchesToStateFromContentSuccess()
	{
		controller.SwitchToState(StateKey.Loading);
		var state = Assert.IsType<Label>(controller.GetLayout().Children[0]);

		Assert.Equal("Loading", state.Text);
	}

	[Fact]
	public void Controller_SwitchesToContentFromStateSuccess()
	{
		controller.SwitchToState(StateKey.Loading);
		var label = Assert.IsType<Label>(controller.GetLayout().Children[0]);

		Assert.Equal("Loading", label.Text);

		controller.SwitchToContent();
		label = Assert.IsType<Label>(controller.GetLayout().Children[0]);

		Assert.Equal("Default", label.Text);
	}

	[Fact]
	public void Controller_SwitchesToContentFromContentSuccess()
	{
		controller.SwitchToContent();
		var label = Assert.IsType<Label>(Assert.Single(controller.GetLayout().Children));

		Assert.Equal("Default", label.Text);
	}

	[Fact]
	public void Controller_SwitchesToStateFromStateSuccess()
	{
		controller.SwitchToState(StateKey.Anything);
		var label = Assert.IsType<Label>(controller.GetLayout().Children[0]);

		Assert.Equal("Anything", label.Text);

		controller.SwitchToState(StateKey.Loading);
		label = Assert.IsType<Label>(controller.GetLayout().Children[0]);

		Assert.Equal("Loading", label.Text);
	}

	[Fact]
	public void Controller_SwitchesToStateFromSameStateSuccess()
	{
		controller.SwitchToState(StateKey.Loading);
		var label = Assert.IsType<Label>(controller.GetLayout().Children[0]);

		Assert.Equal("Loading", label.Text);

		controller.SwitchToState(StateKey.Loading);
		label = Assert.IsType<Label>(controller.GetLayout().Children[0]);

		Assert.Equal("Loading", label.Text);
	}

	[Fact]
	public void Controller_GridStateViewSpansParent()
	{
		gridController.SwitchToState(StateKey.Loading);
		var view = (View)gridController.GetLayout().Children[0];

		Assert.Equal(Grid.GetColumnSpan(view), grid.ColumnDefinitions.Count);
		Assert.Equal(Grid.GetRowSpan(view), grid.RowDefinitions.Count);
	}

	[Fact]
	public void EnsureDefaults()
	{
		// Arrange
		var stackLayout = new StackLayout();

		// Act Assert
		Assert.Equal([], StateContainer.GetStateViews(stackLayout));
		Assert.Empty(StateContainer.GetStateViews(stackLayout));
		Assert.Equal(StateContainerDefaults.CurrentState, StateContainer.GetCurrentState(stackLayout));
		Assert.Equal(StateContainerDefaults.CanStateChange, StateContainer.GetCanStateChange(stackLayout));
		Assert.Equal(StateViewDefaults.StateKey, StateView.GetStateKey(stackLayout));
	}

	[Fact]
	public void EnsureLayoutControllerIsUniquePerLayout()
	{
		// Arrange
		var grid1 = new Grid();
		var grid2 = new Grid();

		// Act
		var grid1StateViews = StateContainer.GetStateViews(grid1);
		var grid2StateViews = StateContainer.GetStateViews(grid2);
		grid1StateViews.Add(new Label
		{
			Text = "Test",
		});

		// Assert
		Assert.NotSame(grid1StateViews, grid2StateViews);
		Assert.Single(grid1StateViews);
		Assert.Empty(grid2StateViews);
	}

	static Task FadeToAsync(VisualElement element, CancellationToken token)
	{
#if NET11_0_OR_GREATER
		return element.FadeToAsync(1, 250, null, token);
#else
		return element.FadeToAsync(1).WaitAsync(token);
#endif
	}

	static Task RotateToAsync(VisualElement element, double rotation, uint length, CancellationToken token)
	{
#if NET11_0_OR_GREATER
		return element.RotateToAsync(rotation, length, null, token);
#else
		return element.RotateToAsync(rotation, length).WaitAsync(token);
#endif
	}

	static class StateKey
	{
		public const string Loading = "LoadingStateKey";
		public const string Error = "ErrorStateKey";
		public const string Anything = "AnythingStateKey";
	}

	sealed class ViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler? PropertyChanged;

		public bool CanChangeState
		{
			get;
			set
			{
				if (value != field)
				{
					field = value;
					OnPropertyChanged();
					ChangeStateCommand.ChangeCanExecute();
				}
			}
		}

		[field: AllowNull, MaybeNull]
		Command ChangeStateCommand => field ??= new Command(() => Trace.WriteLine("Command Tapped"), () => CanChangeState);

		void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}