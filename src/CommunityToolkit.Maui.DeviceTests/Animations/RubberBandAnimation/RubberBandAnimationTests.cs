using CommunityToolkit.Maui.Animations;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.Animations)]
[Collection(TestCollections.DisableParallelization)]
public class RubberBandAnimationTests : AnimationContractTestBase<RubberBandAnimation>
{
	const double originalScaleX = 0.9;
	const double originalScaleY = 1.1;
	const double firstStretchFactor = 1.25;

	protected override IReadOnlyList<(BindableProperty Property, double Value)> OriginalValues { get; } = [(VisualElement.ScaleXProperty, originalScaleX), (VisualElement.ScaleYProperty, originalScaleY)];

	protected override BindableProperty PeakProperty => VisualElement.ScaleXProperty;

	// The end of the first keyframe segment, where the band is stretched widest
	protected override double PeakValue => originalScaleX * firstStretchFactor;

	protected override RubberBandAnimation CreateAnimation() => new() { Length = runLength };
}
