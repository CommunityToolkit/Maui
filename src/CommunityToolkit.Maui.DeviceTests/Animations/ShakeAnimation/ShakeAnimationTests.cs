using CommunityToolkit.Maui.Animations;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.Animations)]
[Collection(TestCollections.DisableParallelization)]
public class ShakeAnimationTests : AnimationContractTestBase<ShakeAnimation>
{
	const double originalTranslationX = 5;
	const double startFactor = 15;
	const double reducingAmount = 5;

	protected override IReadOnlyList<(BindableProperty Property, double Value)> OriginalValues { get; } = [(VisualElement.TranslationXProperty, originalTranslationX)];

	protected override BindableProperty PeakProperty => VisualElement.TranslationXProperty;

	protected override double PeakValue => originalTranslationX - startFactor;

	protected override ShakeAnimation CreateAnimation() => new() { StartFactor = startFactor, ReducingAmount = reducingAmount, Length = runLength };
}
