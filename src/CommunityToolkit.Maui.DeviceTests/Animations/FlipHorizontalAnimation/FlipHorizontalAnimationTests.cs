using CommunityToolkit.Maui.Animations;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.Animations)]
[Collection(TestCollections.DisableParallelization)]
public class FlipHorizontalAnimationTests : ToAnimationTestBase<FlipHorizontalAnimation>
{
	const double originalRotationY = 10;
	const double targetRotationY = 90;

	// Never 0 while the run lasts: .NET MAUI on Windows keeps an old PlaneProjection when the x/y rotation returns to 0 with other values set
	protected override IReadOnlyList<(BindableProperty Property, double Value)> OriginalValues { get; } = [(VisualElement.RotationYProperty, originalRotationY)];

	protected override BindableProperty PeakProperty => VisualElement.RotationYProperty;

	protected override double PeakValue => targetRotationY;

	protected override FlipHorizontalAnimation CreateAnimation() => new() { Rotation = targetRotationY, Length = runLength };

	protected override Task<bool> AppToAnimation(VisualElement view, double target) => view.RotateYToAsync(target, runLength, Easing.Linear);
}
