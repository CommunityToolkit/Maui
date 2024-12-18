using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class GestureManagerTests : BaseTest
{
	[Fact]
	public void HandleUserInteraction_ShouldUpdateInteractionStatus()
	{
		// Arrange
		var touchBehavior = new TouchBehavior();
		var interactionStatus = TouchInteractionStatus.Completed;

		// Act
		GestureManager.HandleUserInteraction(in touchBehavior, in interactionStatus);

		// Assert
		Assert.Equal(interactionStatus, touchBehavior.CurrentInteractionStatus);
	}

	[Theory]
	[InlineData(HoverStatus.Entered, HoverState.Hovered)]
	[InlineData(HoverStatus.Exited, HoverState.Default)]
	public void HandleHover_ShouldUpdateHoverStateAndStatus(HoverStatus hoverStatus, HoverState expectedHoverState)
	{
		// Arrange
		var touchBehavior = new TouchBehavior
		{
			IsEnabled = true
		};

		// Act
		GestureManager.HandleHover(in touchBehavior, in hoverStatus);

		// Assert
		Assert.Equal(expectedHoverState, touchBehavior.CurrentHoverState);
		Assert.Equal(hoverStatus, touchBehavior.CurrentHoverStatus);
	}

	[Theory]
	[InlineData(TouchStatus.Started, true, TouchState.Pressed)]
	[InlineData(TouchStatus.Completed, true, TouchState.Default)]
	[InlineData(TouchStatus.Canceled, false, TouchState.Default)]
	public void HandleTouch_ShouldUpdateTouchStatusAndState(TouchStatus status, bool isEnabled, TouchState expectedTouchState)
	{
		// Arrange
		var touchBehavior = new TouchBehavior
		{
			IsEnabled = isEnabled,
			Element = new Label()
		};
		var tappedCompletedRaised = false;
		touchBehavior.TouchGestureCompleted += HandleTouchGestureCompleted;

		// Act
		var gestureManager = new GestureManager();
		GestureManager.HandleTouch(in touchBehavior, in status);

		// Assert
		Assert.Equal(expectedTouchState, touchBehavior.CurrentTouchState);
		Assert.Equal(status is TouchStatus.Completed && isEnabled, tappedCompletedRaised);

		void HandleTouchGestureCompleted(object? sender, TouchGestureCompletedEventArgs e)
		{
			touchBehavior.TouchGestureCompleted -= HandleTouchGestureCompleted;
			tappedCompletedRaised = true;
		}
	}

	[Fact]
	public async Task ChangeStateAsync_ShouldUpdateVisualState()
	{
		// Arrange
		var touchBehavior = new TouchBehavior
		{
			IsEnabled = true,
			Element = new Label()
		};
		var token = new CancellationTokenSource().Token;

		// Act
		var gestureManager = new GestureManager();
		await gestureManager.ChangeStateAsync(touchBehavior, false, token);

		// Assert
		Assert.Equal(TouchState.Default, touchBehavior.CurrentTouchState);
		Assert.Equal(HoverState.Default, touchBehavior.CurrentHoverState);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task HandleLongPress_ShouldRaiseLongPressCompleted()
	{
		// Arrange
		var touchBehavior = new TouchBehavior
		{
			IsEnabled = true,
			Element = new Button(),
			CurrentTouchState = TouchState.Pressed,
			CurrentInteractionStatus = TouchInteractionStatus.Started,
			LongPressDuration = 100,
			LongPressCommand = null
		};
		var longPressCompletedRaised = false;
		touchBehavior.LongPressCompleted += HandleLongPressCompleted;

		// Act
		var gestureManager = new GestureManager();
		var token = new CancellationTokenSource().Token;
		await gestureManager.HandleLongPress(touchBehavior, token);

		// Assert
		Assert.True(longPressCompletedRaised);


		void HandleLongPressCompleted(object? sender, LongPressCompletedEventArgs e)
		{
			touchBehavior.LongPressCompleted -= HandleLongPressCompleted;
			longPressCompletedRaised = true;
		}
	}
}