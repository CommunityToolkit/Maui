using System.Collections.Concurrent;
using System.Reflection;
using CommunityToolkit.Maui.Core.Views;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class MauiDrawingViewTests
{
	// The pointer overrides of MauiDrawingView (OnPointerPressed, OnPointerMoved, OnPointerReleased and OnPointerCanceled) read a
	// PointerRoutedEventArgs, which WinUI cannot construct, and the test app cannot inject pointer input (InputInjector needs the restricted
	// capability inputInjectionBrokered). The shared OnStart/OnMoving/OnFinish/OnCancel logic that they call is covered through reflection by
	// the other tests; the DrawingView page of the Sample logs each pointer event while a person draws.

	// Catches: Initialize on an old Windows version assigns the drawable, or throws
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

	// Catches: Initialize does not assign a DrawingViewDrawable on supported Windows versions
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

	// Catches: Dispose does not mark the view disposed, does not dispose the in-progress path, or runs again on a second call
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

	// Catches: Dispose(false) is not recorded or disposes the in-progress path, or a later Dispose(true) is not ignored
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

	// Catches: the finalizer disposes managed resources (calls Dispose(true) instead of Dispose(false))
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

	sealed class DisposeProbe : MauiDrawingView
	{
		public void CallDispose(bool disposing) => Dispose(disposing);
	}

	// Records every Dispose(bool) call, including the one made by the finalizer on the finalizer thread.
	sealed class FinalizerProbe : MauiDrawingView
	{
		public static ConcurrentQueue<bool> Calls { get; } = new();

		protected override void Dispose(bool disposing)
		{
			Calls.Enqueue(disposing);
			base.Dispose(disposing);
		}
	}
}