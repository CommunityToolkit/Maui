using Android.Content;
using AndroidX.Core.Content;
using Google.Common.Util.Concurrent;
using Java.Util.Concurrent;

namespace CommunityToolkit.Maui.Extensions;

static class ListenableFutureExtensions
{
	public static void AddListenerOnMainThread(this IListenableFuture future, Context context, Action listener) =>
		future.AddListenerOnExecutor(ContextCompat.GetMainExecutor(context), listener);

	// Guava 33 (brought by Media3, so by MediaElement) resolves the 2022 ListenableFuture binding 9999.0.0, whose AddListener and Get keep neither
	// the future nor their arguments alive during the JNI call: under NativeAOT a GC there lets the GC bridge delete their JNI references
	public static void AddListenerOnExecutor(this IListenableFuture future, IExecutor? executor, Action listener)
	{
		var runnable = new Java.Lang.Runnable(() =>
		{
			try
			{
				listener();
			}
			finally
			{
				GC.KeepAlive(future);
			}
		});

		future.AddListener(runnable, executor);

		GC.KeepAlive(future);
		GC.KeepAlive(runnable);
		GC.KeepAlive(executor);
	}
}