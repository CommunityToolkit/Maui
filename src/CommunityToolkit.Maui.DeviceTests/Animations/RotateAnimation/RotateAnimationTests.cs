using CommunityToolkit.Maui.Animations;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.Animations)]
[Collection(TestCollections.DisableParallelization)]
public class RotateAnimationTests : ToAnimationTestBase<RotateAnimation>
{
	const double originalRotation = 10;
	const double targetRotation = 180;

	protected override IReadOnlyList<(BindableProperty Property, double Value)> OriginalValues { get; } = [(VisualElement.RotationProperty, originalRotation)];

	protected override BindableProperty PeakProperty => VisualElement.RotationProperty;

	protected override double PeakValue => targetRotation;

	protected override RotateAnimation CreateAnimation() => new() { Rotation = targetRotation, Length = runLength };
}
