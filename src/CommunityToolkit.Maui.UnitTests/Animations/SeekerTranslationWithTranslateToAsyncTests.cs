using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

// A TranslateToAsync keeps running under an attention seeker that animates the same axis, and writes the axis as an absolute value too
public class SeekerTranslationWithTranslateToAsyncTests : BaseAnimationTest
{
	const double translationDistance = 100;
	const uint shortRunLength = 96;
	const uint longRunLength = 320;

	public static TheoryData<string, bool> SeekersAndWhetherTheyOutlastTheTranslation { get; } = new()
	{
		{ nameof(BounceAnimation), false }, { nameof(BounceAnimation), true },
		{ nameof(ShakeAnimation), false }, { nameof(ShakeAnimation), true },
		{ nameof(WobbleAnimation), false }, { nameof(WobbleAnimation), true },
	};

	public static TheoryData<string> TranslatingSeekerNames { get; } = [nameof(BounceAnimation), nameof(ShakeAnimation), nameof(WobbleAnimation)];

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(SeekersAndWhetherTheyOutlastTheTranslation))]
	public Task Seeker_StartedWhileTranslateToAsyncRuns_LeavesBothAxesAtTheAppsTarget(string seekerName, bool seekerOutlastsTheTranslation) => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var (targetX, targetY) = (view.TranslationX + translationDistance, view.TranslationY + translationDistance);

		var seekerRun = await StartSeekerDuringTranslateToAsync(view, seekerName, seekerOutlastsTheTranslation ? longRunLength : shortRunLength, seekerOutlastsTheTranslation ? shortRunLength : longRunLength, token);

		await seekerRun.Seeker.WaitAsync(waitTimeout, token);
		var wasTranslateInterrupted = await seekerRun.Translation.WaitAsync(waitTimeout, token);

		wasTranslateInterrupted.Should().BeFalse();
		view.TranslationX.Should().BeApproximately(targetX, precision);
		view.TranslationY.Should().BeApproximately(targetY, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task BounceAnimation_OutlastingTheTranslation_HopsOnTopOfTheTranslationsTarget() => RunOnSingleThreadAsync(async token =>
	{
		const double distance = 30;
		var view = CreateAnimatedViewWithNonDefaultValues();
		var targetY = view.TranslationY + translationDistance;

		var run = await StartSeekerDuringTranslateToAsync(view, nameof(BounceAnimation), 2 * longRunLength, shortRunLength, token, distance);
		await run.Translation.WaitAsync(waitTimeout, token);
		using var translationYRecorder = new ExtremeValueRecorder(view, VisualElement.TranslationYProperty);
		await run.Seeker.WaitAsync(waitTimeout, token);

		translationYRecorder.Min.Should().BeApproximately(targetY - distance, 0.01, "the first hop rises by the distance from the translation's target");
		view.TranslationY.Should().BeApproximately(targetY, precision);
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(TranslatingSeekerNames))]
	public Task Seeker_CancelledAfterTheTranslationEnded_RestoresWithoutTheSeekersOffset(string seekerName) => RunOnSingleThreadAsync(async token =>
	{
		using var seekerTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
		var view = CreateAnimatedViewWithNonDefaultValues();
		var (targetX, targetY) = (view.TranslationX + translationDistance, view.TranslationY + translationDistance);

		var run = await StartSeekerDuringTranslateToAsync(view, seekerName, 10 * longRunLength, shortRunLength, seekerTokenSource.Token);
		await run.Translation.WaitAsync(waitTimeout, token);
		await WaitForFramesAsync(view, 8, token);
		seekerTokenSource.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.Seeker.WaitAsync(waitTimeout, token));
		view.TranslationX.Should().BeApproximately(targetX, precision);
		view.TranslationY.Should().BeApproximately(targetY, precision);
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(TranslatingSeekerNames))]
	public Task Seeker_RetriggeredAfterTheTranslationEnded_EndsAtTheAppsTarget(string seekerName) => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var (targetX, targetY) = (view.TranslationX + translationDistance, view.TranslationY + translationDistance);

		var run = await StartSeekerDuringTranslateToAsync(view, seekerName, 10 * longRunLength, shortRunLength, token);
		await run.Translation.WaitAsync(waitTimeout, token);
		await WaitForFramesAsync(view, 8, token);
		var retriggeredRun = Create(seekerName, shortRunLength).Animate(view, token);

		await Task.WhenAll(run.Seeker, retriggeredRun).WaitAsync(waitTimeout, token);
		view.TranslationX.Should().BeApproximately(targetX, precision);
		view.TranslationY.Should().BeApproximately(targetY, precision);
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(TranslatingSeekerNames))]
	public Task Seeker_CancelledInTheTurnTheAppSetsTheTranslation_KeepsTheAppsTranslation(string seekerName) => RunOnSingleThreadAsync(async token =>
	{
		const double appTranslation = 55;
		using var seekerTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
		var view = CreateAnimatedViewWithNonDefaultValues();
		var translationProperty = TranslationPropertyOf(seekerName);
		var startingTranslation = (double)view.GetValue(translationProperty);

		var hasMoved = WaitForPropertyAsync(view, translationProperty, token, translation => Math.Abs(translation - startingTranslation) > precision);
		var run = Create(seekerName, longRunLength).Animate(view, seekerTokenSource.Token);
		await hasMoved;

		view.SetValue(translationProperty, appTranslation);
		seekerTokenSource.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(waitTimeout, token));
		((double)view.GetValue(translationProperty)).Should().Be(appTranslation, "the app wrote the translation after the seeker's last frame, so nothing but the app's value is left to keep");
	}, TestContext.Current.CancellationToken);

	static async Task<(Task Seeker, Task<bool> Translation)> StartSeekerDuringTranslateToAsync(Label view, string seekerName, uint seekerLength, uint translationLength, CancellationToken token, double distance = 30)
	{
		var startingTranslationX = view.TranslationX;
		var translating = WaitForPropertyAsync(view, VisualElement.TranslationXProperty, token, translationX => translationX - startingTranslationX > 5);
		var translation = view.TranslateToAsync(startingTranslationX + translationDistance, view.TranslationY + translationDistance, translationLength, Easing.Linear, token);
		await translating;

		return (Create(seekerName, seekerLength, distance).Animate(view, token), translation);
	}

	static BindableProperty TranslationPropertyOf(string seekerName) => seekerName is nameof(BounceAnimation) ? VisualElement.TranslationYProperty : VisualElement.TranslationXProperty;

	static BaseAnimation Create(string seekerName, uint length, double distance = 30) => seekerName switch
	{
		nameof(BounceAnimation) => new BounceAnimation { Distance = distance, Length = length },
		nameof(ShakeAnimation) => new ShakeAnimation { Length = length },
		nameof(WobbleAnimation) => new WobbleAnimation { Length = length },
		_ => throw new NotSupportedException(seekerName)
	};
}