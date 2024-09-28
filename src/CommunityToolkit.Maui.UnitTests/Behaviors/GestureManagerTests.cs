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
            var sender = new TouchBehavior { IsEnabled = true };

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
            var sender = new TouchBehavior { IsEnabled = true, CanExecute = canExecute };
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
            var sender = new TouchBehavior { IsEnabled = true, Element = new VisualElementMock() };
            var token = new CancellationTokenSource().Token;

            // Act
            var gestureManager = new GestureManager();
            await gestureManager.ChangeStateAsync(sender, false, token);

            // Assert
            Assert.Equal(TouchState.Default, sender.CurrentTouchState);
            Assert.Equal(HoverState.Default, sender.CurrentHoverState);
            Assert.Equal(TouchBehavior.UnpressedVisualState, sender.Element.VisualStateManager.CurrentState);
        }

        [Fact]
        public async Task HandleLongPress_ShouldRaiseLongPressCompleted()
        {
            // Arrange
            var sender = new TouchBehavior { IsEnabled = true, CanExecute = true, CurrentTouchState = TouchState.Pressed, LongPressDuration = 100 };
            var longPressCompletedRaised = false;
            sender.LongPressCompleted += (_, _) => longPressCompletedRaised = true;

            // Act
            var gestureManager = new GestureManager();
            var token = new CancellationTokenSource().Token;
            await gestureManager.HandleLongPress(sender, token);

            // Assert
            Assert.True(longPressCompletedRaised);
        }

        [Fact]
        public void Reset_ShouldResetDefaultBackgroundColor()
        {
            // Arrange
            var gestureManager = new GestureManager();
            gestureManager.defaultBackgroundColor = Colors.Red;

            // Act
            gestureManager.Reset();

            // Assert
            Assert.Null(gestureManager.defaultBackgroundColor);
        }

        [Fact]
        public async Task AbortAnimations_ShouldCancelAndDisposeAnimationTokenSource()
        {
            // Arrange
            var sender = new TouchBehavior { Element = new VisualElementMock() };
            var gestureManager = new GestureManager();
            gestureManager.animationTokenSource = new CancellationTokenSource();
            var token = new CancellationTokenSource().Token;

            // Act
            await gestureManager.AbortAnimations(sender, token);

            // Assert
            Assert.Null(gestureManager.animationTokenSource);
        }

        // Add more test methods for other public methods and internal methods as needed
    }

    // Mock class for VisualElement
    public class VisualElementMock : VisualElement, IVisualStateManager
    {
        public string CurrentState { get; private set; }

        public void GoToState(string state)
        {
            CurrentState = state;
        }
    }