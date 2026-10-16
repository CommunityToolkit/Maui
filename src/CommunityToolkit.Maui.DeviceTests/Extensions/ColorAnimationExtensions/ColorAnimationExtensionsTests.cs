using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.DeviceTests;

// A rate of 0 used to reach .NET MAUI's tweener, which divides by the rate on each frame: the DivideByZeroException was thrown in the
// platform animation ticker's frame callback, where no caller could catch it, and the app terminated. The unit tests' ticker swallows
// such exceptions, so only a device shows the crash.
[Category(TestCategory.ColorAnimationExtensions)]
[Collection(TestCollections.DisableParallelization)]
public class ColorAnimationExtensionsTests : CommunityToolkitHandlerTestBase
{
	// The animation name of the generated TextColorTo: nameof(TextColorTo).
	const string textColorToAnimationName = "TextColorTo";

	const uint animationLength = 100;

	[Fact]
	public async Task BackgroundColorToThrowsForAZeroRateAndRunsAtTheSmallestRate()
	{
		SetupBuilder();

		var border = new Border { BackgroundColor = Colors.Blue, WidthRequest = 50, HeightRequest = 50 };

		await AttachAndRun(border, async handler =>
		{
			await border.OnLoadedAsync();

			Assert.Throws<ArgumentOutOfRangeException>("rate", () => { _ = border.BackgroundColorTo(Colors.Red, rate: 0, length: animationLength); });
			Assert.False(border.AnimationIsRunning(nameof(ColorAnimationExtensions.BackgroundColorTo)), "A rejected call must not start an animation.");
			Assert.Equal(Colors.Blue, border.BackgroundColor);

			// When Android's animations are off, MAUI ends the animation at once.
			Assert.True(await border.BackgroundColorTo(Colors.Red, rate: 1, length: animationLength));
			Assert.Equal(Colors.Red, border.BackgroundColor);
		});
	}

	[Fact]
	public async Task TextColorToThrowsForAZeroRateAndRunsAtTheSmallestRate()
	{
		SetupBuilder();

		var label = new Label { Text = nameof(TextColorToThrowsForAZeroRateAndRunsAtTheSmallestRate), TextColor = Colors.Blue };

		await AttachAndRun(label, async handler =>
		{
			await label.OnLoadedAsync();

			Assert.Throws<ArgumentOutOfRangeException>("rate", () => { _ = label.TextColorTo(Colors.Red, rate: 0, length: animationLength); });
			Assert.False(label.AnimationIsRunning(textColorToAnimationName), "A rejected call must not start an animation.");
			Assert.Equal(Colors.Blue, label.TextColor);

			// When Android's animations are off, MAUI ends the animation at once.
			Assert.True(await label.TextColorTo(Colors.Red, rate: 1, length: animationLength));
			Assert.Equal(Colors.Red, label.TextColor);
		});
	}
}