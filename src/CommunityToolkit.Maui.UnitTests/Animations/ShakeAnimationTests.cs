using System.ComponentModel;
using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public class ShakeAnimationTests : BaseKeyframeAnimationTests<ShakeAnimation>
{
	const double originalTranslationX = 12;
	const double originalTranslationY = 7;

	protected override IReadOnlySet<string> AnimatedProperties { get; } = new HashSet<string> { nameof(VisualElement.TranslationX) };

	[Fact]
	public void EnsureDefaults()
	{
		var animation = new ShakeAnimation();

		Assert.Equal(BaseAnimationDefaults.Easing, animation.Easing);
		Assert.Equal(ShakeAnimationDefaults.Length, animation.Length);
		Assert.Equal(ShakeAnimationDefaults.StartFactor, animation.StartFactor);
		Assert.Equal(ShakeAnimationDefaults.ReducingAmount, animation.ReducingAmount);
		Assert.Equal(300u, animation.Length);
		Assert.Equal(BindingMode.OneWay, ShakeAnimation.StartFactorProperty.DefaultBindingMode);
		Assert.Equal(BindingMode.OneWay, ShakeAnimation.ReducingAmountProperty.DefaultBindingMode);
		Assert.Equal(15.0, animation.StartFactor);
		Assert.Equal(5.0, animation.ReducingAmount);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(-0.5)]
	public void Animate_NonPositiveReducingAmount_ThrowsBeforeReturningTheTask(double reducingAmount)
	{
		var animation = new ShakeAnimation { ReducingAmount = reducingAmount };
		var view = new Label();

		var exception = Assert.Throws<ArgumentOutOfRangeException>(() => { _ = animation.Animate(view, TestContext.Current.CancellationToken); });

		Assert.Equal("ReducingAmount", exception.ParamName);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_ReachesBothSidesOfTheStartFactor() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new ShakeAnimation { Length = functionalTestLength };
		var view = CreateAnimatedView();

		using var translations = new ExtremeValueRecorder(view, VisualElement.TranslationXProperty);
		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		translations.Min.Should().BeApproximately(originalTranslationX - animation.StartFactor, precision);
		translations.Max.Should().BeApproximately(originalTranslationX + animation.StartFactor, precision);
		view.TranslationX.Should().BeApproximately(originalTranslationX, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_HonorsCustomStartFactor() => RunOnSingleThreadAsync(async token =>
	{
		const double startFactor = 30;

		var animation = new ShakeAnimation { StartFactor = startFactor, Length = functionalTestLength };
		var view = CreateAnimatedView();

		using var translations = new ExtremeValueRecorder(view, VisualElement.TranslationXProperty);
		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		translations.Min.Should().BeApproximately(originalTranslationX - startFactor, precision);
		translations.Max.Should().BeApproximately(originalTranslationX + startFactor, precision);
		view.TranslationX.Should().BeApproximately(originalTranslationX, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_LeavesTranslationYUntouched() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new ShakeAnimation { Length = functionalTestLength };
		var view = CreateAnimatedView();
		var translationYChanges = 0;
		view.PropertyChanged += HandlePropertyChanged;

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		view.PropertyChanged -= HandlePropertyChanged;
		view.TranslationY.Should().Be(originalTranslationY);
		translationYChanges.Should().Be(0);

		void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(VisualElement.TranslationY) && Math.Abs(view.TranslationY - originalTranslationY) > precision)
			{
				translationYChanges++;
			}
		}
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_WithSegmentsThatDoNotDivideEvenly_StillTakesTheFullLength() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new ShakeAnimation { StartFactor = 11, ReducingAmount = 10 };

		var elapsedMilliseconds = await MeasureElapsedAnimationMillisecondsAsync(animation, token);

		elapsedMilliseconds.Should().BeInRange(animation.Length, animation.Length + 16);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_StartFactorFarLargerThanReducingAmount_StillTakesTheFullLength() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new ShakeAnimation { StartFactor = 15, ReducingAmount = 0.01, Length = functionalTestLength };

		var elapsedMilliseconds = await MeasureElapsedAnimationMillisecondsAsync(animation, token);

		elapsedMilliseconds.Should().BeInRange(animation.Length, animation.Length + 16);
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[InlineData(1e-12)]
	[InlineData(double.Epsilon)]
	public Task Animate_ReducingAmountFarSmallerThanStartFactor_CompletesAndReturnsToRest(double reducingAmount) => RunOnSingleThreadAsync(async token =>
	{
		var animation = new ShakeAnimation { StartFactor = 15, ReducingAmount = reducingAmount, Length = functionalTestLength };
		var view = CreateAnimatedView();

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		view.TranslationX.Should().BeApproximately(originalTranslationX, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_StartFactorFarLargerThanReducingAmount_KeepsShakingUntilTheEnd() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new ShakeAnimation { StartFactor = 15, ReducingAmount = 0.01, Length = functionalTestLength };
		var view = CreateAnimatedView();
		using var recorder = new PropertyRecorder(view);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var translations = recorder.ValuesOf(nameof(VisualElement.TranslationX)).ToList();
		var lastQuarter = translations.Skip(translations.Count * 3 / 4).Take(translations.Count / 4 - 1);
		lastQuarter.Max(translationX => Math.Abs(translationX - originalTranslationX)).Should().BeGreaterThan(10);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_WithDefaults_MakesAFinalMovementBackToRestInsteadOfSnapping() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new ShakeAnimation { Length = functionalTestLength };
		var view = CreateAnimatedView();
		using var recorder = new PropertyRecorder(view);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var translations = recorder.ValuesOf(nameof(VisualElement.TranslationX)).ToList();
		translations[^1].Should().BeApproximately(originalTranslationX, precision);
		translations[^2].Should().BeLessThan(originalTranslationX + animation.ReducingAmount - precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_StartFactorOfZero_CompletesWithoutChangingTranslationX() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new ShakeAnimation { StartFactor = 0, Length = functionalTestLength };
		var view = CreateAnimatedView();
		using var recorder = new PropertyRecorder(view);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		recorder.ValuesOf(nameof(VisualElement.TranslationX)).Should().OnlyContain(translationX => Math.Abs(translationX - originalTranslationX) <= precision);
		view.TranslationX.Should().BeApproximately(originalTranslationX, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_MoreSwingsThanTheCap_EndsWithThe100thSwing() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new ShakeAnimation { StartFactor = 15, ReducingAmount = 0.1, Length = functionalTestLength };
		var view = CreateAnimatedView();
		using var recorder = new PropertyRecorder(view);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var translations = recorder.ValuesOf(nameof(VisualElement.TranslationX)).ToList();
		translations[^2].Should().BeApproximately(originalTranslationX + animation.StartFactor - (99 * animation.ReducingAmount), precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_WithEasing_ShapesEachMovementNotTheWholeRun() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new ShakeAnimation { Easing = Easing.CubicIn, Length = functionalTestLength };
		var view = CreateAnimatedView();
		using var recorder = new PropertyRecorder(view);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		// CubicIn over the whole run barely moves the view on the first tick; over the first movement alone it moves it more than 1
		recorder.ValuesOf(nameof(VisualElement.TranslationX)).First().Should().BeLessThan(originalTranslationX - 1);
	}, TestContext.Current.CancellationToken);

	[Theory]
	[InlineData(double.NaN)]
	[InlineData(double.PositiveInfinity)]
	[InlineData(double.NegativeInfinity)]
	public void NonFiniteValues_AreIgnored(double nonFiniteValue)
	{
		var animation = new ShakeAnimation
		{
			StartFactor = nonFiniteValue,
			ReducingAmount = nonFiniteValue
		};

		Assert.Equal(ShakeAnimationDefaults.StartFactor, animation.StartFactor);
		Assert.Equal(ShakeAnimationDefaults.ReducingAmount, animation.ReducingAmount);
	}

	static Label CreateAnimatedView()
	{
		var label = new Label
		{
			TranslationX = originalTranslationX,
			TranslationY = originalTranslationY
		};
		label.EnableAnimations();

		return label;
	}
}
