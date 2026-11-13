using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Nito.AsyncEx;
using System.Diagnostics;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public abstract class BaseAnimationTests<TAnimation> : BaseTest where TAnimation : BaseAnimation, new()
{
	protected virtual TAnimation CreateAnimation() => new();

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task LengthShouldDictateFullAnimationLength() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateAnimation();

		Label label = new();
		label.EnableAnimations();

		var stopwatch = new Stopwatch();
		stopwatch.Start();
		await animation.Animate(label, token);
		stopwatch.Stop();

		double allowance = (animation.Length * 0.1) + 50;

		stopwatch.ElapsedMilliseconds.Should().BeCloseTo(animation.Length, (uint)allowance);
	}, TestContext.Current.CancellationToken);

	// Same why as FadeAnimationTests.RunOnSingleThreadAsync: off the test thread the mock ticker races MAUI's static animation bookkeeping and drops ticks.
	static Task RunOnSingleThreadAsync(Func<CancellationToken, Task> test, CancellationToken token) =>
		Task.Run(() => AsyncContext.Run(() => test(token)), token);
}
