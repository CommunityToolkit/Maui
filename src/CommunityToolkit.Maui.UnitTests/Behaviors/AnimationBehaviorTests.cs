using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Nito.AsyncEx;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class AnimationBehaviorTests() : BaseBehaviorTest<AnimationBehavior, VisualElement>(new AnimationBehavior(), new View())
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

		await animationStartedTcs.Task;
		await animationEndedTcs.Task;

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
	public void AnimateCommandTokenCanceled()
	{
		OperationCanceledException? exception = null;
		var animationEndedTcs = new TaskCompletionSource();
		var animationCommandCts = new CancellationTokenSource();

		var mockAnimation = new MockAnimation();
		mockAnimation.AnimationEnded += HandleAnimationEnded;

		var behavior = new AnimationBehavior
		{
			AnimationType = mockAnimation
		};

		new Label
		{
			Behaviors = { behavior }
		}.EnableAnimations();

		try
		{
			// Run using AsyncContext to catch Exception thrown by fire-and-forget AnimateCommand (ICommand)
			AsyncContext.Run(async () =>
			{
				await animationCommandCts.CancelAsync();
				behavior.AnimateCommand.Execute(animationCommandCts.Token);
				await animationEndedTcs.Task;
			});
		}
		catch (OperationCanceledException e)
		{
			exception = e;
		}

		Assert.NotNull(exception);

		void HandleAnimationEnded(object? sender, EventArgs e)
		{
			mockAnimation.AnimationEnded -= HandleAnimationEnded;
			animationEndedTcs.SetResult();
		}
	}

	[Fact]
	public void AnimateCommandTokenExpired()
	{
		OperationCanceledException? exception = null;
		var animationEndedTcs = new TaskCompletionSource();
		var animationCommandCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		var mockAnimation = new MockAnimation();
		mockAnimation.AnimationEnded += HandleAnimationEnded;

		var behavior = new AnimationBehavior
		{
			AnimationType = mockAnimation
		};

		new Label
		{
			Behaviors = { behavior }
		}.EnableAnimations();

		try
		{
			// Run using AsyncContext to catch Exception thrown by fire-and-forget AnimateCommand (ICommand)
			AsyncContext.Run(async () =>
			{
				behavior.AnimateCommand.Execute(animationCommandCts.Token);
				await animationEndedTcs.Task;
			});
		}
		catch (OperationCanceledException e)
		{
			exception = e;
		}

		Assert.NotNull(exception);

		void HandleAnimationEnded(object? sender, EventArgs e)
		{
			mockAnimation.AnimationEnded -= HandleAnimationEnded;
			animationEndedTcs.SetResult();
		}
	}

	class MockAnimation : BaseAnimation
	{
		public bool HasAnimated { get; private set; }

		public event EventHandler? AnimationStarted;

		public event EventHandler? AnimationEnded;

		public override async Task Animate(VisualElement element, CancellationToken token)
		{
			ArgumentNullException.ThrowIfNull(element);

			AnimationStarted?.Invoke(this, EventArgs.Empty);

			await element.RotateToAsync(70).WaitAsync(token);

			HasAnimated = true;

			AnimationEnded?.Invoke(this, EventArgs.Empty);
		}
	}
}