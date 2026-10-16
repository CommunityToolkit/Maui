using System.Diagnostics;
using Xunit.Sdk;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;
#if IOS || MACCATALYST
using PlatformView = UIKit.UIView;
#elif ANDROID
using Microsoft.Maui.Platform;
using ABitmap = Android.Graphics.Bitmap;
using ACanvas = Android.Graphics.Canvas;
using PlatformView = Android.Views.View;
#elif WINDOWS
using PlatformView = Microsoft.UI.Xaml.FrameworkElement;
#endif

namespace CommunityToolkit.Maui.DeviceTests;

// At the app root because tests of several folders use these helpers.
static class VisualBehaviorHelpers
{
	// The pixels are polled because an image loads asynchronously.
	public static Task WaitForPixelsAsync(PlatformView platformView, IMauiContext mauiContext, Color expected, params Color[] unexpected) =>
		AssertEventually(() => ShowsAsync(platformView, mauiContext, expected, unexpected), timeout: 3000, message: $"The rendered view does not show {expected}{(unexpected.Length is 0 ? string.Empty : $" without {string.Join(", ", unexpected)}")}.");

#if ANDROID
	// ToBitmap waits for a layout pass that never comes for a view that is already laid out: it always costs one second.
	// The view is hosted in the window and laid out, so draw it directly, and scan all its pixels at once (a view can be
	// as large as the screen, and one JNI call per pixel takes seconds).
	static Task<bool> ShowsAsync(PlatformView platformView, IMauiContext mauiContext, Color expected, Color[] unexpected)
	{
		_ = mauiContext;

		if (platformView.Width <= 0 || platformView.Height <= 0)
		{
			return Task.FromResult(false);
		}

		using var bitmap = ABitmap.CreateBitmap(platformView.Width, platformView.Height, ABitmap.Config.Argb8888!)!;
		using (var canvas = new ACanvas(bitmap))
		{
			platformView.Draw(canvas);
		}

		var pixels = new int[bitmap.Width * bitmap.Height];
		bitmap.GetPixels(pixels, 0, bitmap.Width, 0, 0, bitmap.Width, bitmap.Height);

		return Task.FromResult(Contains(pixels, expected) && unexpected.All(color => !Contains(pixels, color)));
	}

	// The same tolerance as the other pixel assertions.
	static bool Contains(int[] pixels, Color color)
	{
		var expected = color.ToPlatform();

		return pixels.Any(pixel => Math.Abs(Android.Graphics.Color.GetAlphaComponent(pixel) - expected.A) <= 1
			&& Math.Abs(Android.Graphics.Color.GetRedComponent(pixel) - expected.R) <= 1
			&& Math.Abs(Android.Graphics.Color.GetGreenComponent(pixel) - expected.G) <= 1
			&& Math.Abs(Android.Graphics.Color.GetBlueComponent(pixel) - expected.B) <= 1);
	}
#else
	static async Task<bool> ShowsAsync(PlatformView platformView, IMauiContext mauiContext, Color expected, Color[] unexpected)
	{
		try
		{
			var bitmap = await platformView.ToBitmap(mauiContext);
			await bitmap.AssertContainsColor(expected);

			foreach (var color in unexpected)
			{
				await bitmap.AssertDoesNotContainColor(color);
			}

			return true;
		}
		catch (XunitException)
		{
			return false;
		}
	}
#endif

	// Only for "nothing happens" assertions, which have no event to wait for: every positive wait uses AssertEventually.
	public static async Task AssertStaysFalseAsync(Func<bool> condition, string message, int windowMilliseconds = 300, int intervalMilliseconds = 25)
	{
		for (var elapsed = 0; elapsed <= windowMilliseconds; elapsed += intervalMilliseconds)
		{
			if (condition())
			{
				throw new XunitException(message);
			}

			await Task.Delay(intervalMilliseconds);
		}
	}

	// A forced collection frees what nothing holds at once; the timeout only keeps a busy machine from turning into a failure. What something
	// holds stays alive whatever the time, so the wait hides no leak.
	static readonly TimeSpan collectionTimeout = TimeSpan.FromSeconds(15);

	// AssertionExtensions.WaitForGC, hardened against two things that are no leak:
	// - The Mono runtime of Android, iOS and Mac Catalyst scans the stacks of its threads conservatively, and the stack of the collecting thread
	//   includes the frames of the collector itself. On iOS the whole test runs on the main thread, which hosts the views and then runs the
	//   collections, so a stale slot left there by the code that touched a view can keep that one object alive (seen once: the Image survived 40
	//   collections, its behavior and its native view did not). So every round collects on a thread of its own, whose stack has never held a view.
	// - On Android the managed peer of a native view lives until the Java side was collected too, which takes a moment. The rounds go on for
	//   collectionTimeout, and AssertionExtensions.WaitForGC then decides and names what is still alive.
	public static async Task WaitForGCAsync(params WeakReference[] references)
	{
		Assert.NotEmpty(references);

#if ANDROID
		for (var i = 0; i < 3; i++)
		{
			Java.Lang.Runtime.GetRuntime()?.Gc();
			await AssertionExtensions.Collect();
		}
#endif
		var stopwatch = Stopwatch.StartNew();

		do
		{
			await CollectOnNewThreadAsync();

			if (references.All(static reference => !reference.IsAlive))
			{
				return;
			}

			await Task.Delay(50);
		}
		while (stopwatch.Elapsed < collectionTimeout);

		await AssertionExtensions.WaitForGC(references);
	}

	// The collections of AssertionExtensions.Collect, on a thread that has run nothing else.
	static Task CollectOnNewThreadAsync()
	{
		var collected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		var thread = new Thread(() =>
		{
			try
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect(2, GCCollectionMode.Forced, true);
				GC.WaitForPendingFinalizers();
				GC.Collect(2, GCCollectionMode.Forced, true);
				collected.SetResult();
			}
			catch (Exception ex)
			{
				collected.SetException(ex);
			}
		})
		{
			IsBackground = true,
			Name = nameof(CollectOnNewThreadAsync)
		};
		thread.Start();

		return collected.Task;
	}
}