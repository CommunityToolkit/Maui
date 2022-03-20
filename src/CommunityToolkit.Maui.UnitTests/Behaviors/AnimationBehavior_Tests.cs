using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class AnimationBehavior_Tests : BaseTest
{
	[Fact]
	public void TabGestureRecognizerAttachedWhenNoEventSpecified()
	{
		var boxView = new BoxView();
		boxView.Behaviors.Add(new AnimationBehavior());
		var gestureRecognizers = boxView.GestureRecognizers.ToList();

		gestureRecognizers.Should().HaveCount(1).And.AllBeOfType<TapGestureRecognizer>();
	}

	[Fact]
	public void TabGestureRecognizerNotAttachedWhenEventSpecified()
	{
		var addBehavior = () => new BoxView().Behaviors.Add(new AnimationBehavior()
		{
			EventName = nameof(BoxView.Focused),
		});

		addBehavior.Should().Throw<InvalidOperationException>();
	}

	[Fact]
	public void TabGestureRecognizerNotAttachedWhenViewIsInputView()
	{
		var addBehavior = () => new Entry().Behaviors.Add(new AnimationBehavior());
		addBehavior.Should().Throw<InvalidOperationException>();
	}

	[Fact]
	public async Task AnimateCommandStartsAnimation()
	{
		bool animationStarted = false, animationEnded = false;

		var animationStartedTCS = new TaskCompletionSource();
		var animationEndedTCS = new TaskCompletionSource();

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

		behavior.AnimateCommand.Execute(null);

		await animationStartedTCS.Task;
		await animationEndedTCS.Task;

		animationEnded.Should().BeTrue();
		animationStarted.Should().BeTrue();
		mockAnimation.HasAnimated.Should().BeTrue();

		void HandleAnimationStarted(object? sender, EventArgs e)
		{
			mockAnimation.AnimationStarted -= HandleAnimationStarted;

			animationStarted = true;
			animationStartedTCS.SetResult();
		}

		void HandleAnimationEnded(object? sender, EventArgs e)
		{
			mockAnimation.AnimationEnded -= HandleAnimationEnded;

			animationEnded = true;
			animationEndedTCS.SetResult();
		}
	}

	class MockAnimation : BaseAnimation
	{
		public bool HasAnimated { get; private set; }

		public event EventHandler? AnimationStarted;
		public event EventHandler? AnimationEnded;

		public override async Task Animate(VisualElement element)
		{
			ArgumentNullException.ThrowIfNull(element);

			AnimationStarted?.Invoke(this, EventArgs.Empty);

			await element.RotateTo(70);

			HasAnimated = true;

			AnimationEnded?.Invoke(this, EventArgs.Empty);
		}
	}
}