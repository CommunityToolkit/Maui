using System.Collections;
using System.Reflection;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CoreGraphics;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class MauiDrawingViewTests
{
	// Catches: TouchesBegan reads the current location (or TouchesMoved the previous one)
	[Fact]
	public Task TouchesBeganReportsThePreviousLocationAndTouchesMovedTheCurrentLocation() => WithView(view =>
	{
		var recorder = new GestureRecorder(view);

		view.TouchesBegan(DrawingViewTestPlatform.CreateTouches(location: new CGPoint(10, 20), previousLocation: new CGPoint(30, 40)), null);
		view.TouchesMoved(DrawingViewTestPlatform.CreateTouches(location: new CGPoint(50, 60), previousLocation: new CGPoint(70, 80)), null);

		AssertPoint(30, 40, Assert.Single(recorder.Started));
		AssertPoint(50, 60, Assert.Single(recorder.Points));
	});

	// Catches: ancestor scroll views stay enabled during the gesture, or are not re-enabled afterwards
	[Fact]
	public Task TouchesBeganDisablesParentScrollViewsAndTouchesEndedReEnablesThem() => WithScrollParents((view, outer, inner) =>
	{
		Assert.True(outer.ScrollEnabled);
		Assert.True(inner.ScrollEnabled);

		DrawingViewTestPlatform.TouchDown(view, 20, 20);
		Assert.False(outer.ScrollEnabled);
		Assert.False(inner.ScrollEnabled);

		DrawingViewTestPlatform.TouchMove(view, 40, 40);
		Assert.False(outer.ScrollEnabled);
		Assert.False(inner.ScrollEnabled);

		DrawingViewTestPlatform.TouchUp(view, 40, 40);
		Assert.True(outer.ScrollEnabled);
		Assert.True(inner.ScrollEnabled);
	});

	// Catches: TouchesCancelled does not re-enable the ancestor scroll views
	[Fact]
	public Task TouchesCancelledReEnablesParentScrollViews() => WithScrollParents((view, outer, inner) =>
	{
		DrawingViewTestPlatform.TouchDown(view, 20, 20);
		Assert.False(outer.ScrollEnabled);
		Assert.False(inner.ScrollEnabled);

		DrawingViewTestPlatform.TouchCancel(view, 20, 20);

		Assert.True(outer.ScrollEnabled);
		Assert.True(inner.ScrollEnabled);
	});

	// Catches: the ancestors are added to the cache again on every gesture (duplicates), or a later gesture no longer toggles them
	[Fact]
	public Task RepeatedGesturesToggleTheParentScrollViewsEachTimeWithoutDuplicates() => WithScrollParents((view, outer, inner) =>
	{
		for (var i = 0; i < 3; i++)
		{
			DrawingViewTestPlatform.TouchDown(view, 20, 20);
			Assert.False(outer.ScrollEnabled, $"gesture {i + 1}: the outer scroll view still scrolls");
			Assert.False(inner.ScrollEnabled, $"gesture {i + 1}: the inner scroll view still scrolls");
			// duplicates have no native observable, so the cache is read while it is in use; whether it is kept after the gesture is not asserted
			// (without a readable cache, for example after a fix that no longer caches the ancestors, there is nothing that could hold duplicates)
			if (TryGetCachedScrollViews(view) is List<object> cached)
			{
				Assert.Equal(cached.Distinct().Count(), cached.Count);
			}

			DrawingViewTestPlatform.TouchUp(view, 20, 20);
			Assert.True(outer.ScrollEnabled);
			Assert.True(inner.ScrollEnabled);
		}
	});

	// The ancestors cached by the first touch must follow the view when it moves into another UIScrollView
	[Fact]
	public async Task GestureAfterTheViewMovesToAnotherScrollViewDisablesOnlyTheNewScrollView()
	{
		SetupBuilder();
		var (root, first, second, view) = await InvokeOnMainThreadAsync(() =>
		{
			var root = new UIView { Frame = new CGRect(0, 0, 600, 300) };
			var first = new UIScrollView { Frame = new CGRect(0, 0, 250, 250) };
			var second = new UIScrollView { Frame = new CGRect(300, 0, 250, 250) };
			var view = DrawingViewTestPlatform.CreateView(200, 100);
			view.Initialize();
			first.AddSubview(view);
			root.AddSubview(first);
			root.AddSubview(second);
			return (root, first, second, view);
		});

		await InvokeOnMainThreadAsync(async () => await root.AttachAndRun(() =>
		{
			Stroke(view, (20, 20), (40, 40));
			view.RemoveFromSuperview();
			second.AddSubview(view);

			DrawingViewTestPlatform.TouchDown(view, 20, 20);
			var firstEnabledDuringGesture = first.ScrollEnabled;
			var secondEnabledDuringGesture = second.ScrollEnabled;
			DrawingViewTestPlatform.TouchUp(view, 20, 20);

			Assert.True(firstEnabledDuringGesture, "The UIScrollView that the drawing view left was disabled by the gesture.");
			Assert.False(secondEnabledDuringGesture, "The UIScrollView that now contains the drawing view stayed scrollable during the gesture.");
			Assert.True(second.ScrollEnabled, "The UIScrollView that now contains the drawing view was not enabled again when the gesture ended.");
		}));
	}

	// The UIScrollView ancestors cached by the first touch must not keep the view (or the scroll view) alive once it is removed and cleaned up
	[Fact]
	public async Task ViewInsideAScrollViewIsCollectableAfterAGesture()
	{
		SetupBuilder();
		WeakReference viewReference = null!, scrollViewReference = null!;

		await HostGestureAndDropAsync();

		await DrawingViewTestPlatform.WaitUntilCollectedAsync(viewReference, scrollViewReference);

		async Task HostGestureAndDropAsync()
		{
			var (scrollView, view) = await InvokeOnMainThreadAsync(() =>
			{
				var scrollView = new UIScrollView { Frame = new CGRect(0, 0, 300, 300) };
				var view = DrawingViewTestPlatform.CreateView(200, 100);
				view.Initialize();
				scrollView.AddSubview(view);
				return (scrollView, view);
			});

			await InvokeOnMainThreadAsync(async () => await scrollView.AttachAndRun(() =>
			{
				DrawingViewTestPlatform.TouchDown(view, 20, 20);
				Assert.False(scrollView.ScrollEnabled); // precondition: the touch found the scroll view ancestor
				DrawingViewTestPlatform.TouchMove(view, 40, 40);
				DrawingViewTestPlatform.TouchUp(view, 40, 40);
			}));

			await InvokeOnMainThreadAsync(view.CleanUp); // what DrawingViewHandler.DisconnectHandler does
			viewReference = new(view);
			scrollViewReference = new(scrollView);
		}
	}

	// A gesture that UIKit cancels (TouchesCancelled, for example when the view leaves its window) must also
	// release the UIScrollView ancestors it found, as ViewInsideAScrollViewIsCollectableAfterAGesture checks for a gesture that ends
	[Fact]
	public async Task ViewInsideAScrollViewIsCollectableAfterACancelledGesture()
	{
		SetupBuilder();
		WeakReference viewReference = null!, scrollViewReference = null!;

		await HostCancelledGestureAndDropAsync();

		await DrawingViewTestPlatform.WaitUntilCollectedAsync(viewReference, scrollViewReference);

		async Task HostCancelledGestureAndDropAsync()
		{
			var (scrollView, view) = await InvokeOnMainThreadAsync(() =>
			{
				var scrollView = new UIScrollView { Frame = new CGRect(0, 0, 300, 300) };
				var view = DrawingViewTestPlatform.CreateView(200, 100);
				view.Initialize();
				scrollView.AddSubview(view);
				return (scrollView, view);
			});

			await InvokeOnMainThreadAsync(async () => await scrollView.AttachAndRun(() =>
			{
				DrawingViewTestPlatform.TouchDown(view, 20, 20);
				Assert.False(scrollView.ScrollEnabled); // precondition: the touch found the scroll view ancestor
				DrawingViewTestPlatform.TouchMove(view, 40, 40);
				DrawingViewTestPlatform.TouchCancel(view, 40, 40);
				Assert.True(scrollView.ScrollEnabled, "The cancelled gesture did not enable the UIScrollView ancestor again.");
			}));

			await InvokeOnMainThreadAsync(view.CleanUp); // what DrawingViewHandler.DisconnectHandler does
			viewReference = new(view);
			scrollViewReference = new(scrollView);
		}
	}

	// Control for ViewInsideAScrollViewIsCollectableAfterAGesture: without a touch the cache stays empty and the same views are collected,
	// so a failure there is caused by the cache and not by the hosting or the GC wait
	[Fact]
	public async Task ViewInsideAScrollViewIsCollectableWithoutAGesture()
	{
		SetupBuilder();
		WeakReference viewReference = null!, scrollViewReference = null!;

		await HostAndDropAsync();

		await DrawingViewTestPlatform.WaitUntilCollectedAsync(viewReference, scrollViewReference);

		async Task HostAndDropAsync()
		{
			var (scrollView, view) = await InvokeOnMainThreadAsync(() =>
			{
				var scrollView = new UIScrollView { Frame = new CGRect(0, 0, 300, 300) };
				var view = DrawingViewTestPlatform.CreateView(200, 100);
				view.Initialize();
				scrollView.AddSubview(view);
				return (scrollView, view);
			});

			await InvokeOnMainThreadAsync(async () => await scrollView.AttachAndRun(() => Assert.Empty(TryGetCachedScrollViews(view) ?? [])));

			await InvokeOnMainThreadAsync(view.CleanUp);
			viewReference = new(view);
			scrollViewReference = new(scrollView);
		}
	}

	// Catches: scroll views that are not ancestors are disabled, or a hierarchy without scroll views breaks the gesture
	[Fact]
	public async Task GestureWithoutScrollViewAncestorsLeavesOtherScrollViewsAlone()
	{
		SetupBuilder();
		var (root, view, sibling) = await InvokeOnMainThreadAsync(() =>
		{
			var root = new UIView { Frame = new CGRect(0, 0, 300, 300) };
			var container = new UIView { Frame = new CGRect(0, 0, 250, 250) };
			var sibling = new UIScrollView { Frame = new CGRect(0, 150, 100, 100) };
			var view = DrawingViewTestPlatform.CreateView(200, 100);
			view.Initialize();
			container.AddSubview(view);
			root.AddSubview(container);
			root.AddSubview(sibling);
			return (root, view, sibling);
		});

		await InvokeOnMainThreadAsync(async () => await root.AttachAndRun(() =>
		{
			var recorder = new GestureRecorder(view);

			Stroke(view, (10, 10), (30, 30));

			Assert.Single(recorder.Completed);
			Assert.True(sibling.ScrollEnabled);
			Assert.Empty(TryGetCachedScrollViews(view) ?? []);
		}));
	}

	// Catches: a gesture on a view without a superview throws
	[Fact]
	public async Task GestureOnAViewWithoutASuperviewWorks()
	{
		SetupBuilder();
		await InvokeOnMainThreadAsync(() =>
		{
			var view = DrawingViewTestPlatform.CreateView(200, 100);
			view.Initialize();
			var recorder = new GestureRecorder(view);

			Assert.Null(view.Superview);
			Stroke(view, (10, 10), (30, 30));

			Assert.Single(recorder.Completed);
			Assert.Empty(TryGetCachedScrollViews(view) ?? []);
		});
	}

	// Catches: Dispose(true) does not release the native object (base.Dispose) or the in-progress path
	[Fact]
	public async Task DisposeReleasesTheNativeHandle()
	{
		SetupBuilder();
		await InvokeOnMainThreadAsync(() =>
		{
			var view = DrawingViewTestPlatform.CreateView(100, 100);
			view.Initialize();
			var cachedPlatformPath = new PlatformPathProbe();
			GetCurrentPath(view).PlatformPath = cachedPlatformPath;
			Assert.NotEqual(ObjCRuntime.NativeHandle.Zero, view.Handle);

			view.Dispose();

			Assert.Equal(ObjCRuntime.NativeHandle.Zero, view.Handle);
			Assert.True(cachedPlatformPath.IsDisposed, "Dispose(true) did not dispose the in-progress PathF.");
		});
	}

	// Catches: the finalizer path (Dispose(false)) releases the native object or disposes the in-progress path (a managed resource)
	[Fact]
	public async Task DisposeWithoutDisposingKeepsTheNativeHandle()
	{
		SetupBuilder();
		await InvokeOnMainThreadAsync(() =>
		{
			var probe = new DisposeProbe { Frame = new CGRect(0, 0, 100, 100) };
			probe.Initialize();
			var cachedPlatformPath = new PlatformPathProbe();
			GetCurrentPath(probe).PlatformPath = cachedPlatformPath;

			probe.CallDispose(false);

			Assert.NotEqual(ObjCRuntime.NativeHandle.Zero, probe.Handle);
			Assert.False(cachedPlatformPath.IsDisposed, "Dispose(false) disposed the in-progress PathF.");
			probe.Dispose();
			Assert.True(cachedPlatformPath.IsDisposed);
		});
	}

	// A second touch that begins while a gesture is in progress (another finger) finds the scroll views already cached: walking the ancestors
	// again would add them twice.
	// Catches: DetectScrollViews without its early return for a cache that is already filled.
	[Fact]
	public Task SecondTouchDuringAGestureDoesNotCacheTheParentScrollViewsAgain() => WithScrollParents((view, outer, inner) =>
	{
		DrawingViewTestPlatform.TouchDown(view, 20, 20);
		DrawingViewTestPlatform.TouchDown(view, 30, 30);

		var cached = TryGetCachedScrollViews(view);
		Skip.If(cached is null, "MauiDrawingView no longer keeps the ancestor scroll views in its scrollViewParents field, so there is no cache that could hold duplicates.");
		Assert.Equal(2, cached.Count);
		Assert.Equal(cached.Distinct().Count(), cached.Count);

		DrawingViewTestPlatform.TouchUp(view, 30, 30);
		Assert.True(outer.ScrollEnabled);
		Assert.True(inner.ScrollEnabled);
	});

	// MovedToWindow raises AttachedToWindow on the next turn of the main loop (as MAUI raises Loaded). A view that left the window again in the
	// meantime is not attached any more, so the event (which makes a waiting DrawingViewHandler listen again) is not raised for it.
	// Catches: the callback of MovedToWindow raising AttachedToWindow without checking that the view is still in a window.
	[Fact]
	public async Task AttachedToWindowIsNotRaisedForAViewThatLeftTheWindowBeforeTheEvent()
	{
		SetupBuilder();
		var window = TestWindow.PlatformWindow;
		var (kept, gone) = await InvokeOnMainThreadAsync(() => (DrawingViewTestPlatform.CreateView(200, 100), DrawingViewTestPlatform.CreateView(200, 100)));
		var keptRaised = new AttachedToWindowRecorder(kept);
		var goneRaised = new AttachedToWindowRecorder(gone);

		try
		{
			await InvokeOnMainThreadAsync(() =>
			{
				window.AddSubview(kept);
				window.AddSubview(gone);
				gone.RemoveFromSuperview();
			});

			await AssertionExtensions.AssertEventually(() => keptRaised.Count == 1, timeout: 3000, message: "The view that stayed in the window never raised AttachedToWindow.");
			await InvokeOnMainThreadAsync(() => { }); // runs after every callback queued before it
			Assert.Equal(0, goneRaised.Count);
		}
		finally
		{
			await InvokeOnMainThreadAsync(() =>
			{
				kept.RemoveFromSuperview();
				gone.RemoveFromSuperview();
			});
		}
	}

	sealed class DisposeProbe : MauiDrawingView
	{
		public void CallDispose(bool disposing) => Dispose(disposing);
	}

	// The drawing view inside an inner UIScrollView inside an outer UIScrollView, both hosted in the runner's window.
	async Task WithScrollParents(Action<MauiDrawingView, UIScrollView, UIScrollView> body)
	{
		SetupBuilder();
		var (outer, inner, view) = await InvokeOnMainThreadAsync(() =>
		{
			var outer = new UIScrollView { Frame = new CGRect(0, 0, 300, 300) };
			var inner = new UIScrollView { Frame = new CGRect(0, 0, 250, 250) };
			var view = DrawingViewTestPlatform.CreateView(200, 100);
			view.Initialize();
			inner.AddSubview(view);
			outer.AddSubview(inner);
			return (outer, inner, view);
		});

		await InvokeOnMainThreadAsync(async () => await outer.AttachAndRun(() => body(view, outer, inner)));
	}

	// The UIScrollView ancestors that MauiDrawingView.macios.cs keeps in its private scrollViewParents field while a gesture is in progress, read defensively:
	// another implementation may change the field, for example to weak references, or remove it. Weak references are resolved to their targets;
	// a missing field, null or a value that is not a collection gives null (no readable cache), and the callers then keep only their assertions on
	// observable behavior (UIScrollView.ScrollEnabled, the gesture events, the leak checks).
	static List<object>? TryGetCachedScrollViews(MauiDrawingView view) =>
		typeof(MauiDrawingView).GetField("scrollViewParents", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(view) is IEnumerable cache
			? cache.Cast<object?>().Select(item => item is WeakReference<UIScrollView> weak ? (weak.TryGetTarget(out var target) ? target : null) : item).OfType<object>().ToList()
			: null;
}