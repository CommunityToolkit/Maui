using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using AVFoundation;
using AVKit;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using CoreGraphics;
using Microsoft.Extensions.Logging;
using ObjCRuntime;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// MediaElementHandler.macios.cs and the Apple parts of MediaElementHandler.shared.cs / AppBuilderExtensions.shared.cs: creating the
// MauiMediaElement and its MediaManager, disconnecting and disposing, and the property/command mapping methods without a manager.
[Category(TestCategory.MediaElementApple)]
[Collection(TestCollections.DisableParallelization)]
public class MediaElementHandlerAppleTests : AppleMediaElementTestBase
{
	[Fact]
	public async Task HandlerCreatesAMauiMediaElementWithAMediaManager()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, handler =>
		{
			var platformView = Assert.IsType<MauiMediaElement>(handler.PlatformView);
			var controller = handler.Manager!.NativePlayerViewController!;
			Assert.Same(controller.View, Assert.Single(platformView.Subviews));
			Assert.Same(handler.Manager.NativePlayer, controller.Player);
			Assert.NotNull(controller.ParentViewController);
			return Task.CompletedTask;
		});
	}

	[Fact]
	public async Task CreatePlatformViewThrowsWithoutMauiContext()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			var handler = new AppleMediaElementHandler();

			var exception = Assert.Throws<InvalidOperationException>(handler.CreatePlatformViewDirectly);

			Assert.Contains("MauiContext cannot be null", exception.Message);
		});
	}

	[Fact]
	public async Task CreatePlatformViewThrowsOnAThreadWithoutDispatcher()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var handler = new AppleMediaElementHandler();
		handler.SetMauiContext(MauiContext);

		// A thread-pool thread has no dispatcher; the check happens before anything native is created.
		var exception = await Task.Run(() => Assert.Throws<InvalidOperationException>(() => handler.SetVirtualView(mediaElement)));

		Assert.Contains("IDispatcher cannot be null", exception.Message);
		Assert.Null(handler.ManagerOrNull);
	}

	[Fact]
	public async Task CreatePlatformViewReusesAnExistingMediaManager()
	{
		SetupApple();
		var pageController = new UIViewController();
		var pageView = new UIView();
		var mediaElement = CreateMediaElement();
		var page = new ContentPage { Content = mediaElement, Handler = new AppleStubElementHandler(pageView) };
		AppleMediaManager? existingManager = null;

		try
		{
			await InvokeOnMainThreadAsync(() =>
			{
				pageController.View!.AddSubview(pageView);
				existingManager = new AppleMediaManager(MauiContext, mediaElement, CommunityToolkit.Maui.DeviceTests.Runners.TestDispatcher.Current);
				var handler = new AppleMediaElementHandler { ManagerOrNull = existingManager };
				handler.SetMauiContext(MauiContext);

				handler.SetVirtualView(mediaElement);

				Assert.Same(existingManager, handler.ManagerOrNull);
				Assert.NotNull(existingManager.NativePlayer);
				Assert.IsType<MauiMediaElement>(handler.PlatformView);
				handler.Disconnect();
			});
		}
		finally
		{
			await InvokeOnMainThreadAsync(() =>
			{
				existingManager?.Dispose();
				page.Handler = null;
				mediaElement.Dispose();
			});
		}
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(true, false)]
	[InlineData(false, true)]
	[InlineData(true, true)]
	public async Task HandlerUsesTheMappersItIsConstructedWithAndTheDefaultMappersForNull(bool customPropertyMapper, bool customCommandMapper)
	{
		// The handler factory creates the handler with the mappers of the static properties of AppleMappedMediaElementHandler.
		SetupApple(builder => builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<MediaElement, AppleMappedMediaElementHandler>()));
		var customCalls = new List<string>();
		AppleMappedMediaElementHandler.PropertyMapperToUse = customPropertyMapper
			? new Microsoft.Maui.PropertyMapper<MediaElement, MediaElementHandler>(MediaElementHandler.PropertyMapper)
			{
				[nameof(IMediaElement.ShouldShowPlaybackControls)] = (_, _) => customCalls.Add("Controls")
			}
			: null;
		AppleMappedMediaElementHandler.CommandMapperToUse = customCommandMapper
			? new Microsoft.Maui.CommandMapper<MediaElement, MediaElementHandler>(MediaElementHandler.CommandMapper)
			{
				[nameof(MediaElement.StopRequested)] = (_, _, _) => customCalls.Add("Stop")
			}
			: null;
		var mediaElement = CreateMediaElement();
		mediaElement.ShouldShowPlaybackControls = false;

		try
		{
			await HostAsync(mediaElement, async handler =>
			{
				Assert.IsType<AppleMappedMediaElementHandler>(handler);
				var controller = handler.Manager!.NativePlayerViewController!;
				await handler.Manager.TakeSourceUpdates();

				// Creating the controller applied ShouldShowPlaybackControls (#3222), and connecting the handler applied the property mapper.
				Assert.False(controller.ShowsPlaybackControls);
				Assert.Equal(customPropertyMapper, customCalls.Contains("Controls"));

				// A change reaches the property mapper: the default one shows the playback controls of the controller, a custom one that
				// replaces that mapping leaves them hidden.
				var controlsCalls = customCalls.Count(static call => call is "Controls");
				mediaElement.ShouldShowPlaybackControls = true;
				Assert.Equal(!customPropertyMapper, controller.ShowsPlaybackControls);
				Assert.Equal(customPropertyMapper ? controlsCalls + 1 : 0, customCalls.Count(static call => call is "Controls"));

				// Stop() reaches the command mapper: the default one stops the player, a custom one that replaces it does not.
				Assert.Equal(MediaElementState.None, mediaElement.CurrentState);
				mediaElement.Stop();
				Assert.Equal(customCommandMapper ? MediaElementState.None : MediaElementState.Stopped, mediaElement.CurrentState);
				Assert.Equal(customCommandMapper, customCalls.Contains("Stop"));
			});
		}
		finally
		{
			AppleMappedMediaElementHandler.PropertyMapperToUse = null;
			AppleMappedMediaElementHandler.CommandMapperToUse = null;
		}
	}

	[Fact]
	public async Task DisconnectHandlerReleasesThePlayerAndTheManager()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			await handler.Manager!.TakeSourceUpdates();
			var manager = handler.Manager;
			var player = manager.NativePlayer!;
			var controller = manager.NativePlayerViewController!;

			handler.Disconnect();

			Assert.Equal(NativeHandle.Zero, player.Handle);
			Assert.Equal(NativeHandle.Zero, controller.Handle);
			Assert.Null(manager.NativePlayer);
			Assert.Null(handler.ManagerOrNull);
		});
	}

	[Fact]
	public async Task DisconnectHandlerRemovesThePlayerViewControllerFromItsParentViewController()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			await handler.Manager!.TakeSourceUpdates();
			var controller = handler.Manager.NativePlayerViewController!;
			var parent = controller.ParentViewController!;
			var controllerHandle = controller.Handle;
			Assert.Contains(parent.ChildViewControllers, child => child.Handle == controllerHandle);

			handler.Disconnect();

			// UIKit containment: a child view controller that is no longer used has to be removed from its parent, otherwise every
			// MediaElement that is added to and removed from a live page leaves its AVPlayerViewController behind.
			Assert.DoesNotContain(parent.ChildViewControllers, child => child.Handle == controllerHandle);
		});
	}

	[Fact]
	public async Task MediaElementPlaysWithTheNewHandlerAfterTheOldHandlerWasDisconnected()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var page = new ContentPage { Content = new VerticalStackLayout { mediaElement } };

		try
		{
			await AttachAndRun(page, async _ =>
			{
				await page.OnLoadedAsync();
				var first = (AppleMediaElementHandler)mediaElement.Handler!;
				await LoadAsync(mediaElement, first, ResourceSource(VideoResource));
				var firstPlayer = first.Manager!.NativePlayer!;

				first.Disconnect();
				Assert.Equal(NativeHandle.Zero, firstPlayer.Handle);

				// The same MediaElement gets a new handler (for example when a recycled item is shown again): it keeps its Source, the new
				// handler builds a new manager and a new native player, and the media plays in it.
				var second = CreateHandler<AppleMediaElementHandler>(mediaElement);
				await second.Manager!.TakeSourceUpdates();
				await WaitUntilReadyAsync(second);
				var secondPlayer = second.Manager.NativePlayer!;
				Assert.NotSame(firstPlayer, secondPlayer);
				Assert.NotEqual(NativeHandle.Zero, secondPlayer.Handle);

				mediaElement.Play();

				await WaitUntilAsync(() => secondPlayer.CurrentTime.Seconds > 0.3, "The playhead of the new player did not advance.");
				Assert.True(secondPlayer.Rate > 0);
			});
		}
		finally
		{
			await InvokeOnMainThreadAsync(() =>
			{
				mediaElement.Handler?.DisconnectHandler();
				page.Handler?.DisconnectHandler();
				mediaElement.Dispose();
			});
		}
	}

	[Fact]
	public async Task DisposeWithoutDisposingKeepsTheManager()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			await handler.Manager!.TakeSourceUpdates();
			var manager = handler.Manager;
			var player = manager.NativePlayer!;

			handler.DisposeCore(disposing: false);

			Assert.Same(manager, handler.ManagerOrNull);
			Assert.Same(player, manager.NativePlayer);
			Assert.NotEqual(NativeHandle.Zero, player.Handle);
		});
	}

	[Fact]
	public async Task DisposeReleasesTheManagerAndThePlayerViewControllerAndCanRunTwice()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			await handler.Manager!.TakeSourceUpdates();
			var manager = handler.Manager;
			var player = manager.NativePlayer!;
			var controller = manager.NativePlayerViewController!;

			handler.DisposeCore(disposing: true);

			Assert.Null(handler.ManagerOrNull);
			Assert.Equal(NativeHandle.Zero, player.Handle);
			Assert.Equal(NativeHandle.Zero, controller.Handle);

			// The second call finds neither a manager nor a player view controller.
			Assert.Null(Record.Exception(() => handler.DisposeCore(disposing: true)));
		});
	}

	[Fact]
	public async Task DisposeOfTheHandlerReleasesTheManagerAndThePlayerViewController()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			await handler.Manager!.TakeSourceUpdates();
			var manager = handler.Manager;
			var player = manager.NativePlayer!;
			var controller = manager.NativePlayerViewController!;

			handler.Dispose();

			Assert.Null(handler.ManagerOrNull);
			Assert.Equal(NativeHandle.Zero, player.Handle);
			Assert.Equal(NativeHandle.Zero, controller.Handle);
		});
	}

	[Fact]
	public async Task PlayerViewFollowsTheSizeOfThePlatformView()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			await handler.Manager!.TakeSourceUpdates();
			var platformView = handler.PlatformView;
			var playerView = handler.Manager.NativePlayerViewController!.View!;
			await WaitUntilAsync(() => platformView.Bounds.Size == new CGSize(200, 100), "The platform view was not laid out.");
			Assert.Equal(new CGSize(200, 100), playerView.Frame.Size);

			mediaElement.WidthRequest = 120;
			mediaElement.HeightRequest = 60;

			await WaitUntilAsync(() => platformView.Bounds.Size == new CGSize(120, 60), "The platform view was not resized.");
			Assert.Equal(new CGSize(120, 60), playerView.Frame.Size);
		});
	}

	[Theory]
	[InlineData(nameof(MediaElementHandler.MapAspect))]
	[InlineData(nameof(MediaElementHandler.MapShouldShowPlaybackControls))]
	[InlineData(nameof(MediaElementHandler.MapSource))]
	[InlineData(nameof(MediaElementHandler.MapSpeed))]
	[InlineData(nameof(MediaElementHandler.MapVolume))]
	[InlineData(nameof(MediaElementHandler.MapShouldKeepScreenOn))]
	[InlineData(nameof(MediaElementHandler.MapShouldMute))]
	[InlineData(nameof(MediaElementHandler.MapStatusUpdated))]
	[InlineData(nameof(MediaElementHandler.MapPlayRequested))]
	[InlineData(nameof(MediaElementHandler.MapPauseRequested))]
	[InlineData(nameof(MediaElementHandler.MapStopRequested))]
	public async Task MappingWithoutAMediaManagerDoesNothing(string mapMethod)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			await handler.Manager!.TakeSourceUpdates();
			handler.Disconnect();
			Assert.Null(handler.ManagerOrNull);

			var exception = Record.Exception(() =>
			{
				switch (mapMethod)
				{
					case nameof(MediaElementHandler.MapAspect): MediaElementHandler.MapAspect(handler, mediaElement); break;
					case nameof(MediaElementHandler.MapShouldShowPlaybackControls): MediaElementHandler.MapShouldShowPlaybackControls(handler, mediaElement); break;
					case nameof(MediaElementHandler.MapSource): MediaElementHandler.MapSource(handler, mediaElement); break;
					case nameof(MediaElementHandler.MapSpeed): MediaElementHandler.MapSpeed(handler, mediaElement); break;
					case nameof(MediaElementHandler.MapVolume): MediaElementHandler.MapVolume(handler, mediaElement); break;
					case nameof(MediaElementHandler.MapShouldKeepScreenOn): MediaElementHandler.MapShouldKeepScreenOn(handler, mediaElement); break;
					case nameof(MediaElementHandler.MapShouldMute): MediaElementHandler.MapShouldMute(handler, mediaElement); break;
					case nameof(MediaElementHandler.MapStatusUpdated): MediaElementHandler.MapStatusUpdated(handler, mediaElement, null); break;
					case nameof(MediaElementHandler.MapPlayRequested): MediaElementHandler.MapPlayRequested(handler, mediaElement, null); break;
					case nameof(MediaElementHandler.MapPauseRequested): MediaElementHandler.MapPauseRequested(handler, mediaElement, null); break;
					case nameof(MediaElementHandler.MapStopRequested): MediaElementHandler.MapStopRequested(handler, mediaElement, null); break;
					default: throw new ArgumentOutOfRangeException(nameof(mapMethod), mapMethod, null);
				}
			});

			Assert.Null(exception);
			Assert.Null(handler.ManagerOrNull);
		});
	}

	[Fact]
	public async Task SeekRequestWithoutAMediaManagerStillCompletesTheSeekTask()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			await handler.Manager!.TakeSourceUpdates();
			handler.Disconnect();
			var seekCompleted = ((IAsynchronousMediaElementHandler)mediaElement).SeekCompletedTCS.Task;
			Assert.False(seekCompleted.IsCompleted);

			MediaElementHandler.MapSeekRequested(handler, mediaElement, new MediaSeekRequestedEventArgs(TimeSpan.FromSeconds(1)));

			await seekCompleted.WaitAsync(TimeSpan.FromSeconds(2));
		});
	}

	[Fact]
	public async Task SeekRequestWithoutArgumentsThrowsArgumentNullException()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			await handler.Manager!.TakeSourceUpdates();
			var context = new AppleCapturingSynchronizationContext();

			// MapSeekRequested is async void: the exception is posted to the synchronization context.
			context.Run(() => MediaElementHandler.MapSeekRequested(handler, mediaElement, null));

			var exception = Assert.IsType<ArgumentNullException>(context.Captured);
			Assert.Equal("args", exception.ParamName);
		});
	}

	[Fact]
	public async Task SeekRequestWithArgumentsOfAnotherTypeDoesNothing()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			await handler.Manager!.TakeSourceUpdates();
			var context = new AppleCapturingSynchronizationContext();

			context.Run(() => MediaElementHandler.MapSeekRequested(handler, mediaElement, new EventArgs()));

			Assert.Null(context.Captured);
		});
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task SeekToBeforeTheMediaIsReadyNeitherCrashesNorHangs(bool withSource)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			await handler.Manager!.TakeSourceUpdates();
			if (withSource)
			{
				// Set in the same main-thread turn as the seek: the player has the item but is not ready to play yet.
				mediaElement.Source = ResourceSource(VideoResource);
			}

			// The capturing context keeps the exception that MapSeekRequested (async void) would otherwise rethrow on the UI thread.
			var context = new AppleCapturingSynchronizationContext();
			Task seek = Task.CompletedTask;
			context.Run(() => seek = mediaElement.SeekTo(TimeSpan.FromSeconds(1)));

			// SeekTo may complete or throw, but it must end and must not crash the app.
			var exception = await Record.ExceptionAsync(() => seek.WaitAsync(TimeSpan.FromSeconds(3)));

			Assert.Null(context.Captured);
			Assert.IsNotType<TimeoutException>(exception);
			await Record.ExceptionAsync(handler.Manager.TakeSourceUpdates);
		});
	}

	// A seek that Stop() interrupts: AVPlayer reports it as not finished, inside a native callback. The seek has ended all the same: SeekTo, which
	// goes through the async void MapSeekRequested, completes and raises SeekCompleted, as on Android, and nothing reaches the UI thread.
	[Fact]
	public async Task SeekToInterruptedByStopCompletesAndRaisesSeekCompleted()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.ShouldMute = true;
		var events = new MediaElementEvents(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			await LoadAsync(mediaElement, handler, ResourceSource("avplayer/long.m4a"));

			// The capturing context keeps an exception that MapSeekRequested (async void) would otherwise rethrow on the UI thread.
			var context = new AppleCapturingSynchronizationContext();
			Task seek = Task.CompletedTask;
			context.Run(() => seek = mediaElement.SeekTo(TimeSpan.FromSeconds(20)));
			mediaElement.Stop();

			var exception = await Record.ExceptionAsync(() => seek.WaitAsync(TimeSpan.FromSeconds(3)));
			await DrainMainQueueAsync();

			Assert.Null(exception);
			Assert.Null(context.Captured);
			Assert.Equal(1, events.SeekCompleted);
			Assert.Equal(MediaElementState.Stopped, mediaElement.CurrentState);
		});
	}

	// SeekTo stops waiting when its token is cancelled, and the seek goes on. When that seek fails later (here the SeekCompleted handler of the app
	// throws, which fails the platform seek), nothing awaits the task of the seek request any more: the failure must not fault it, which would
	// raise TaskScheduler.UnobservedTaskException (apps log it, or crash on it) once the task is collected.
	[Fact]
	public async Task FailureOfASeekAfterItsTokenWasCancelledLeavesNoUnobservedTaskException()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.ShouldMute = true;
		var failureMessage = $"The SeekCompleted handler of the app failed ({Guid.NewGuid():N}).";
		var unobservedFailures = new ConcurrentQueue<AggregateException>();
		void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
		{
			// The handler's exception comes wrapped (MAUI's WeakEventManager invokes handlers through reflection): the text of the whole exception
			// holds the unique message
			if (e.Exception.ToString().Contains(failureMessage, StringComparison.Ordinal))
			{
				unobservedFailures.Enqueue(e.Exception);
				e.SetObserved();
			}
		}

		TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
		try
		{
			WeakReference? seekRequestTask = null;
			await HostAsync(mediaElement, async handler =>
			{
				await LoadAsync(mediaElement, handler, ResourceSource("avplayer/long.m4a"));
				var seekEnded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
				mediaElement.SeekCompleted += (_, _) =>
				{
					seekEnded.TrySetResult();
					throw new InvalidOperationException(failureMessage);
				};

				using var cancellation = new CancellationTokenSource();
				var seek = mediaElement.SeekTo(TimeSpan.FromSeconds(20), cancellation.Token);
				seekRequestTask = GetSeekRequestTask(mediaElement);
				cancellation.Cancel();

				await Assert.ThrowsAnyAsync<OperationCanceledException>(() => seek);
				await WaitAsync(seekEnded.Task, "The seek never ended.");
				await DrainMainQueueAsync();
			});

			// Collecting the task of the seek request runs the finalizer that reports an exception nothing observed
			await AssertionExtensions.WaitForGC(seekRequestTask!);
			GC.WaitForPendingFinalizers();

			Assert.Empty(unobservedFailures);
		}
		finally
		{
			TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
		}
	}

	// A separate method, so that no local of the test keeps the task alive.
	[MethodImpl(MethodImplOptions.NoInlining)]
	static WeakReference GetSeekRequestTask(MediaElement mediaElement) => new(((IAsynchronousMediaElementHandler)mediaElement).SeekCompletedTCS.Task);

	[Fact]
	public void PropertyMapperMapsTheMediaElementProperties()
	{
		// Looping has no mapping on iOS and Mac Catalyst (MediaManager's played-to-end handling implements it); the #if that leaves it out
		// is compile-time only (the mapping method does not exist in the Apple handler), so the test checks the mappings that exist.
		var keys = MediaElementHandler.PropertyMapper.GetKeys().ToList();

		Assert.Contains(nameof(IMediaElement.Aspect), keys);
		Assert.Contains(nameof(IMediaElement.ShouldShowPlaybackControls), keys);
		Assert.Contains(nameof(IMediaElement.Source), keys);
		Assert.Contains(nameof(IMediaElement.Speed), keys);
		Assert.Contains(nameof(IMediaElement.Volume), keys);
		Assert.Contains(nameof(IMediaElement.ShouldKeepScreenOn), keys);
		Assert.Contains(nameof(IMediaElement.ShouldMute), keys);
	}

	[Fact]
	public async Task UseMauiCommunityToolkitMediaElementRegistersTheToolkitHandler()
	{
		// No test double: the handler the toolkit registers.
		SetupBuilder();
		var mediaElement = CreateMediaElement();
		var page = new ContentPage { Content = mediaElement };

		Assert.Equal(typeof(MediaElementHandler), MauiContext.Handlers.GetHandlerType(typeof(MediaElement)));
		try
		{
			await AttachAndRun(page, async _ =>
			{
				await page.OnLoadedAsync();

				var handler = Assert.IsType<MediaElementHandler>(mediaElement.Handler);
				var platformView = Assert.IsType<MauiMediaElement>(handler.PlatformView);

				// The product handler starts without a MediaManager, so CreatePlatformView created one (the test double handler assigns its
				// own manager before it calls the product's CreatePlatformView, which then keeps it): exactly the toolkit's MediaManager,
				// whose player view controller's view is what the platform view hosts.
				var manager = Assert.IsType<MediaManager>(mediaElementHandlerManager.GetValue(handler));
				var controller = (AVPlayerViewController)mediaManagerPlayerViewController.GetValue(manager)!;
				Assert.Same(controller.View, Assert.Single(platformView.Subviews));
			});
		}
		finally
		{
			await InvokeOnMainThreadAsync(() =>
			{
				mediaElement.Handler?.DisconnectHandler();
				page.Handler?.DisconnectHandler();
				mediaElement.Dispose();
			});
		}
	}

	static readonly System.Reflection.PropertyInfo mediaElementHandlerManager =
		typeof(MediaElementHandler).GetProperty("MediaManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

	static readonly System.Reflection.PropertyInfo mediaManagerPlayerViewController =
		typeof(MediaManager).GetProperty("PlayerViewController", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

	[Fact]
	public async Task AndroidOnlyOptionsDoNotChangeTheAppleHandler()
	{
		SetupBuilder(isAndroidForegroundServiceEnabled: true, configureMediaElementOptions: options => options.SetDefaultAndroidViewType(AndroidViewType.TextureView));
		var mediaElement = CreateMediaElement();
		var page = new ContentPage { Content = mediaElement };

		Assert.True(MediaElementOptions.IsAndroidForegroundServiceEnabled);
		Assert.Equal(AndroidViewType.TextureView, MediaElementOptions.DefaultAndroidViewType);
		try
		{
			await AttachAndRun(page, async _ =>
			{
				await page.OnLoadedAsync();

				Assert.IsType<MauiMediaElement>(mediaElement.Handler!.PlatformView);
			});
		}
		finally
		{
			await InvokeOnMainThreadAsync(() =>
			{
				mediaElement.Handler?.DisconnectHandler();
				page.Handler?.DisconnectHandler();
				mediaElement.Dispose();
			});
		}
	}

	[Fact]
	public async Task MediaElementHandlerAndItsObjectsDoNotLeak()
	{
		SetupApple();
		WeakReference mediaElementReference = null!, pageReference = null!, handlerReference = null!, platformViewReference = null!, managerReference = null!;

		await InvokeOnMainThreadAsync(() =>
		{
			var mediaElement = CreateMediaElement();
			var page = new ContentPage { Content = new VerticalStackLayout { mediaElement } };
			CreateHandler(page);
			var handler = (AppleMediaElementHandler)mediaElement.Handler!;
			mediaElementReference = new(mediaElement);
			pageReference = new(page);
			handlerReference = new(handler);
			platformViewReference = new(handler.PlatformView);
			managerReference = new(handler.Manager);

			handler.Disconnect();
			page.Handler?.DisconnectHandler();
			mediaElement.Handler = null;
			page.Handler = null;
			mediaElement.Dispose();
		});

		await AssertionExtensions.WaitForGC(mediaElementReference, pageReference, handlerReference, platformViewReference, managerReference);
	}
}