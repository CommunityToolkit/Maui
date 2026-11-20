using Microsoft.UI.Xaml.Media;

namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class FlipAnimationTestBase<TAnimation>
{
	// A flip that never turns the view never makes .NET MAUI write a projection, so a projection the app set itself is not stale
	[Fact]
	public Task FlipThatNeverTurnsTheViewKeepsAnAppSuppliedMatrix3DProjection() => AssertFlipThatNeverTurnsTheViewKeepsAppProjectionAsync(static () => new Matrix3DProjection());

	// .NET MAUI's own projection is a PlaneProjection too, so only the instance tells the app's apart from a stale one
	[Fact]
	public Task FlipThatNeverTurnsTheViewKeepsAnAppSuppliedPlaneProjection() => AssertFlipThatNeverTurnsTheViewKeepsAppProjectionAsync(static () => new PlaneProjection());

	// .NET MAUI replaces the app's projection as soon as the view turns, and keeps its own once the view is back at 0 with other transforms set
	[Fact]
	public async Task FlipThatTurnsTheViewClearsTheStaleProjectionThatReplacedAnAppSuppliedOne()
	{
		SetupBuilder();

		var originalValues = OriginalValuesFromZeroWithOtherTransformsSet;
		var view = CreateAnimatedView(originalValues);

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			var appProjection = new PlaneProjection();
			GetTransformedView(handler).Projection = appProjection;

			await CreateAnimation().Animate(view).WaitAsync(waitTimeout);

			await AssertStaysAtOriginalValuesAsync(handler, view, originalValues, nativeStateAtRest, "after a flip that replaced an app-supplied projection");
		});
	}

	async Task AssertFlipThatNeverTurnsTheViewKeepsAppProjectionAsync(Func<Projection> createAppProjection)
	{
		SetupBuilder();

		var originalValues = OriginalValuesFromZeroWithOtherTransformsSet;
		var view = CreateAnimatedView(originalValues);

		await RunLoadedAsync(view, async (handler, _) =>
		{
			var appProjection = createAppProjection();
			GetTransformedView(handler).Projection = appProjection;

			var animation = CreateAnimation();
			animation.Rotation = 0;
			await animation.Animate(view).WaitAsync(waitTimeout);

			Assert.Equal(0, (double)view.GetValue(PeakProperty));
			Assert.Same(appProjection, GetTransformedView(handler).Projection);
		});
	}
}
