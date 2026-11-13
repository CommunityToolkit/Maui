using CommunityToolkit.Maui.Animations;

namespace CommunityToolkit.Maui.DeviceTests;

// Every attention seeker runs under one animation handle, so starting one aborts the running one, which returns the view first
[Category(TestCategory.Animations)]
[Collection(TestCollections.DisableParallelization)]
public class KeyframeAnimationExtensionsTests : AnimationTestBase
{
	const double valueTolerance = 0.001;
	const double originalRotation = 10;
	const double originalAnchorY = 0.25;
	const double originalTranslationX = 5;
	const double originalTranslationY = 5;
	const double swingAngle = 15;
	const double wobbleDistance = 25;
	const double rotateBy = 180;
	const double translateTargetX = 60;
	const double bounceDistance = 30;

	// Swing animates Rotation and AnchorY, Wobble TranslationX and Rotation: each run has a property of its own and one they share
	static readonly IReadOnlyList<(BindableProperty Property, double Value)> originalValues =
		[(VisualElement.RotationProperty, originalRotation), (VisualElement.AnchorYProperty, originalAnchorY), (VisualElement.TranslationXProperty, originalTranslationX)];

	static readonly IReadOnlyList<(BindableProperty Property, double Value)> rotationAndAnchor =
		[(VisualElement.RotationProperty, originalRotation), (VisualElement.AnchorYProperty, originalAnchorY)];

	[Fact]
	public async Task AttentionSeekerStartedWhileAnotherRunsReturnsBothPropertySetsToTheirOriginalValues()
	{
		SetupBuilder();

		var view = CreateAnimatedView(originalValues);
		var swing = new SwingAnimation { RotationAngle = swingAngle, Length = runLength };
		var wobble = new WobbleAnimation { Distance = wobbleDistance, RotationAngle = 5, Length = runLength };

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			var swinging = WaitForValueAsync(view, VisualElement.RotationProperty, rotation => rotation >= originalRotation + (swingAngle / 3));
			var swingRun = swing.Animate(view);
			await swinging;

			var wobblePeak = WaitForValueAsync(view, VisualElement.TranslationXProperty, translationX => Math.Abs(translationX - (originalTranslationX - wobbleDistance)) <= valueTolerance);
			var wobbleRun = wobble.Animate(view);

			// Started on the UI thread, Wobble aborted Swing synchronously, and Swing returned the view before Wobble read the original values
			AssertAtOriginalValues(handler, view, originalValues, nativeStateAtRest, "right after Wobble started");

			await swingRun.WaitAsync(waitTimeout);
			Assert.True(swingRun.IsCompletedSuccessfully, $"Swing ended {swingRun.Status}; a replaced attention seeker completes normally.");

			await wobbleRun.WaitAsync(waitTimeout);
			await wobblePeak;
			AssertAtOriginalValues(handler, view, originalValues, nativeStateAtRest, "after Wobble completed");
		});
	}

	[Fact]
	public async Task AttentionSeekerStartedWhileALongerRotateAnimationRunsStopsItAndReturnsTheViewToItsOriginalValues()
	{
		SetupBuilder();

		var view = CreateAnimatedView(rotationAndAnchor);
		var rotate = new RotateAnimation { Rotation = rotateBy, Length = 2 * runLength };
		var swing = new SwingAnimation { RotationAngle = swingAngle, Length = runLength };

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			var rotating = WaitForValueAsync(view, VisualElement.RotationProperty, rotation => rotation >= originalRotation + 20);
			var rotateRun = rotate.Animate(view);
			await rotating;

			var swingPeak = WaitForValueAsync(view, VisualElement.RotationProperty, rotation => Math.Abs(rotation - (originalRotation + swingAngle)) <= valueTolerance);
			var swingRun = swing.Animate(view);

			await Task.WhenAll(rotateRun, swingRun).WaitAsync(waitTimeout);
			await swingPeak;

			await AssertStaysAtOriginalValuesAsync(handler, view, rotationAndAnchor, nativeStateAtRest, "after Swing replaced the Rotate animation");
		});
	}

	[Fact]
	public async Task BounceStartedWhileTranslateToRunsLeavesTheTranslateRunningToItsTarget()
	{
		SetupBuilder();

		var view = CreateAnimatedView([(VisualElement.TranslationXProperty, originalTranslationX), (VisualElement.TranslationYProperty, originalTranslationY)]);
		var bounce = new BounceAnimation { Distance = bounceDistance, Length = runLength };

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			var translating = WaitForValueAsync(view, VisualElement.TranslationXProperty, translationX => translationX >= originalTranslationX + 10);
			var translateRun = view.TranslateToAsync(translateTargetX, originalTranslationY, 3 * runLength, Easing.Linear);
			await translating;

			var bouncePeak = WaitForValueAsync(view, VisualElement.TranslationYProperty, translationY => Math.Abs(translationY - (originalTranslationY - bounceDistance)) <= valueTolerance);
			var bounceRun = bounce.Animate(view);

			await bounceRun.WaitAsync(waitTimeout);
			await bouncePeak;

			// TranslateToAsync reports true when something interrupted it
			var translateWasInterrupted = await translateRun.WaitAsync(waitTimeout);
			Assert.False(translateWasInterrupted, "Bounce interrupted the TranslateToAsync that was running.");

			Assert.True(Math.Abs(view.TranslationX - translateTargetX) <= valueTolerance, $"TranslationX is {view.TranslationX}, expected the target {translateTargetX}.");
			Assert.True(Math.Abs(view.TranslationY - originalTranslationY) <= valueTolerance, $"TranslationY is {view.TranslationY}, expected its original {originalTranslationY}.");

			var mismatch = FindNativeMismatch(handler, view);
			Assert.True(mismatch is null, mismatch);
		});
	}
}
