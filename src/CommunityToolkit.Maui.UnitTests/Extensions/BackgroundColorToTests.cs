using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

public class BackgroundColorToTests : BaseTest
{
	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task BackgroundColorTo_VerifyColorChanged()
	{
		Color originalBackgroundColor = Colors.Blue, updatedBackgroundColor = Colors.Red;

		VisualElement element = new Label { BackgroundColor = originalBackgroundColor };
		element.EnableAnimations();

		Assert.Equal(originalBackgroundColor, element.BackgroundColor);

		var isSuccessful = await element.BackgroundColorTo(updatedBackgroundColor, token: TestContext.Current.CancellationToken);

		Assert.True(isSuccessful);
		Assert.Equal(updatedBackgroundColor, element.BackgroundColor);
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task BackgroundColorTo_CancellationTokenExpired()
	{
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		VisualElement element = new Label { BackgroundColor = Colors.Blue };
		element.EnableAnimations();

		// Ensure CancellationToken has expired
		await Task.Delay(100, TestContext.Current.CancellationToken);

		await Assert.ThrowsAsync<OperationCanceledException>(() => element.BackgroundColorTo(Colors.Green, token: cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task BackgroundColorTo_CancellationTokenCanceled()
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

		VisualElement element = new Label { BackgroundColor = Colors.Blue };
		element.EnableAnimations();

		// Ensure CancellationToken has expired
		await cts.CancelAsync();

		await Assert.ThrowsAsync<OperationCanceledException>(() => element.BackgroundColorTo(Colors.Green, token: cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task BackgroundColorTo_VerifyColorChangedForDefaultBackgroundColor()
	{
		Color updatedBackgroundColor = Colors.Yellow;

		VisualElement element = new Label();
		element.EnableAnimations();

		var isSuccessful = await element.BackgroundColorTo(updatedBackgroundColor, token: TestContext.Current.CancellationToken);

		Assert.True(isSuccessful);
		Assert.Equal(updatedBackgroundColor, element.BackgroundColor);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task BackgroundColorTo_DoesNotAllowNullVisualElement()
	{
		VisualElement? element = null;

#pragma warning disable CS8603 // Possible null reference return.
		await Assert.ThrowsAsync<NullReferenceException>(() => element?.BackgroundColorTo(Colors.Red, token: TestContext.Current.CancellationToken));
#pragma warning restore CS8603 // Possible null reference return.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task BackgroundColorTo_DoesNotAllowNullColor()
	{
		VisualElement element = new Label();
		element.EnableAnimations();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => element.BackgroundColorTo(null, token: TestContext.Current.CancellationToken));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact]
	public void BackgroundColorTo_DoesNotAllowZeroRate()
	{
		Color originalBackgroundColor = Colors.Blue;

		VisualElement element = new Label { BackgroundColor = originalBackgroundColor };
		element.EnableAnimations();

		// Thrown by the call itself, before an animation starts: .NET MAUI divides by the rate on every frame of the platform's animation ticker
		Assert.Throws<ArgumentOutOfRangeException>("rate", () => { _ = element.BackgroundColorTo(Colors.Red, rate: 0, token: TestContext.Current.CancellationToken); });

		Assert.False(element.AnimationIsRunning(nameof(ColorAnimationExtensions.BackgroundColorTo)));
		Assert.Equal(originalBackgroundColor, element.BackgroundColor);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task BackgroundColorTo_VerifyColorChangedForSmallestRate()
	{
		Color originalBackgroundColor = Colors.Blue, updatedBackgroundColor = Colors.Red;

		VisualElement element = new Label { BackgroundColor = originalBackgroundColor };
		element.EnableAnimations();

		var isSuccessful = await element.BackgroundColorTo(updatedBackgroundColor, rate: 1, token: TestContext.Current.CancellationToken);

		Assert.True(isSuccessful);
		Assert.Equal(updatedBackgroundColor, element.BackgroundColor);
	}
}