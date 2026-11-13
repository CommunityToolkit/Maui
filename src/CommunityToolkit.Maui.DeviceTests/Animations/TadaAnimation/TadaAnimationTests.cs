using CommunityToolkit.Maui.Animations;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.Animations)]
[Collection(TestCollections.DisableParallelization)]
public class TadaAnimationTests : AnimationContractTestBase<TadaAnimation>
{
	const double originalScale = 0.8;
	const double originalRotation = 10;
	const double minimumScale = 0.9;
	const double maximumScale = 1.1;
	const double rotationAngle = 3;

	protected override IReadOnlyList<(BindableProperty Property, double Value)> OriginalValues { get; } = [(VisualElement.ScaleProperty, originalScale), (VisualElement.RotationProperty, originalRotation)];

	protected override BindableProperty PeakProperty => VisualElement.ScaleProperty;

	protected override double PeakValue => originalScale * minimumScale;

	protected override TadaAnimation CreateAnimation() => new() { MinimumScale = minimumScale, MaximumScale = maximumScale, RotationAngle = rotationAngle, Length = runLength };
}
