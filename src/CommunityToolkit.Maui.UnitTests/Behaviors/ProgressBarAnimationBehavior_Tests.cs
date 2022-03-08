using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests;

public class ProgressBarAnimationBehavior_Tests : BaseTest
{
	public static readonly IReadOnlyList<object[]> ValidData = new[]
	{
		new object[] { 0.5, 50, Easing.BounceIn },
		new object[] { 1, 1500, Easing.Default },
		new object[] { 0, 250, Easing.CubicOut }
	};

	[Theory]
	[MemberData(nameof(ValidData))]
	public async Task ValidPropertiesTests(double progress, uint length, Easing easing)
	{
		var progressBar = new ProgressBar();
		progressBar.EnableAnimations();

		var progressBarAnimationBehavior = new ProgressBarAnimationBehavior();
		progressBar.Behaviors.Add(progressBarAnimationBehavior);

		Assert.Equal(0.0d, progressBar.Progress);
		Assert.Equal(0.0d, ProgressBarAnimationBehavior.ProgressProperty.DefaultValue);
		Assert.Equal((uint)500, ProgressBarAnimationBehavior.LengthProperty.DefaultValue);
		Assert.Equal(Easing.Linear, ProgressBarAnimationBehavior.EasingProperty.DefaultValue);

		progressBarAnimationBehavior.Length = length;
		progressBarAnimationBehavior.Easing = easing;
		progressBarAnimationBehavior.Progress = progress;

		// Wait for ProgressTo animation to complete
		await Task.Delay(TimeSpan.FromMilliseconds(length * 1.25));

		Assert.Equal(progress, progressBar.Progress);
		Assert.Equal(progress, progressBarAnimationBehavior.Progress);
		Assert.Equal(length, progressBarAnimationBehavior.Length);
		Assert.Equal(easing, progressBarAnimationBehavior.Easing);
	}

	[Theory]
	[InlineData(double.MinValue)]
	[InlineData(-1)]
	[InlineData(-0.0000000000001)]
	[InlineData(1.0000000000001)]
	[InlineData(double.MaxValue)]
	public void InvalidProgressValuesTest(double progress)
	{
		var progressBarAnimationBehavior = new ProgressBarAnimationBehavior();

		Assert.Throws<ArgumentOutOfRangeException>(() => progressBarAnimationBehavior.Progress = progress);
	}

	[Fact]
	public void AttachedToInvalidElementTest()
	{
		IReadOnlyList<VisualElement> invalidVisualElements = new[]
		{
			new Button(),
			new Frame(),
			new Label(),
			new VisualElement(),
			new View(),
			new Entry(),
		};

		foreach (var invalidVisualElement in invalidVisualElements)
		{
			Assert.Throws<InvalidOperationException>(() => invalidVisualElement.Behaviors.Add(new ProgressBarAnimationBehavior()));
		}
	}

	[Fact]
	public void AttachedToValidElementTest()
	{
		var progressBar = new ProgressBar();
		var customProgressBar = new CustomProgressBar();

		progressBar.Invoking(x => x.Behaviors.Add(new ProgressBarAnimationBehavior())).Should().NotThrow<InvalidOperationException>();
		customProgressBar.Invoking(x => x.Behaviors.Add(new ProgressBarAnimationBehavior())).Should().NotThrow<InvalidOperationException>();
	}

	class CustomProgressBar : ProgressBar
	{

	}
}

