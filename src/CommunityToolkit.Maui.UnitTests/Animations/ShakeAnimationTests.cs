using System.ComponentModel;
using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public class ShakeAnimationTests : BaseAnimationTests<ShakeAnimation>
{
	const double originalTranslationX = 12;
	const double originalTranslationY = 7;

	[Fact]
	public void EnsureDefaults()
	{
		var animation = new ShakeAnimation();

		Assert.Equal(BaseAnimationDefaults.Easing, animation.Easing);
		Assert.Equal(ShakeAnimationDefaults.Length, animation.Length);
		Assert.Equal(ShakeAnimationDefaults.StartFactor, animation.StartFactor);
		Assert.Equal(ShakeAnimationDefaults.ReducingAmount, animation.ReducingAmount);
		Assert.Equal(300u, animation.Length);
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
		var animation = new ShakeAnimation();
		var view = CreateAnimatedView();

		var translations = new ExtremeValueRecorder(view, VisualElement.TranslationXProperty);
		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		translations.Min.Should().BeApproximately(originalTranslationX - animation.StartFactor, precision);
		translations.Max.Should().BeApproximately(originalTranslationX + animation.StartFactor, precision);
		view.TranslationX.Should().BeApproximately(originalTranslationX, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_HonorsCustomStartFactor() => RunOnSingleThreadAsync(async token =>
	{
		const double startFactor = 30;

		var animation = new ShakeAnimation { StartFactor = startFactor };
		var view = CreateAnimatedView();

		var translations = new ExtremeValueRecorder(view, VisualElement.TranslationXProperty);
		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		translations.Min.Should().BeApproximately(originalTranslationX - startFactor, precision);
		translations.Max.Should().BeApproximately(originalTranslationX + startFactor, precision);
		view.TranslationX.Should().BeApproximately(originalTranslationX, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_LeavesTranslationYUntouched() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new ShakeAnimation();
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

		var fastestDuration = await MeasureFastestDurationAsync(animation, token);

		fastestDuration.TotalMilliseconds.Should().BeApproximately(animation.Length, 100);
	}, TestContext.Current.CancellationToken);

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
