using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Nito.AsyncEx;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class AnimationBehaviorTests() : BaseBehaviorTest<AnimationBehavior, VisualElement>(new AnimationBehavior(), new MockView())
{
	[Fact]
	public void TapGestureRecognizerAttachedWhenAnimateOnTapSetToTrue()
	{
		var boxView = new BoxView();
		boxView.Behaviors.Add(new AnimationBehavior() { AnimateOnTap = true });
		var gestureRecognizers = boxView.GestureRecognizers.ToList();

		gestureRecognizers.Should().HaveCount(1).And.AllBeOfType<TapGestureRecognizer>();
	}

	[Fact]
	public void TapGestureRecognizerAttachedEvenWithAnotherAlreadyAttached()
	{
		var boxView = new BoxView();
		boxView.GestureRecognizers.Add(new TapGestureRecognizer());
		boxView.Behaviors.Add(new AnimationBehavior() { AnimateOnTap = true });
		var gestureRecognizers = boxView.GestureRecognizers.ToList();

		gestureRecognizers.Should().HaveCount(2).And.AllBeOfType<TapGestureRecognizer>();
	}

	[Fact]
	public void TapGestureRecognizerNotAttachedWhenAnimateOnTapSetToFalse()
	{
		var boxView = new BoxView();
		boxView.Behaviors.Add(new AnimationBehavior
		{
			EventName = nameof(BoxView.Focused),
		});
		var gestureRecognizers = boxView.GestureRecognizers.ToList();

		gestureRecognizers.Should().BeEmpty();
	}

	[Fact]
	public void TapGestureRecognizerAddedAndRemovedDynamically()
	{
		var behavior = new AnimationBehavior() { AnimateOnTap = false };

		var boxView = new BoxView();
		boxView.Behaviors.Add(behavior);
		var gestureRecognizers = boxView.GestureRecognizers.ToList();

		gestureRecognizers.Should().BeEmpty();

		behavior.AnimateOnTap = true;

		gestureRecognizers = boxView.GestureRecognizers.ToList();
		gestureRecognizers.Should().HaveCount(1).And.AllBeOfType<TapGestureRecognizer>();

		behavior.AnimateOnTap = false;

		gestureRecognizers = boxView.GestureRecognizers.ToList();
		gestureRecognizers.Should().BeEmpty();
	}

	[Fact]
	public void CorrectTapGestureRecognizerRemoved()
	{
		var behavior = new AnimationBehavior() { AnimateOnTap = true };
		var boxView = new BoxView();
		boxView.GestureRecognizers.Add(new TapGestureRecognizer() { AutomationId = "Test1" });
		boxView.Behaviors.Add(behavior);
		boxView.GestureRecognizers.Add(new TapGestureRecognizer() { AutomationId = "Test2" });

		var gestureRecognizers = boxView.GestureRecognizers.ToList();
		gestureRecognizers.Should().HaveCount(3).And.AllBeOfType<TapGestureRecognizer>();

		behavior.AnimateOnTap = false;

		gestureRecognizers = boxView.GestureRecognizers.ToList();
		gestureRecognizers.Should().HaveCount(2).And.AllBeOfType<TapGestureRecognizer>();
		gestureRecognizers.Select(g => ((TapGestureRecognizer)g).AutomationId).Should().BeEquivalentTo("Test1", "Test2");
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task AnimateCommandStartsAnimation()
	{
		bool animationStarted = false, animationEnded = false;

		var animationStartedTcs = new TaskCompletionSource();
		var animationEndedTcs = new TaskCompletionSource();

		var mockAnimation = new MockAnimation();
		mockAnimation.AnimationStarted += HandleAnimationStarted;
		mockAnimation.AnimationEnded += HandleAnimationEnded;

		var behavior = new AnimationBehavior
		{
			AnimationType = mockAnimation
		};

		new Label
		{
			Behaviors = { behavior }
		}.EnableAnimations();

		behavior.AnimateCommand.Execute(TestContext.Current.CancellationToken);

		await animationStartedTcs.Task.WaitAsync(TestContext.Current.CancellationToken);
		await animationEndedTcs.Task.WaitAsync(TestContext.Current.CancellationToken);

		animationEnded.Should().BeTrue();
		animationStarted.Should().BeTrue();
		mockAnimation.HasAnimated.Should().BeTrue();

		void HandleAnimationStarted(object? sender, EventArgs e)
		{
			mockAnimation.AnimationStarted -= HandleAnimationStarted;

			animationStarted = true;
			animationStartedTcs.SetResult();
		}

		void HandleAnimationEnded(object? sender, EventArgs e)
		{
			mockAnimation.AnimationEnded -= HandleAnimationEnded;

			animationEnded = true;
			animationEndedTcs.SetResult();
		}
	}

	[Fact]
	public void AnimateCommandWithACancelledToken_DoesNotThrowOrStartTheAnimation()
	{
		using var animationCommandCts = new CancellationTokenSource();
		var mockAnimation = new MockAnimation();
		var behavior = new AnimationBehavior
		{
			AnimationType = mockAnimation
		};

		new Label
		{
			Behaviors = { behavior }
		}.EnableAnimations();

		// AsyncContext rethrows an exception that escapes the fire-and-forget AnimateCommand, which would crash an app
		var exception = Record.Exception(() => AsyncContext.Run(async () =>
		{
			await animationCommandCts.CancelAsync();
			behavior.AnimateCommand.Execute(animationCommandCts.Token);
		}));

		Assert.Null(exception);
		Assert.False(mockAnimation.AnimationHasStarted);
		Assert.False(mockAnimation.HasAnimated);
	}

	[Fact]
	public void AnimateCommandTokenCanceledMidAnimation_DoesNotThrowAndReturnsTheView()
	{
		using var animationCommandCts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

		var behavior = new AnimationBehavior
		{
			AnimationType = new ShakeAnimation { Length = 2000 }
		};

		var label = new Label
		{
			Behaviors = { behavior }
		};
		label.EnableAnimations();

		var shaking = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		label.PropertyChanged += HandlePropertyChanged;

		// AsyncContext rethrows an exception that escapes the fire-and-forget AnimateCommand, which would crash an app
		var exception = Record.Exception(() => AsyncContext.Run(async () =>
		{
			behavior.AnimateCommand.Execute(animationCommandCts.Token);
			await shaking.Task.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);
			animationCommandCts.Cancel();
		}));

		Assert.Null(exception);
		label.TranslationX.Should().Be(0);

		void HandlePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(VisualElement.TranslationX))
			{
				label.PropertyChanged -= HandlePropertyChanged;
				shaking.TrySetResult();
			}
		}
	}

	[Fact]
	public void AnimateCommandWithAnExpiredToken_DoesNotThrowOrStartTheAnimation()
	{
		using var animationCommandCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));
		var mockAnimation = new MockAnimation();
		var behavior = new AnimationBehavior
		{
			AnimationType = mockAnimation
		};

		new Label
		{
			Behaviors = { behavior }
		}.EnableAnimations();

		Assert.True(SpinWait.SpinUntil(() => animationCommandCts.IsCancellationRequested, TimeSpan.FromMilliseconds((int)TestDuration.Short)));

		// AsyncContext rethrows an exception that escapes the fire-and-forget AnimateCommand, which would crash an app
		var exception = Record.Exception(() => AsyncContext.Run(() => behavior.AnimateCommand.Execute(animationCommandCts.Token)));

		Assert.Null(exception);
		Assert.False(mockAnimation.AnimationHasStarted);
		Assert.False(mockAnimation.HasAnimated);
	}

	[Fact]
	public void AnimateCommand_OperationCanceledExceptionNotCausedByItsToken_StillReachesTheApp()
	{
		var behavior = new AnimationBehavior
		{
			AnimationType = new CancelsItselfAnimation()
		};

		new Label
		{
			Behaviors = { behavior }
		}.EnableAnimations();

		// AsyncContext rethrows an exception that escapes the fire-and-forget AnimateCommand, which would crash an app
		var exception = Record.Exception(() => AsyncContext.Run(() => behavior.AnimateCommand.Execute(TestContext.Current.CancellationToken)));

		Assert.IsType<OperationCanceledException>(exception, exactMatch: false);
	}

	sealed partial class CancelsItselfAnimation : BaseAnimation
	{
		public override Task Animate(VisualElement view, CancellationToken token = default) => Task.FromCanceled(new CancellationToken(canceled: true));
	}

	class MockAnimation : BaseAnimation
	{
		public event EventHandler? AnimationStarted;

		public event EventHandler? AnimationEnded;

		public bool AnimationHasStarted { get; private set; }

		public bool HasAnimated { get; private set; }

		public override async Task Animate(VisualElement element, CancellationToken token)
		{
			ArgumentNullException.ThrowIfNull(element);

			AnimationHasStarted = true;
			AnimationStarted?.Invoke(this, EventArgs.Empty);

			await element.RotateToAsync(70, 250, null, token);

			HasAnimated = true;

			AnimationEnded?.Invoke(this, EventArgs.Empty);
		}
	}
}