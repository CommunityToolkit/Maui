using System.Diagnostics;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.DeviceTestsInfrastructure)]
[Collection(TestCollections.DisableParallelization)]
public class MauiWindowHostTests : MauiWindowHostTestBase
{
	const int hostedWindowCount = 3;
	static readonly TimeSpan geometryRequestsSettleTime = TimeSpan.FromSeconds(1);

	[Fact]
	public async Task HostingWindowsLeavesTheWindowOfTheRunnerAsItWas()
	{
		SetupBuilder();
		var geometryBefore = await InvokeOnMainThreadAsync(RunnerWindowGeometry.Capture);

		for (var i = 0; i < hostedWindowCount; i++)
		{
			var page = new ContentPage();
			await RunInMauiWindowAsync(page, isPagePresentedModally: false, page, static _ => Task.CompletedTask);
			await AssertRunnerWindowGeometryStays(geometryBefore);
		}
	}

	// A geometry request of the scene resolves asynchronously, so a change can show up after the host returned.
	async Task AssertRunnerWindowGeometryStays(RunnerWindowGeometry geometryBefore)
	{
		var stopwatch = Stopwatch.StartNew();
		do
		{
			var geometryNow = await InvokeOnMainThreadAsync(RunnerWindowGeometry.Capture);
			Assert.True(geometryBefore == geometryNow, $"Hosting a window changed the window of the runner from {geometryBefore} to {geometryNow}.");
			await Task.Delay(50);
		}
		while (stopwatch.Elapsed < geometryRequestsSettleTime);
	}
}