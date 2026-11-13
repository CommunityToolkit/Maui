using CommunityToolkit.Maui.Animations;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.Animations)]
[Collection(TestCollections.DisableParallelization)]
public class ScaleAnimationTests : ToAnimationTestBase<ScaleAnimation>
{
	const double originalScale = 0.8;
	const double targetScale = 1.2;

	protected override IReadOnlyList<(BindableProperty Property, double Value)> OriginalValues { get; } = [(VisualElement.ScaleProperty, originalScale)];

	protected override BindableProperty PeakProperty => VisualElement.ScaleProperty;

	protected override double PeakValue => targetScale;

	protected override ScaleAnimation CreateAnimation() => new() { Scale = targetScale, Length = runLength };
}
