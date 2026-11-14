using CommunityToolkit.Maui.Animations;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.Animations)]
[Collection(TestCollections.DisableParallelization)]
public class HeartbeatAnimationTests : AnimationContractTestBase<HeartbeatAnimation>
{
	const double originalScale = 0.8;
	const double maximumScale = 1.3;

	protected override IReadOnlyList<(BindableProperty Property, double Value)> OriginalValues { get; } = [(VisualElement.ScaleProperty, originalScale)];

	protected override BindableProperty PeakProperty => VisualElement.ScaleProperty;

	protected override double PeakValue => originalScale * maximumScale;

	protected override HeartbeatAnimation CreateAnimation() => new() { MaximumScale = maximumScale, Length = runLength };
}
