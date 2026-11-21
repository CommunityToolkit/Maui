using CommunityToolkit.Maui.Animations;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.FlipHorizontalAnimation)]
[Collection(TestCollections.DisableParallelization)]
public class FlipHorizontalAnimationTests : FlipAnimationTestBase<FlipHorizontalAnimation>
{
	const double originalRotationY = 10;
	const double targetRotationY = 90;

	// Not 0, which FlipAnimationTestBase covers
	protected override IReadOnlyList<(BindableProperty Property, double Value)> OriginalValues { get; } = [(VisualElement.RotationYProperty, originalRotationY)];

	protected override BindableProperty PeakProperty => VisualElement.RotationYProperty;

	protected override double PeakValue => targetRotationY;

	protected override FlipHorizontalAnimation CreateAnimation() => new() { Rotation = targetRotationY, Length = runLength };

	protected override Task<bool> StartAppsToAnimation(VisualElement view, double target) => view.RotateYToAsync(target, runLength, Easing.Linear);
}
