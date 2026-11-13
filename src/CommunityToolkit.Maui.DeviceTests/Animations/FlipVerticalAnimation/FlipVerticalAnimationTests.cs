using CommunityToolkit.Maui.Animations;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.Animations)]
[Collection(TestCollections.DisableParallelization)]
public class FlipVerticalAnimationTests : ToAnimationTestBase<FlipVerticalAnimation>
{
	const double originalRotationX = 10;
	const double targetRotationX = 90;

	// Never 0 while the run lasts: .NET MAUI on Windows keeps an old PlaneProjection when the x/y rotation returns to 0 with other values set
	protected override IReadOnlyList<(BindableProperty Property, double Value)> OriginalValues { get; } = [(VisualElement.RotationXProperty, originalRotationX)];

	protected override BindableProperty PeakProperty => VisualElement.RotationXProperty;

	protected override double PeakValue => targetRotationX;

	protected override FlipVerticalAnimation CreateAnimation() => new() { Rotation = targetRotationX, Length = runLength };
}
