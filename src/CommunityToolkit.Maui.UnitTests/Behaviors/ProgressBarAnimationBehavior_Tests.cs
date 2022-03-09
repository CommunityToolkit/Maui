﻿using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class ProgressBarAnimationBehavior_Tests : BaseTest
{
	public static readonly IReadOnlyList<object[]> ValidData = new[]
	{
		new object[] { 0.5, 175, Easing.BounceIn },
		new object[] { 1, 1500, Easing.Default },
		new object[] { 0, 750, Easing.CubicOut }
	};

	[Theory]
	[MemberData(nameof(ValidData))]
	public async Task ValidPropertiesTests(double progress, uint length, Easing easing)
	{
		var progressBar = new ProgressBar();
		progressBar.EnableAnimations();

		var progressBarAnimationCompletedTCS = new TaskCompletionSource();

		var progressBarAnimationBehavior = new ProgressBarAnimationBehavior();
		progressBarAnimationBehavior.AnimationCompleted += HandleAnimationComplted;
		progressBar.Behaviors.Add(progressBarAnimationBehavior);

		Assert.Equal(0.0d, progressBar.Progress);
		Assert.Equal(0.0d, ProgressBarAnimationBehavior.ProgressProperty.DefaultValue);
		Assert.Equal((uint)500, ProgressBarAnimationBehavior.LengthProperty.DefaultValue);
		Assert.Equal(Easing.Linear, ProgressBarAnimationBehavior.EasingProperty.DefaultValue);

		progressBarAnimationBehavior.Length = length;
		progressBarAnimationBehavior.Easing = easing;
		progressBarAnimationBehavior.Progress = progress;

		if (progressBar.Progress != progress)
		{
			await progressBarAnimationCompletedTCS.Task;
		}

		Assert.Equal(progress, progressBar.Progress);
		Assert.Equal(progress, progressBarAnimationBehavior.Progress);
		Assert.Equal(length, progressBarAnimationBehavior.Length);
		Assert.Equal(easing, progressBarAnimationBehavior.Easing);

		void HandleAnimationComplted(object? sender, EventArgs e)
		{
			ArgumentNullException.ThrowIfNull(sender);
			progressBarAnimationCompletedTCS.SetResult();
		}
	}

	[Theory]
	[InlineData(double.MinValue, 0)]
	[InlineData(-1, 0)]
	[InlineData(-0.0000000000001, 0)]
	[InlineData(1.0000000000001, 1)]
	[InlineData(double.MaxValue, 1)]
	public void InvalidProgressValuesTest(double inputProgressValue, double expectedProgressValue)
	{
		var progressBarAnimationBehavior = new ProgressBarAnimationBehavior
		{
			Progress = inputProgressValue
		};

		Assert.Equal(expectedProgressValue, progressBarAnimationBehavior.Progress);
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
	public void AttachedToRemovedFromValidElementTest()
	{
		var progressBar = new ProgressBar();
		var customProgressBar = new CustomProgressBar();

		var progressBarAnimationBehavior_ProgressBar = new ProgressBarAnimationBehavior();
		var progressBarAnimationBehavior_CustomProgressBar = new ProgressBarAnimationBehavior();

		progressBar.Invoking(x => x.Behaviors.Add(progressBarAnimationBehavior_ProgressBar)).Should().NotThrow<InvalidOperationException>();
		customProgressBar.Invoking(x => x.Behaviors.Add(progressBarAnimationBehavior_CustomProgressBar)).Should().NotThrow<InvalidOperationException>();

		Assert.Single(progressBar.Behaviors.OfType<ProgressBarAnimationBehavior>());
		Assert.Single(customProgressBar.Behaviors.OfType<ProgressBarAnimationBehavior>());

		progressBar.Invoking(x => x.Behaviors.Remove(progressBarAnimationBehavior_ProgressBar)).Should().NotThrow<InvalidOperationException>();
		customProgressBar.Invoking(x => x.Behaviors.Remove(progressBarAnimationBehavior_CustomProgressBar)).Should().NotThrow<InvalidOperationException>();

		Assert.Empty(progressBar.Behaviors);
		Assert.Empty(customProgressBar.Behaviors);
	}

	class CustomProgressBar : ProgressBar
	{

	}
}

