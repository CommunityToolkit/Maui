using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class GestureManagerTests
{
	[Fact]
	public void HandleUserInteraction_ShouldUpdateInteractionStatus()
	{
		// Arrange
		var sender = new TouchBehavior();
		var interactionStatus = TouchInteractionStatus.Completed;

		// Act
		GestureManager.HandleUserInteraction(in sender, in interactionStatus);

		// Assert
		Assert.Equal(interactionStatus, sender.CurrentInteractionStatus);
	}

	[Theory]
	[InlineData(HoverStatus.Entered, HoverState.Hovered)]
	[InlineData(HoverStatus.Exited, HoverState.Default)]
	public void HandleHover_ShouldUpdateHoverStateAndStatus(HoverStatus hoverStatus, HoverState expectedHoverState)
	{
		// Arrange
		var sender = new TouchBehavior
		{
			IsEnabled = true
		};

		// Act
		GestureManager.HandleHover(in sender, in hoverStatus);

		// Assert
		Assert.Equal(expectedHoverState, sender.CurrentHoverState);
		Assert.Equal(hoverStatus, sender.CurrentHoverStatus);
	}

	[Theory]
	[InlineData(TouchStatus.Started, true, TouchState.Pressed)]
	[InlineData(TouchStatus.Completed, true, TouchState.Default)]
	[InlineData(TouchStatus.Canceled, false, TouchState.Default)]
	public void HandleTouch_ShouldUpdateTouchStatusAndState(TouchStatus status, bool canExecute, TouchState expectedTouchState)
	{
		// Arrange
		var sender = new TouchBehavior
		{
			IsEnabled = canExecute
		};
		var tappedCompletedRaised = false;
		sender.TouchGestureCompleted += (_, _) => tappedCompletedRaised = true;

		// Act
		var gestureManager = new GestureManager();
		gestureManager.HandleTouch(in sender, in status);

		// Assert
		Assert.Equal(expectedTouchState, sender.CurrentTouchState);
		Assert.Equal(status == TouchStatus.Completed && canExecute, tappedCompletedRaised);
	}

	[Fact]
	public async Task ChangeStateAsync_ShouldUpdateVisualState()
	{
		// Arrange
		var sender = new TouchBehavior
		{
			IsEnabled = true,
			Element = new Button()
		};
		var token = new CancellationTokenSource().Token;

		// Act
		var gestureManager = new GestureManager();
		await gestureManager.ChangeStateAsync(sender, false, token);

		// Assert
		Assert.Equal(TouchState.Default, sender.CurrentTouchState);
		Assert.Equal(HoverState.Default, sender.CurrentHoverState);
	}

	[Fact]
	public async Task HandleLongPress_ShouldRaiseLongPressCompleted()
	{
		// Arrange
		var touchBehavior = new TouchBehavior
		{
			IsEnabled = true,
			Element = new Button(),
			CurrentTouchState = TouchState.Pressed,
			LongPressDuration = 100
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