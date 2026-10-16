using Android.Content;
using Android.Views;
using CommunityToolkit.Maui.Core.Views;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class MauiDrawingViewTests
{
	// Catches: the view ignores the context it is constructed with
	[Fact]
	public Task ViewIsCreatedWithTheGivenContext() => WithView(view =>
	{
		Assert.Same(MauiProgramDefaults.DefaultContext, view.Context);
	});

	// Catches: Down/Up/Cancel do not ask the parent to (stop) intercepting, or ask with the wrong value
	[Fact]
	public Task DownAsksTheParentNotToInterceptTouchesAndUpOrCancelAllowsItAgain() => WithView(view =>
	{
		var parent = (DrawingViewTestPlatform.RecordingFrameLayout)view.Parent!;

		DrawingViewTestPlatform.TouchDown(view, 20, 20);
		Assert.Equal(new[] { true }, parent.DisallowInterceptRequests);

		DrawingViewTestPlatform.TouchMove(view, 40, 40);
		Assert.Equal(new[] { true }, parent.DisallowInterceptRequests);

		DrawingViewTestPlatform.TouchUp(view, 40, 40);
		Assert.Equal(new[] { true, false }, parent.DisallowInterceptRequests);

		DrawingViewTestPlatform.TouchDown(view, 20, 20);
		DrawingViewTestPlatform.TouchCancel(view, 20, 20);
		Assert.Equal(new[] { true, false, true, false }, parent.DisallowInterceptRequests);
	});

	// Catches: a view without a parent throws, or handled actions do not return true
	[Fact]
	public async Task TouchEventsWithoutAParentAreHandledAndDoNotThrow()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = DrawingViewTestPlatform.CreateView(200, 100);
			view.Initialize();
			var recorder = new GestureRecorder(view);
			Assert.Null(view.Parent);

			Assert.True(DrawingViewTestPlatform.TouchDown(view, 20, 20));
			Assert.True(DrawingViewTestPlatform.TouchMove(view, 40, 40));
			Assert.True(DrawingViewTestPlatform.TouchUp(view, 40, 40));
			Assert.True(DrawingViewTestPlatform.TouchDown(view, 20, 20));
			Assert.True(DrawingViewTestPlatform.TouchCancel(view, 20, 20));

			Assert.Single(recorder.Completed);
			Assert.Equal(1, recorder.Cancelled);
		});
	}

	// Catches: actions other than Down/Move/Up/Cancel are handled
	[Theory]
	[InlineData(MotionEventActions.PointerDown)]
	[InlineData(MotionEventActions.PointerUp)]
	[InlineData(MotionEventActions.Outside)]
	[InlineData(MotionEventActions.HoverMove)]
	public Task UnhandledActionsReturnFalseWithoutRaisingEvents(MotionEventActions action) => WithView(view =>
	{
		var recorder = new GestureRecorder(view);
		DrawingViewTestPlatform.TouchDown(view, 20, 20);

		var handled = DrawingViewTestPlatform.Send(view, action, 60, 60);

		Assert.False(handled);
		Assert.Empty(recorder.Points);
		Assert.Empty(recorder.Completed);
		Assert.Equal(0, recorder.Cancelled);
	});

	// Catches: a null MotionEvent is not rejected with ArgumentNullException
	[Fact]
	public Task NullMotionEventThrowsArgumentNullException() => WithView(view =>
	{
		Assert.Throws<ArgumentNullException>(() => view.OnTouchEvent(null));
	});

	// Catches: touch pixels are not divided by the display density
	[Fact]
	public Task TouchPixelsAreReportedInDeviceIndependentUnits() => WithView(view =>
	{
		var recorder = new GestureRecorder(view);
		var density = DrawingViewTestPlatform.TouchDensity;

		DrawingViewTestPlatform.Send(view, MotionEventActions.Down, 105, 210);
		DrawingViewTestPlatform.Send(view, MotionEventActions.Move, 210, 105);

		AssertPoint(105 / density, 210 / density, Assert.Single(recorder.Started));
		AssertPoint(210 / density, 105 / density, Assert.Single(recorder.Points));
	});

	// Catches: any of the four bounds comparisons of the Move case is dropped, or the out-of-bounds point is not reported
	[Theory]
	[InlineData("left")]
	[InlineData("top")]
	[InlineData("right")]
	[InlineData("bottom")]
	public Task MoveOnOrBeyondAnEdgeDoesNotExtendThePathButIsStillReported(string edge) => WithViewAsync(async view =>
	{
		view.Paint = new SolidPaint(white);
		view.LineColor = red;
		view.LineWidth = 10;
		var recorder = new GestureRecorder(view);
		double width = view.Width, height = view.Height; // pixels
		DrawingViewTestPlatform.TouchDown(view, 100, 50);

		var (x, y) = edge switch
		{
			"left" => (0d, height / 2),
			"top" => (width / 2, 0d),
			"right" => (width, height / 2),
			"bottom" => (width / 2, height),
			_ => throw new ArgumentOutOfRangeException(nameof(edge))
		};
		DrawingViewTestPlatform.Send(view, MotionEventActions.Move, x, y);

		var density = DrawingViewTestPlatform.TouchDensity;
		AssertPoint(x / density, y / density, Assert.Single(recorder.Points));
		DrawingPixelAssert.DoesNotContainColor(await RenderAsync(view), red, because: $"the {edge} edge is not inside the view, so the in-progress path is not extended");
	});

	// Catches: a move just inside the edges is ignored
	[Fact]
	public Task MoveJustInsideTheEdgesExtendsThePath() => WithViewAsync(async view =>
	{
		view.Paint = new SolidPaint(white);
		view.LineColor = red;
		view.LineWidth = 10;
		DrawingViewTestPlatform.TouchDown(view, 100, 50);

		DrawingViewTestPlatform.Send(view, MotionEventActions.Move, 1, view.Height / 2d);

		DrawingPixelAssert.SegmentHasColor(await RenderAsync(view), (20, 50), (80, 50), red);
	});

	// Catches: points are not clamped (or clamped to the wrong bound) before the line is drawn
	[Theory]
	[InlineData("left", 150, 90, -100, 50, 75, 70, 75, 78)]
	[InlineData("right", 100, 90, 300, 50, 150, 70, 150, 80)]
	[InlineData("top", 50, 90, 100, -50, 75, 45, 75, 20)]
	[InlineData("bottom", 50, 10, 100, 150, 75, 55, 75, 80)]
	public Task StoredLinePointsOutsideTheViewAreClampedIntoItsBounds(string side, double startX, double startY, double endX, double endY, double clampedProbeX, double clampedProbeY, double unclampedProbeX, double unclampedProbeY) => WithViewAsync(async view =>
	{
		view.Paint = new SolidPaint(white);
		view.Lines.Add(new MauiDrawingLine { LineColor = red, LineWidth = 4, ShouldSmoothPathWhenDrawn = false, Points = [new((float)startX, (float)startY), new((float)endX, (float)endY)] });

		var image = await RenderAsync(view);

		DrawingPixelAssert.ColorAtUnits(image, clampedProbeX, clampedProbeY, red, because: $"{side}: the end point is clamped into the 200x100 view, bending the line");
		DrawingPixelAssert.NotColorAtUnits(image, unclampedProbeX, unclampedProbeY, red, because: $"{side}: the unclamped line would pass here");
	});

	// Catches: a line without points breaks the Android normalization
	[Fact]
	public Task EmptyStoredLineIsNormalizedWithoutError() => WithViewAsync(async view =>
	{
		view.Paint = new SolidPaint(white);
		view.Lines.Add(new MauiDrawingLine { LineColor = red, Points = [] });

		DrawingPixelAssert.DoesNotContainColor(await RenderAsync(view), red);
	});

	// Catches: points moved outside the view during a gesture are dropped from the stored line instead of clamped when drawn
	[Fact]
	public Task LineDrawnBeyondTheViewKeepsItsPointsAndIsClampedWhenRendered() => WithViewAsync(async view =>
	{
		view.Paint = new SolidPaint(white);
		view.LineColor = red;
		view.LineWidth = 4;
		view.ShouldClearOnFinish = false;
		var recorder = new GestureRecorder(view);

		DrawingViewTestPlatform.TouchDown(view, 100, 80);
		DrawingViewTestPlatform.TouchMove(view, -100, -20);
		DrawingViewTestPlatform.TouchUp(view, -100, -20);

		var line = Assert.Single(recorder.Completed);
		Assert.Equal(2, line.Points.Count);
		AssertPoint(-100, -20, line.Points[1]);
		var image = await RenderAsync(view);
		DrawingPixelAssert.ColorAtUnits(image, 50, 40, red, because: "the end point (-100,-20) is clamped to (0,0)");
		DrawingPixelAssert.NotColorAtUnits(image, 50, 55, red, because: "the unclamped line would pass here");
	});

	// Catches: Dispose(true) does not dispose the in-progress path.
	// The Handle checks are preconditions, not evidence: Java.Lang.Object.Dispose() releases the peer itself and Java.Lang.Object.Dispose(bool)
	// is empty (Mono.Android 36), so the product's base.Dispose(disposing) call has no observable effect on Android.
	[Fact]
	public async Task DisposeReleasesTheNativePeer()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = DrawingViewTestPlatform.CreateView(100, 100);
			view.Initialize();
			var cachedPlatformPath = new PlatformPathProbe();
			GetCurrentPath(view).PlatformPath = cachedPlatformPath;
			Assert.NotEqual(IntPtr.Zero, view.Handle);

			view.Dispose();

			Assert.Equal(IntPtr.Zero, view.Handle);
			Assert.True(cachedPlatformPath.IsDisposed, "Dispose(true) did not dispose the in-progress PathF.");
		});
	}

	// Catches: the finalizer path (Dispose(false)) disposes the in-progress path (a managed resource)
	[Fact]
	public async Task DisposeWithoutDisposingKeepsTheNativePeer()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var probe = new DisposeProbe(MauiProgramDefaults.DefaultContext);
			probe.Initialize();
			var cachedPlatformPath = new PlatformPathProbe();
			GetCurrentPath(probe).PlatformPath = cachedPlatformPath;

			probe.CallDispose(false);

			Assert.NotEqual(IntPtr.Zero, probe.Handle); // precondition (see DisposeReleasesTheNativePeer)
			Assert.False(cachedPlatformPath.IsDisposed, "Dispose(false) disposed the in-progress PathF.");
			probe.Dispose();
			Assert.True(cachedPlatformPath.IsDisposed);
		});
	}

	sealed class DisposeProbe(Context context) : MauiDrawingView(context)
	{
		public void CallDispose(bool disposing) => Dispose(disposing);
	}
}