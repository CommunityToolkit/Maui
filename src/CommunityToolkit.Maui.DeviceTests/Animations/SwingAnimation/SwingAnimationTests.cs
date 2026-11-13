using CommunityToolkit.Maui.Animations;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.Animations)]
[Collection(TestCollections.DisableParallelization)]
public class SwingAnimationTests : AnimationContractTestBase<SwingAnimation>
{
	const double originalRotation = 10;
	const double originalAnchorY = 0.25;
	const double rotationAngle = 15;

	// AnchorY is not the default 0.5, so a run that returns AnchorY to 0.5 instead of the view's own value fails
	protected override IReadOnlyList<(BindableProperty Property, double Value)> OriginalValues { get; } = [(VisualElement.RotationProperty, originalRotation), (VisualElement.AnchorYProperty, originalAnchorY)];

	protected override BindableProperty PeakProperty => VisualElement.RotationProperty;

	protected override double PeakValue => originalRotation + rotationAngle;

	protected override SwingAnimation CreateAnimation() => new() { RotationAngle = rotationAngle, Length = runLength };
}
