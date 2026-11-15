using CommunityToolkit.Maui.Animations;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.FlipVerticalAnimation)]
[Collection(TestCollections.DisableParallelization)]
public class FlipVerticalAnimationTests : FlipAnimationTestBase<FlipVerticalAnimation>
{
	const double originalRotationX = 10;
	const double targetRotationX = 90;

	// Not 0, which FlipAnimationTestBase covers
	protected override IReadOnlyList<(BindableProperty Property, double Value)> OriginalValues { get; } = [(VisualElement.RotationXProperty, originalRotationX)];

	protected override BindableProperty PeakProperty => VisualElement.RotationXProperty;

	protected override double PeakValue => targetRotationX;

	protected override FlipVerticalAnimation CreateAnimation() => new() { Rotation = targetRotationX, Length = runLength };

	protected override Task<bool> AppToAnimation(VisualElement view, double target) => view.RotateXToAsync(target, runLength, Easing.Linear);
}
