using System.Collections.Concurrent;
using System.Reflection;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class MauiDrawingViewTests
{
	const uint otherPointerId = 2;

	// The pointer overrides of MauiDrawingView (OnPointerPressed, OnPointerMoved, OnPointerReleased, OnPointerCanceled and OnPointerCaptureLost)
	// read a PointerRoutedEventArgs, which WinUI cannot construct, and the test app cannot inject pointer input (InputInjector needs the restricted
	// capability inputInjectionBrokered). So the tests call the methods behind them through reflection, and the pointer capture itself is checked
	// on the DrawingView page of the Sample, which logs each pointer event while a person draws.

	// On Windows a release also reaches the view when the press began on another element, and with ShouldClearOnFinish a finished line clears the lines.
	[Fact]
	public Task TouchUpWithoutTouchDownKeepsTheLinesWhenShouldClearOnFinishIsSet() => WithView(view =>
	{
		view.ShouldClearOnFinish = true;
		var line = new MauiDrawingLine { Points = [new(10, 10), new(50, 50)] };
		view.Lines.Add(line);
		var recorder = new GestureRecorder(view);

		DrawingViewTestPlatform.TouchUp(view, 50, 50);

		Assert.Same(line, Assert.Single(view.Lines));
		Assert.Empty(recorder.Completed);
		Assert.Equal(0, recorder.Cancelled);
	});

	[Fact]
	public Task CaptureLossWhileDrawingRaisesDrawingCancelledAndAddsNoLine() => WithView(view =>
	{
		var recorder = new GestureRecorder(view);
		DrawingViewTestPlatform.TouchDown(view, 10, 10);
		DrawingViewTestPlatform.TouchMove(view, 30, 30);

		LoseCapture(view);

		Assert.Equal(1, recorder.Cancelled);
		Assert.Empty(recorder.Completed);
		Assert.Empty(view.Lines);
	});

	[Fact]
	public Task MovesAndTheTouchUpAfterACaptureLossNeitherDrawNorCompleteALine() => WithView(view =>
	{
		var recorder = new GestureRecorder(view);
		DrawingViewTestPlatform.TouchDown(view, 10, 10);
		DrawingViewTestPlatform.TouchMove(view, 30, 30);
		LoseCapture(view);

		DrawingViewTestPlatform.TouchMove(view, 60, 60);
		DrawingViewTestPlatform.TouchUp(view, 60, 60);

		AssertPoint(30, 30, Assert.Single(recorder.Points));
		Assert.Empty(recorder.Completed);
		Assert.Empty(view.Lines);
		Assert.Equal(1, recorder.Cancelled);
	});

	[Fact]
	public Task CaptureLossAfterTheTouchUpRaisesNothing() => WithView(view =>
	{
		var recorder = new GestureRecorder(view);
		DrawingViewTestPlatform.Stroke(view, (10, 10), (30, 30));

		LoseCapture(view);

		Assert.Equal(0, recorder.Cancelled);
		Assert.Same(Assert.Single(recorder.Completed), Assert.Single(view.Lines));
	});

	[Fact]
	public Task CaptureLossWithoutATouchDownRaisesNothing() => WithView(view =>
	{
		var recorder = new GestureRecorder(view);

		LoseCapture(view);

		Assert.Equal(0, recorder.Cancelled);
	});

	// WinUI raises PointerCaptureLost during DrawingLineCompleted when a handler of that event removes the view from its window.
	[Fact]
	public Task CaptureLossDuringDrawingLineCompletedDoesNotCancelTheLine() => WithView(view =>
	{
		var recorder = new GestureRecorder(view);
		var captureLoss = new CaptureLossOnDrawingLineCompleted(view);

		DrawingViewTestPlatform.Stroke(view, (10, 10), (30, 30));

		Assert.Equal(1, captureLoss.Count);
		Assert.Equal(0, recorder.Cancelled);
		Assert.Same(Assert.Single(recorder.Completed), Assert.Single(view.Lines));
	});

	[Fact]
	public Task PressOfAnotherPointerWhileALineIsDrawnStartsNoLine() => WithView(view =>
	{
		var recorder = new GestureRecorder(view);
		DrawingViewTestPlatform.TouchDown(view, 10, 10);

		DrawingViewTestPlatform.TouchDown(view, otherPointerId, 80, 80);
		DrawingViewTestPlatform.TouchUp(view, 10, 10);
		DrawingViewTestPlatform.TouchMove(view, otherPointerId, 90, 90);
		DrawingViewTestPlatform.TouchUp(view, otherPointerId);

		AssertPoint(10, 10, Assert.Single(recorder.Started));
		Assert.Empty(recorder.Points);
		AssertPoint(10, 10, Assert.Single(Assert.Single(recorder.Completed).Points));
	});

	[Fact]
	public Task MovesOfAnotherPointerDoNotExtendTheLine() => WithView(view =>
	{
		var recorder = new GestureRecorder(view);
		DrawingViewTestPlatform.TouchDown(view, 10, 10);

		DrawingViewTestPlatform.TouchMove(view, otherPointerId, 80, 80);
		DrawingViewTestPlatform.TouchMove(view, 30, 30);
		DrawingViewTestPlatform.TouchUp(view, 30, 30);

		AssertPoint(30, 30, Assert.Single(recorder.Points));
		var line = Assert.Single(recorder.Completed);
		Assert.Equal(2, line.Points.Count);
		AssertPoint(30, 30, line.Points[1]);
	});

	[Fact]
	public Task ReleaseOfAnotherPointerDoesNotEndTheLine() => WithView(view =>
	{
		var recorder = new GestureRecorder(view);
		DrawingViewTestPlatform.TouchDown(view, 10, 10);

		DrawingViewTestPlatform.TouchUp(view, otherPointerId);
		DrawingViewTestPlatform.TouchMove(view, 30, 30);
		DrawingViewTestPlatform.TouchUp(view, 30, 30);

		AssertPoint(30, 30, Assert.Single(recorder.Points));
		Assert.Equal(2, Assert.Single(recorder.Completed).Points.Count);
	});

	[Fact]
	public Task CancelOfAnotherPointerDoesNotCancelTheLine() => WithView(view =>
	{
		var recorder = new GestureRecorder(view);
		DrawingViewTestPlatform.TouchDown(view, 10, 10);

		DrawingViewTestPlatform.TouchCancel(view, otherPointerId);
		DrawingViewTestPlatform.TouchMove(view, 30, 30);
		DrawingViewTestPlatform.TouchUp(view, 30, 30);

		Assert.Equal(0, recorder.Cancelled);
		Assert.Equal(2, Assert.Single(recorder.Completed).Points.Count);
	});

	[Fact]
	public Task CaptureLossOfAnotherPointerDoesNotCancelTheLine() => WithView(view =>
	{
		var recorder = new GestureRecorder(view);
		DrawingViewTestPlatform.TouchDown(view, 10, 10);

		LoseCapture(view, otherPointerId);
		DrawingViewTestPlatform.TouchMove(view, 30, 30);
		DrawingViewTestPlatform.TouchUp(view, 30, 30);

		Assert.Equal(0, recorder.Cancelled);
		Assert.Equal(2, Assert.Single(recorder.Completed).Points.Count);
	});

	[Fact]
	public async Task InitializeOnWindowsOlderThan18362LeavesTheDrawableUnset()
	{
		Skip.If(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18362), "Needs Windows older than 10.0.18362 (Windows 10 1809). This machine takes the other branch, which InitializeAssignsTheNativeDrawable covers.");
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = DrawingViewTestPlatform.CreateView(100, 100);
			view.Initialize();

			Assert.Null(DrawingViewTestPlatform.GetDrawable(view));
		});
	}

	[Fact]
	public async Task InitializeAssignsTheDrawableOnSupportedWindowsVersions()
	{
		Skip.IfNot(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18362), "Needs Windows 10.0.18362 or newer.");
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = DrawingViewTestPlatform.CreateView(100, 100);
			view.Initialize();

			var drawable = Assert.IsType<IDrawable>(DrawingViewTestPlatform.GetDrawable(view), exactMatch: false);
			Assert.Equal("DrawingViewDrawable", drawable.GetType().Name);
		});
	}

	[Fact]
	public async Task DisposeMarksTheViewDisposedAndCanBeRepeated()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = DrawingViewTestPlatform.CreateView(100, 100);
			view.Initialize();
			var cachedPlatformPath = new PlatformPathProbe();
			GetCurrentPath(view).PlatformPath = cachedPlatformPath;
			Assert.False(IsDisposed(view));

			view.Dispose();
			Assert.True(IsDisposed(view));
			Assert.True(cachedPlatformPath.IsDisposed, "Dispose did not dispose the in-progress PathF.");

			var laterPlatformPath = new PlatformPathProbe();
			GetCurrentPath(view).PlatformPath = laterPlatformPath;
			view.Dispose();
			Assert.True(IsDisposed(view));
			Assert.False(laterPlatformPath.IsDisposed, "A second Dispose ran the disposal again.");
		});
	}

	[Fact]
	public async Task DisposeWithoutDisposingMarksTheViewDisposedAndLaterDisposeIsIgnored()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var probe = new DisposeProbe();
			probe.Initialize();
			var cachedPlatformPath = new PlatformPathProbe();
			GetCurrentPath(probe).PlatformPath = cachedPlatformPath;

			probe.CallDispose(false);
			Assert.True(IsDisposed(probe));
			Assert.False(cachedPlatformPath.IsDisposed, "Dispose(false) disposed the in-progress PathF.");

			probe.CallDispose(true);
			Assert.True(IsDisposed(probe));
			Assert.False(cachedPlatformPath.IsDisposed, "Dispose(true) after Dispose(false) was not ignored.");
		});
	}

	[Fact]
	public async Task FinalizerRunsDisposeWithoutDisposingManagedResources()
	{
		SetupBuilder();
		FinalizerProbe.Calls.Clear();

		await InvokeOnMainThreadAsync(() => new FinalizerProbe().Initialize());
		for (var i = 0; i < 40 && FinalizerProbe.Calls.IsEmpty; i++)
		{
			await AssertionExtensions.Collect();
		}

		Skip.If(FinalizerProbe.Calls.IsEmpty, "The finalizer did not run within 40 forced collections (WinUI can keep the managed wrapper alive), so Dispose(false) could not be observed.");
		Assert.All(FinalizerProbe.Calls, disposing => Assert.False(disposing));
	}

	static bool IsDisposed(MauiDrawingView view) =>
		(bool)typeof(MauiDrawingView).GetField("isDisposed", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;

	static void LoseCapture(MauiDrawingView view, uint pointerId = DrawingViewTestPlatform.DefaultPointerId) =>
		DrawingViewTestPlatform.Invoke(view, "OnCaptureLost", pointerId);

	sealed class DisposeProbe : MauiDrawingView
	{
		public void CallDispose(bool disposing) => Dispose(disposing);
	}

	// The finalizer thread also calls Dispose(bool), so the calls are kept in a ConcurrentQueue.
	sealed class FinalizerProbe : MauiDrawingView
	{
		public static ConcurrentQueue<bool> Calls { get; } = new();

		protected override void Dispose(bool disposing)
		{
			Calls.Enqueue(disposing);
			base.Dispose(disposing);
		}
	}

	// A plain instance method is the handler because the view keeps its subscribers weakly.
	sealed class CaptureLossOnDrawingLineCompleted
	{
		readonly MauiDrawingView view;

		public CaptureLossOnDrawingLineCompleted(MauiDrawingView view)
		{
			this.view = view;
			view.DrawingLineCompleted += OnDrawingLineCompleted;
		}

		public int Count { get; private set; }

		void OnDrawingLineCompleted(object? sender, MauiDrawingLineCompletedEventArgs e)
		{
			Count++;
			LoseCapture(view);
		}
	}
}