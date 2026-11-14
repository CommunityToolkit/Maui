using CommunityToolkit.Maui.Animations;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.Animations)]
[Collection(TestCollections.DisableParallelization)]
public class BounceAnimationTests : AnimationContractTestBase<BounceAnimation>
{
	const double originalTranslationY = 5;
	const double distance = 30;

	protected override IReadOnlyList<(BindableProperty Property, double Value)> OriginalValues { get; } = [(VisualElement.TranslationYProperty, originalTranslationY)];

	protected override BindableProperty PeakProperty => VisualElement.TranslationYProperty;

	protected override double PeakValue => originalTranslationY - distance;

	protected override BounceAnimation CreateAnimation() => new() { Distance = distance, Length = runLength };
}
