using System.Runtime.CompilerServices;
using AndroidX.Camera.Lifecycle;
using CommunityToolkit.Maui.Extensions;
using Google.Common.Util.Concurrent;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.ListenableFutureExtensionsAndroid)]
[Collection(TestCollections.PermanentAndroidGrants)]
public class ListenableFutureExtensionsAndroidTests : CameraAndroidTestBase
{
	[Fact]
	public async Task AddListenerOnExecutorKeepsTheFutureAliveWhileJavaAddsTheListener()
	{
		await GetNativeCameraProviderAsync();

		var executor = new GarbageCollectingExecutor();
		var (futureReference, originalHandle) = AddListenerToAnUnreferencedFuture(executor);

		Assert.True(executor.WasExecutedWhileAddingListener, "The future was not complete when its listener was added, so the test could not check anything.");
		Assert.True(futureReference.TryGetTarget(out var future), "The future was collected while Java added its listener.");
		Assert.Equal(originalHandle, future.Handle);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static (WeakReference<IListenableFuture> FutureReference, nint OriginalHandle) AddListenerToAnUnreferencedFuture(GarbageCollectingExecutor executor)
	{
		var future = ProcessCameraProvider.GetInstance(AppContext);
		var futureReference = new WeakReference<IListenableFuture>(future);
		var originalHandle = future.Handle;
		executor.IsAddingListener = true;
		future.AddListenerOnExecutor(executor, static () => { });
		executor.IsAddingListener = false;
		return (futureReference, originalHandle);
	}

	sealed class GarbageCollectingExecutor : Java.Lang.Object, Java.Util.Concurrent.IExecutor
	{
		public bool IsAddingListener { get; set; }

		public bool WasExecutedWhileAddingListener { get; private set; }

		public void Execute(Java.Lang.IRunnable? command)
		{
			WasExecutedWhileAddingListener = IsAddingListener;

			// The GC bridge swaps JNI references on its own thread after the GC returns (WaitForGCBridgeProcessing is empty on NativeAOT); a round takes ~30-60 ms on CI emulators
			for (var round = 0; round < 3; round++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				Thread.Sleep(TimeSpan.FromMilliseconds(250));
			}

			command?.Run();
		}
	}
}