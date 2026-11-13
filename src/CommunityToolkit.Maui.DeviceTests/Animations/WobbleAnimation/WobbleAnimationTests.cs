using CommunityToolkit.Maui.Animations;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.Animations)]
[Collection(TestCollections.DisableParallelization)]
public class WobbleAnimationTests : AnimationContractTestBase<WobbleAnimation>
{
	const double originalTranslationX = 5;
	const double originalRotation = 10;
	const double distance = 25;
	const double rotationAngle = 5;

	protected override IReadOnlyList<(BindableProperty Property, double Value)> OriginalValues { get; } = [(VisualElement.TranslationXProperty, originalTranslationX), (VisualElement.RotationProperty, originalRotation)];

	protected override BindableProperty PeakProperty => VisualElement.TranslationXProperty;

	protected override double PeakValue => originalTranslationX - distance;

	protected override WobbleAnimation CreateAnimation() => new() { Distance = distance, RotationAngle = rotationAngle, Length = runLength };
}
