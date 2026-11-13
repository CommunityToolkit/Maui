using CommunityToolkit.Maui.Animations;

namespace CommunityToolkit.Maui.DeviceTests;

// A "To" animation runs under .NET MAUI's own animation handles, so a run started while another one runs interrupts it without AnimationBehavior's help
public abstract class ToAnimationTestBase<TAnimation> : AnimationContractTestBase<TAnimation> where TAnimation : BaseAnimation
{
	[Fact]
	public async Task AnimationStartedMidRunStopsTheRunningOneAndReturnsTheViewToItsOriginalValues()
	{
		SetupBuilder();

		var view = CreateAnimatedView(OriginalValues);

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			var midRun = WaitForMidRunAsync(view);
			var firstRun = CreateAnimation().Animate(view);
			await midRun;

			var peakAgain = WaitForPeakAsync(view);
			var secondRun = CreateAnimation().Animate(view);
			await Task.WhenAll(firstRun, secondRun).WaitAsync(waitTimeout);

			Assert.True(peakAgain.IsCompletedSuccessfully, "The second animation did not reach its peak.");
			AssertAtOriginalValues(handler, view, OriginalValues, nativeStateAtRest, "after both animations ended");
		});
	}
}
