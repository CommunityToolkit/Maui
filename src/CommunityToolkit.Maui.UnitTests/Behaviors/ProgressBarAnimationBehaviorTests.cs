using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class ProgressBarAnimationBehaviorTests() : BaseBehaviorTest<ProgressBarAnimationBehavior, ProgressBar>(new ProgressBarAnimationBehavior(), new ProgressBar())
{
	public static readonly TheoryData<double, uint, Easing> ValidData = new()
	{
		{
			1, 500, Easing.Default
		},
		{
			0, 750, Easing.CubicOut
		}
	};

	[Theory(Timeout = 5000)]
	[MemberData(nameof(ValidData))]
	public async Task ValidPropertiesTests(double progress, uint length, Easing easing)
	{
		var progressBar = new ProgressBar();
		progressBar.EnableAnimations();

		var progressBarAnimationCompletedTcs = new TaskCompletionSource();

		var progressBarAnimationBehavior = new ProgressBarAnimationBehavior();
		progressBarAnimationBehavior.AnimationCompleted += HandleAnimationCompleted;
		progressBar.Behaviors.Add(progressBarAnimationBehavior);

		Assert.Equal(0.0d, progressBar.Progress);
		Assert.Equal((uint)500, progressBarAnimationBehavior.Length);
		Assert.Equal(Easing.Linear, progressBarAnimationBehavior.Easing);

		progressBarAnimationBehavior.Length = length;
		progressBarAnimationBehavior.Easing = easing;
		progressBarAnimationBehavior.Progress = progress;

		if (progressBar.Progress != progress)
		{
			await progressBarAnimationCompletedTcs.Task;
		}

		Assert.Equal(progress, progressBar.Progress);
		Assert.Equal(progress, progressBarAnimationBehavior.Progress);
		Assert.Equal(length, progressBarAnimationBehavior.Length);
		Assert.Equal(easing, progressBarAnimationBehavior.Easing);

		void HandleAnimationCompleted(object? sender, EventArgs e)
		{
			ArgumentNullException.ThrowIfNull(sender);

			progressBarAnimationBehavior.AnimationCompleted -= HandleAnimationCompleted;
			progressBarAnimationCompletedTcs.SetResult();
		}
	}

	[Theory]
	[InlineData(double.MinValue)]
	[InlineData(-1)]
	[InlineData(-0.0000000000001)]
	[InlineData(1.0000000000001)]
	[InlineData(double.MaxValue)]
	public void InvalidProgressValuesTest(double inputProgressValue)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() =>
		{
			new ProgressBarAnimationBehavior
			{
				Progress = inputProgressValue
			};
		});
	}

	[Fact]
	public void AttachedToInvalidElementTest()
	{
		IReadOnlyList<VisualElement> invalidVisualElements =
		[
			new Button(),
			new Border(),
			new Label(),
			new VisualElement(),
			new View(),
			new Entry(),
		];

		foreach (var invalidVisualElement in invalidVisualElements)
		{
			Assert.Throws<InvalidOperationException>(() => invalidVisualElement.Behaviors.Add(new ProgressBarAnimationBehavior()));
		}
	}

	[Fact]
	public void AttachedToRemovedFromValidElementTest()
	{
		var progressBar = new ProgressBar();
		var customProgressBar = new CustomProgressBar();

		var progressBarAnimationBehaviorProgressBar = new ProgressBarAnimationBehavior();
		var progressBarAnimationBehaviorCustomProgressBar = new ProgressBarAnimationBehavior();

		progressBar.Invoking(x => x.Behaviors.Add(progressBarAnimationBehaviorProgressBar)).Should().NotThrow<InvalidOperationException>();
		customProgressBar.Invoking(x => x.Behaviors.Add(progressBarAnimationBehaviorCustomProgressBar)).Should().NotThrow<InvalidOperationException>();

		Assert.Single(progressBar.Behaviors.OfType<ProgressBarAnimationBehavior>());
		Assert.Single(customProgressBar.Behaviors.OfType<ProgressBarAnimationBehavior>());

		progressBar.Invoking(x => x.Behaviors.Remove(progressBarAnimationBehaviorProgressBar)).Should().NotThrow<InvalidOperationException>();
		customProgressBar.Invoking(x => x.Behaviors.Remove(progressBarAnimationBehaviorCustomProgressBar)).Should().NotThrow<InvalidOperationException>();

		Assert.Empty(progressBar.Behaviors);
		Assert.Empty(customProgressBar.Behaviors);
	}

	class CustomProgressBar : ProgressBar
	{

	}
}