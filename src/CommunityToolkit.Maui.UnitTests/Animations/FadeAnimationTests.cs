using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public class FadeAnimationTests : BaseTest
{
	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task AnimateShouldThrowWithNullView()
	{
		FadeAnimation animation = new();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => animation.Animate(null, CancellationToken.None));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task CancellationTokenCanceled()
	{
		FadeAnimation animation = new();
		var cts = new CancellationTokenSource();

		var label = new Label
		{
			Opacity = 0.9
		};
		label.EnableAnimations();

		await Assert.ThrowsAsync<TaskCanceledException>(() =>
		{
			cts.Cancel();
			return animation.Animate(label, cts.Token);
		});
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task CancellationTokenExpired()
	{
		FadeAnimation animation = new();
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		var label = new Label
		{
			Opacity = 0.9
		};
		label.EnableAnimations();

		await Assert.ThrowsAsync<TaskCanceledException>(() => animation.Animate(label, cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task AnimateShouldReturnToOriginalOpacity()
	{
		FadeAnimation animation = new();

		var label = new Label
		{
			Opacity = 0.9
		};
		label.EnableAnimations();

		await animation.Animate(label, CancellationToken.None);

		label.Opacity.Should().Be(0.9);
	}
}