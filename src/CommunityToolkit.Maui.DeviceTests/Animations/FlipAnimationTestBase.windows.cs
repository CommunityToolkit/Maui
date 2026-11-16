using Microsoft.UI.Xaml.Media;

namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class FlipAnimationTestBase<TAnimation>
{
	// A flip that never turns the view never makes .NET MAUI write a PlaneProjection, so a projection the app set itself is not stale
	[Fact]
	public async Task FlipThatNeverTurnsTheViewKeepsAnAppSuppliedProjection()
	{
		SetupBuilder();

		IReadOnlyList<(BindableProperty Property, double Value)> originalValues =
			[(PeakProperty, 0), (VisualElement.RotationProperty, originalRotation), (VisualElement.TranslationXProperty, originalTranslationX)];
		var view = CreateAnimatedView(originalValues);

		await RunLoadedAsync(view, async (handler, _) =>
		{
			var appProjection = new Matrix3DProjection();
			GetTransformedView(handler).Projection = appProjection;

			var animation = CreateAnimation();
			animation.Rotation = 0;
			await animation.Animate(view).WaitAsync(waitTimeout);

			Assert.Equal(0, (double)view.GetValue(PeakProperty));
			Assert.Same(appProjection, GetTransformedView(handler).Projection);
		});
	}
}
