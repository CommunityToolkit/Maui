using System.ComponentModel;
using System.Text;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class ExpanderTests : BaseViewTest
{
	readonly Maui.Views.Expander expander = new();

	[Fact]
	public void ExpanderShouldBeAssignedToIExpander()
	{
		expander.Should().BeAssignableTo<IExpander>();
	}

	[Fact]
	public void CheckDefaultValues()
	{
		Assert.Equal(ExpandDirection.Down, expander.Direction);
		Assert.False(expander.IsExpanded);
		Assert.Null(expander.Content);
		Assert.Null(expander.Header);
		Assert.Null(expander.Command);
		Assert.Null(expander.CommandParameter);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ExpandedChangedIsExpandedPassedWithEvent(bool expectedIsExpanded)
	{
		bool? isExpanded = null;
		var action = new EventHandler<ExpandedChangedEventArgs>((_, e) => isExpanded = e.IsExpanded);
		expander.ExpandedChanged += action;
		((IExpander)expander).ExpandedChanged(expectedIsExpanded);
		expander.ExpandedChanged -= action;

		isExpanded.Should().Be(expectedIsExpanded);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ExpandedChangedCommandExecutedWithParams(bool expectedIsExpanded)
	{
		bool? isExpanded = null;

		expander.Command = new Command<bool>(parameter => isExpanded = parameter);
		expander.CommandParameter = expectedIsExpanded;
		((IExpander)expander).ExpandedChanged(expectedIsExpanded);

		isExpanded.Should().Be(expectedIsExpanded);
	}

	[Theory]
	[InlineData((ExpandDirection)(-1))]
	[InlineData((ExpandDirection)2)]
	public void ExpanderDirectionThrowsInvalidEnumArgumentException(ExpandDirection direction)
	{
		Assert.Throws<InvalidEnumArgumentException>(() => expander.Direction = direction);
	}

	[Fact]
	public void EnsureExpandedChanged()
	{
		var isExpanded_Initial = expander.IsExpanded;

		var header = new View();
		expander.Header = header;

		expander.HeaderTapGestureRecognizer.SendTapped(header);
		var isExpanded_Final = expander.IsExpanded;

		Assert.True(isExpanded_Final);
		Assert.False(isExpanded_Initial);
		Assert.NotEqual(isExpanded_Initial, isExpanded_Final);

		expander.HeaderTapGestureRecognizer.SendTapped(header);

		Assert.False(expander.IsExpanded);
	}

	[Fact]
	public void EnsureHandleHeaderTappedExecutesWhenHeaderTapped()
	{
		int handleHeaderTappedCount = 0;
		bool didHandleHeaderTappedExecute = false;

		var header = new View();
		expander.Header = new View();
		expander.HandleHeaderTapped = HandleHeaderTapped;

		expander.HeaderTapGestureRecognizer.SendTapped(header);

		Assert.True(didHandleHeaderTappedExecute);
		Assert.Equal(1, handleHeaderTappedCount);

		expander.HandleHeaderTapped = null;

		expander.HeaderTapGestureRecognizer.SendTapped(header);

		Assert.True(didHandleHeaderTappedExecute);
		Assert.Equal(1, handleHeaderTappedCount);

		void HandleHeaderTapped(TappedEventArgs tappedEventArgs)
		{
			handleHeaderTappedCount++;
			didHandleHeaderTappedExecute = true;
		}
	}

	[Fact]
	public void EnsureDefaults()
	{
		var expander = new Maui.Views.Expander();
		Assert.Equal(ExpanderDefaults.Direction, expander.Direction);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ExpanderRaisesEventsInCorrectOrderWhenExpanding()
	{
		TaskCompletionSource tcs = new();
		var expander = new Maui.Views.Expander();
		StringBuilder callOrder = new();
		expander.ExpandedChanging += (_, _) => callOrder.Append("changing,");
		expander.ExpandedChanged += (_, _) => { callOrder.Append("changed,"); tcs.TrySetResult(); };
		var controller = new MockExpansionController();
		controller.Expanding += (_, _) => callOrder.Append("controllerExpanding,");
		controller.Collapsing += (_, _) => callOrder.Append("controllerCollapsing,");
		expander.ExpansionController = controller;
		expander.Header = new Label { Text = "Header" };
		expander.Content = new Label { Text = "Hello" };
		expander.IsExpanded = true;
		await tcs.Task;
		Assert.Equal("changing,controllerExpanding,changed,", callOrder.ToString());
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ExpanderCallsCorrectControllerMethod()
	{
		TaskCompletionSource tcs = new();
		var expander = new Maui.Views.Expander();
		expander.ExpandedChanged += (_, _) => tcs.TrySetResult();
		var controller = new MockExpansionController();
		expander.ExpansionController = controller;
		expander.Header = new Label { Text = "Header" };
		expander.Content = new Label { Text = "Hello" };
		expander.IsExpanded = true;
		await tcs.Task;
		Assert.Equal(1, controller.ExpandingCount);
		Assert.Equal(0, controller.CollapsingCount);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ExpanderExpandsContentBecomesVisibleHeightRestored()
	{
		var tcs = new TaskCompletionSource();
		var expander = new Maui.Views.Expander();
		expander.ExpandedChanged += (_, _) => tcs.TrySetResult();
		expander.Header = new Label { Text = "Header" };
		expander.Content = new Label { Text = "Hello" };
		expander.IsExpanded = true;
		await tcs.Task;
		var element = expander.Content as VisualElement;
		Assert.NotNull(expander.ContentHost);
		Assert.NotNull(expander.Content);
		Assert.NotNull(element);
		Assert.True(element.IsVisible);
		Assert.Equal(-1, expander.ContentHost.HeightRequest);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ExpanderCollapseContentHiddenHeightZero()
	{
		var expander = new Maui.Views.Expander();
		expander.IsExpanded = true;
		var tcs = new TaskCompletionSource();
		expander.ExpandedChanged += (_, _) => tcs.TrySetResult();
		expander.Header = new Label { Text = "Header" };
		expander.Content = new Label { Text = "Hello" };
		expander.IsExpanded = false;
		await tcs.Task;
		var element = expander.Content as VisualElement;
		Assert.NotNull(expander.ContentHost);
		Assert.NotNull(expander.Content);
		Assert.NotNull(element);
		Assert.False(element.IsVisible);
		Assert.Equal(0, expander.ContentHost.HeightRequest);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ExpanderIsExpandedSetBeforeContentAssignedInitialStateCorrect()
	{
		var expander = new Maui.Views.Expander();
		expander.IsExpanded = true;
		expander.Header = new Label { Text = "Header" };
		expander.Content = new Label { Text = "Hello" };
		var element = expander.Content as VisualElement;
		Assert.NotNull(expander.ContentHost);
		Assert.NotNull(element);
		Assert.True(element.IsVisible);
		Assert.Equal(-1, expander.ContentHost.HeightRequest);
	}

	[Fact]
	public void AttachingAnimationBehaviorSetsExpansionController()
	{
		var expander = new Maui.Views.Expander();
		var behavior = new ExpanderAnimationBehavior();
		expander.Behaviors.Add(behavior);
		Assert.IsType<ExpanderAnimationBehavior>(expander.Behaviors[0]);
		Assert.IsType<ExpanderAnimationBehavior>(behavior);
		Assert.IsType<ExpanderAnimationBehavior>(expander.ExpansionController);
		Assert.Same(behavior, expander.ExpansionController);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task OnExpandingAsyncAnimatesHeightCorrectly()
	{
		var tcs = new TaskCompletionSource();
		var expander = new Maui.Views.Expander();
		expander.ExpandedChanged += (_, _) => tcs.TrySetResult();
		var behavior = new ExpanderAnimationBehavior();
		expander.Behaviors.Add(behavior);
		expander.Header = new Label { Text = "Header" };
		expander.Content = new Label { Text = "Hello" };
		Assert.NotNull(expander.ContentHost);
		expander.ContentHost.HeightRequest = 0;
		expander.IsExpanded = true;
		await tcs.Task;
		var element = expander.Content as VisualElement;
		Assert.True(expander.IsExpanded);
		Assert.Equal(-1, expander.ContentHost.HeightRequest);
		Assert.NotNull(element);
		Assert.True(element.IsVisible);
	}

	[Fact]
	public async Task ExpanderContentHostIsUnsetWhenContentIsRemoved()
	{
		var expander = new Expander();
		expander.Content = new Label { Text = "Hello" };
		Assert.NotNull(expander.ContentHost);
		expander.Content = (IView)null!;
		Assert.Null(expander.ContentHost);
	}

	class MockExpansionController : IExpansionController
	{
		public event EventHandler? Expanding;
		public event EventHandler? Collapsing;

		public int ExpandingCount { get; private set; } = 0;
		public int CollapsingCount { get; private set; } = 0;

		public async Task OnExpandingAsync(Expander expander)
		{
			await Task.Yield();
			ExpandingCount++;
			Expanding?.Invoke(this, EventArgs.Empty);
		}

		public async Task OnCollapsingAsync(Expander expander)
		{
			await Task.Yield();
			CollapsingCount++;
			Collapsing?.Invoke(this, EventArgs.Empty);
		}
	}
}