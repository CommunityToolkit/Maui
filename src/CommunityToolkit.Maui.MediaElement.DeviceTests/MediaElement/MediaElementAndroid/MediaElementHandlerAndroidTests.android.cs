using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Android.Graphics.Drawables;
using Android.Views;
using AndroidX.Media3.Common;
using AndroidX.Media3.UI;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using AColor = Android.Graphics.Color;
using ARelativeLayout = Android.Widget.RelativeLayout;
using PlayerConsts = AndroidX.Media3.Common.BasePlayer.InterfaceConsts;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.MediaElementAndroid)]
[Collection(TestCollections.DisableParallelization)]
public class MediaElementHandlerAndroidTests : MediaElementAndroidTestBase
{
	[Fact]
	public async Task HandlerBuildsPlayerViewInsideRelativeLayoutInsideMauiMediaElement()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();

		await HostAsync(mediaElement, handler =>
		{
			var platformView = Assert.IsType<MauiMediaElement>(handler.PlatformView);
			Assert.Equal(1, platformView.ChildCount);
			var relativeLayout = Assert.IsType<ARelativeLayout>(platformView.GetChildAt(0));
			Assert.Equal((ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent), (relativeLayout.LayoutParameters!.Width, relativeLayout.LayoutParameters.Height));
			Assert.Equal(1, relativeLayout.ChildCount);

			var playerView = Assert.IsType<PlayerView>(relativeLayout.GetChildAt(0));
			Assert.Equal((ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent), (playerView.LayoutParameters!.Width, playerView.LayoutParameters.Height));
			var background = Assert.IsType<ColorDrawable>(playerView.Background);
			Assert.Equal(AColor.Black, background.Color);
			Assert.False(playerView.UseController);
			Assert.False(playerView.ControllerAutoShow);

			var player = Assert.IsType<AndroidX.Media3.ExoPlayer.IExoPlayer>(playerView.Player, exactMatch: false);
			Assert.False(player.IsReleased);
			Assert.False(player.PlayWhenReady);
			Assert.Equal(PlayerConsts.StateIdle, player.PlaybackState);
			Assert.Equal(0, player.MediaItemCount);
		});
	}

	[Fact]
	public async Task HandlerUsesSurfaceViewByDefault()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();

		await HostAsync(mediaElement, handler =>
		{
			var playerView = GetPlayerView(mediaElement);

			Assert.Equal(AndroidViewType.SurfaceView, mediaElement.AndroidViewType);
			Assert.IsType<SurfaceView>(playerView.VideoSurfaceView);
			Assert.NotEqual(GetResourceId(playerView, "texture_view_media_element"), playerView.Id);
			Assert.Equal(AColor.Black, GetShutterColor(playerView));
		});
	}

	// The shutter color and the id come from Platforms/Android/Resources/layout/textureview.xml, which the handler inflates for a TextureView.
	[Fact]
	public async Task HandlerInflatesTextureViewLayoutWhenAndroidViewTypeIsTextureView()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(() => new MediaElement { AndroidViewType = AndroidViewType.TextureView, WidthRequest = 200, HeightRequest = 100 });

		await HostAsync(mediaElement, handler =>
		{
			var playerView = GetPlayerView(mediaElement);

			Assert.IsType<TextureView>(playerView.VideoSurfaceView);
			Assert.Equal(GetResourceId(playerView, "texture_view_media_element"), playerView.Id);
			Assert.Equal(AColor.White, GetShutterColor(playerView));
			Assert.False(playerView.UseController);
			Assert.False(playerView.ControllerAutoShow);
			Assert.Equal((ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent), (playerView.LayoutParameters!.Width, playerView.LayoutParameters.Height));
			Assert.IsType<AndroidX.Media3.ExoPlayer.IExoPlayer>(playerView.Player, exactMatch: false);
		});
	}

	[Fact]
	public async Task ConfiguredDefaultAndroidViewTypeDecidesTheViewTypeOfNewMediaElements()
	{
		SetupProbeBuilder(configureMediaElementOptions: options => options.SetDefaultAndroidViewType(AndroidViewType.TextureView));
		var mediaElement = await CreateMediaElementAsync();

		await HostAsync(mediaElement, handler =>
		{
			Assert.Equal(AndroidViewType.TextureView, mediaElement.AndroidViewType);
			Assert.IsType<TextureView>(GetPlayerView(mediaElement).VideoSurfaceView);
		});
	}

	[Fact]
	public async Task HandlerCreationThrowsNotSupportedForUnknownAndroidViewType()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(() => new MediaElement { AndroidViewType = (AndroidViewType)42, WidthRequest = 200, HeightRequest = 100 });

		var exception = await Assert.ThrowsAsync<NotSupportedException>(() => InvokeOnMainThreadAsync(() => CreateHandler<AndroidMediaElementProbeHandler>(mediaElement)));

		Assert.Contains("42", exception.Message);
		Assert.Contains("is not yet supported", exception.Message);
	}

	[Fact]
	public async Task CreatePlatformViewThrowsWithoutMauiContext()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => InvokeOnMainThreadAsync(() => new AndroidMediaElementProbeHandler().CreatePlatformViewDirectly()));

		Assert.Contains("MauiContext", exception.Message);
		GC.KeepAlive(mediaElement);
	}

	[Fact]
	public async Task HandlerCreationOffTheUiThreadThrowsBecauseThereIsNoDispatcher()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(() =>
		{
			var handler = new AndroidMediaElementProbeHandler();
			handler.SetMauiContext(MauiContext);
			handler.SetVirtualView(mediaElement);
		}));

		Assert.Contains("IDispatcher", exception.Message);
	}

	[Fact]
	public async Task CreatingThePlatformViewAgainReusesTheMediaManager()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			var manager = handler.Manager;
			var firstPlatformView = (MauiMediaElement)handler.PlatformView!;

			var secondPlatformView = handler.CreatePlatformViewDirectly();
			try
			{
				Assert.NotNull(manager);
				Assert.Same(manager, handler.Manager);
				Assert.NotSame(firstPlatformView, secondPlatformView);
				Assert.NotSame(GetPlayerView(firstPlatformView).Player, GetPlayerView(secondPlatformView).Player);
			}
			finally
			{
				// The second player has the manager as its listener: it is released while the manager still exists. A player event that reaches
				// a manager disposed by the handler crashes the process ("Unable to activate instance of type MediaManager").
				secondPlatformView.Dispose();
			}
		});
	}

	[Fact]
	public async Task ShouldLoopPlaybackMapsToTheRepeatModeOfThePlayer()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.ShouldLoopPlayback = true);

		await WithHandlerAsync(mediaElement, handler =>
		{
			var player = GetPlayer(mediaElement);
			Assert.Equal(PlayerConsts.RepeatModeOne, player.RepeatMode);

			mediaElement.ShouldLoopPlayback = false;
			Assert.Equal(PlayerConsts.RepeatModeOff, player.RepeatMode);

			mediaElement.ShouldLoopPlayback = true;
			Assert.Equal(PlayerConsts.RepeatModeOne, player.RepeatMode);
		});
	}

	[Fact]
	public async Task ShouldLoopPlaybackMapperDoesNothingAfterTheHandlerWasDisposed()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.ShouldLoopPlayback = true);

		await WithHandlerAsync(mediaElement, handler =>
		{
			DisposePlatformViewThenHandler(mediaElement, handler);
			Assert.Null(handler.Manager);

			var exception = Record.Exception(() => MediaElementHandler.ShouldLoopPlayback(handler, mediaElement));

			Assert.Null(exception);
		});
	}

	// Every mapper is "handler.MediaManager?.X()": a handler that was disposed has no manager and the property mapper (or a command) can still
	// reach it, because the MediaElement keeps its handler. A "!" or a missing "?" would throw NullReferenceException.
	[Fact]
	public async Task MappersDoNothingAfterTheHandlerWasDisposed()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = new UriMediaSource());

		await WithHandlerAsync(mediaElement, handler =>
		{
			DisposePlatformViewThenHandler(mediaElement, handler);
			Assert.Null(handler.Manager);
			var mappers = new Dictionary<string, Action>
			{
				[nameof(MediaElementHandler.MapAspect)] = () => MediaElementHandler.MapAspect(handler, mediaElement),
				[nameof(MediaElementHandler.MapShouldShowPlaybackControls)] = () => MediaElementHandler.MapShouldShowPlaybackControls(handler, mediaElement),
				[nameof(MediaElementHandler.MapSource)] = () => MediaElementHandler.MapSource(handler, mediaElement),
				[nameof(MediaElementHandler.MapSpeed)] = () => MediaElementHandler.MapSpeed(handler, mediaElement),
				[nameof(MediaElementHandler.MapStatusUpdated)] = () => MediaElementHandler.MapStatusUpdated(handler, mediaElement, null),
				[nameof(MediaElementHandler.MapVolume)] = () => MediaElementHandler.MapVolume(handler, mediaElement),
				[nameof(MediaElementHandler.MapShouldKeepScreenOn)] = () => MediaElementHandler.MapShouldKeepScreenOn(handler, mediaElement),
				[nameof(MediaElementHandler.MapShouldMute)] = () => MediaElementHandler.MapShouldMute(handler, mediaElement),
				[nameof(MediaElementHandler.MapPlayRequested)] = () => MediaElementHandler.MapPlayRequested(handler, mediaElement, null),
				[nameof(MediaElementHandler.MapPauseRequested)] = () => MediaElementHandler.MapPauseRequested(handler, mediaElement, null),
				[nameof(MediaElementHandler.MapStopRequested)] = () => MediaElementHandler.MapStopRequested(handler, mediaElement, null),
				[nameof(MediaElementHandler.ShouldLoopPlayback)] = () => MediaElementHandler.ShouldLoopPlayback(handler, mediaElement),
			};

			var failures = mappers
				.Select(mapper => (mapper.Key, Exception: Record.Exception(mapper.Value)))
				.Where(result => result.Exception is not null)
				.Select(result => $"{result.Key}: {result.Exception!.GetType().Name}")
				.ToArray();

			Assert.Empty(failures);
		});
	}

	[Fact]
	public async Task PropertyChangesAndCommandsAfterTheHandlerWasDisconnectedDoNotThrow()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();
		AndroidMediaElementProbeHandler? disconnectedHandler = null;
		await WithHandlerAsync(mediaElement, handler => disconnectedHandler = handler);

		var exception = await Record.ExceptionAsync(() => InvokeOnMainThreadAsync(() =>
		{
			mediaElement.Aspect = Aspect.Fill;
			mediaElement.ShouldShowPlaybackControls = true;
			mediaElement.ShouldKeepScreenOn = true;
			mediaElement.ShouldMute = true;
			mediaElement.ShouldLoopPlayback = true;
			mediaElement.Speed = 2;
			mediaElement.Volume = 0.5;
			mediaElement.Source = MediaSource.FromResource(toneAsset);
			mediaElement.Play();
			mediaElement.Pause();
			mediaElement.Stop();
		}));

		Assert.Null(exception);
		Assert.Null(disconnectedHandler!.Manager);
	}

	[Fact]
	public async Task DisconnectHandlerReleasesThePlayerAndDisposesThePlatformView()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();
		AndroidMediaElementProbeHandler? probeHandler = null;
		MauiMediaElement? platformView = null;
		JavaObjectReference? playerReference = null;

		try
		{
			await HostAsync(mediaElement, handler =>
			{
				probeHandler = handler;
				platformView = GetMauiMediaElement(mediaElement);
				var player = GetPlayer(mediaElement);
				playerReference = new JavaObjectReference((Java.Lang.Object)player);
				Assert.False(player.IsReleased);
				Assert.NotEqual(IntPtr.Zero, platformView.Handle);
			});
			// HostAsync disconnected the handler.

			Assert.NotNull(probeHandler);
			Assert.Equal(IntPtr.Zero, platformView!.Handle);
			Assert.Null(probeHandler.Manager);
			// The managed peer was disposed with the view, so the player's state is read through a new peer of the same Java object.
			Assert.True(await InvokeOnMainThreadAsync(() => playerReference!.GetPeer<AndroidX.Media3.ExoPlayer.IExoPlayer>().IsReleased));
		}
		finally
		{
			playerReference?.Dispose();
		}
	}

	// MAUI's IElementHandler.DisconnectHandler calls the toolkit's DisconnectHandler(platformView) only while the handler has a platform view,
	// so it never runs twice through MAUI: the test calls it twice directly.
	[Fact]
	public async Task DisconnectHandlerCalledTwiceDoesNotThrow()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			var platformView = GetMauiMediaElement(mediaElement);
			handler.DisconnectPlatformView(platformView);
			Assert.Null(handler.Manager);
			Assert.Equal(IntPtr.Zero, platformView.Handle);

			var exception = Record.Exception(() => handler.DisconnectPlatformView(platformView));

			Assert.Null(exception);
			Assert.Null(handler.Manager);
		});
	}

	[Fact]
	public async Task DisposingWithDisposingFalseKeepsTheMediaManagerAndDisposingWithTrueReleasesIt()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			var player = GetPlayer(mediaElement);

			handler.InvokeDispose(false);

			// The finalizer path releases nothing: it must not touch managed objects that may already be finalized.
			Assert.NotNull(handler.Manager);
			Assert.False(player.IsReleased);

			DisposePlatformViewThenHandler(mediaElement, handler);

			Assert.Null(handler.Manager);
		});
	}

	[Fact]
	public async Task HostingTheMediaElementAgainLoadsItsSourceIntoANewPlayer()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);
		MediaManager? firstManager = null;

		await HostAsync(mediaElement, async handler =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			firstManager = handler.Manager;
		});
		// HostAsync disconnected the handler.

		await HostAsync(mediaElement, async handler =>
		{
			await WaitUntilAsync(() => recorder.OpenedCount == 2, "The source was never loaded into the new player.");
			var player = GetPlayer(mediaElement);

			Assert.NotNull(handler.Manager);
			Assert.NotSame(firstManager, handler.Manager);
			Assert.False(player.IsReleased);
			Assert.Equal(1, player.MediaItemCount);
			await WaitUntilAsync(() => player.Duration == toneDuration.TotalMilliseconds, "The new player never reported the duration.");
		});
	}

	[Fact]
	public async Task HandlerAndPlayerDoNotLeak()
	{
		SetupProbeBuilder();
		WeakReference viewReference = null!, handlerReference = null!, platformViewReference = null!, playerViewReference = null!, managerReference = null!, playerReference = null!;

		await InvokeOnMainThreadAsync(() =>
		{
			var mediaElement = new MediaElement { WidthRequest = 200, HeightRequest = 100 };
			var handler = CreateHandler<AndroidMediaElementProbeHandler>(mediaElement);
			var platformView = (MauiMediaElement)handler.PlatformView!;
			var playerView = GetPlayerView(platformView);

			viewReference = new(mediaElement);
			handlerReference = new(handler);
			platformViewReference = new(platformView);
			playerViewReference = new(playerView);
			managerReference = new(handler.Manager);
			playerReference = new(playerView.Player);

			((IElementHandler)handler).DisconnectHandler();
			mediaElement.Handler = null;
		});

		await AssertionExtensions.WaitForGC(viewReference, handlerReference, platformViewReference, playerViewReference, managerReference, playerReference);
	}

	[Fact]
	public async Task PlayRequestStartsPlaybackAndPauseRequestPausesIt()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var player = GetPlayer(mediaElement);
			Assert.False(player.PlayWhenReady);

			mediaElement.Play();
			Assert.True(player.PlayWhenReady);
			await WaitForStateAsync(mediaElement, MediaElementState.Playing);
			await WaitUntilAsync(() => player.IsPlaying, "The player never started to play.");

			mediaElement.Pause();
			Assert.False(player.PlayWhenReady);
			await WaitForStateAsync(mediaElement, MediaElementState.Paused);
			Assert.False(player.IsPlaying);
		});
	}

	[Fact]
	public async Task StopRequestStopsThePlayerAndRewindsToTheStart()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var player = GetPlayer(mediaElement);
			await mediaElement.SeekTo(TimeSpan.FromSeconds(1));
			Assert.Equal(1000, player.CurrentPosition);
			await WaitUntilAsync(() => mediaElement.Position == TimeSpan.FromSeconds(1), "The status timer never copied the seek position.");

			mediaElement.Stop();

			Assert.Equal(PlayerConsts.StateIdle, player.PlaybackState);
			Assert.Equal(0, player.CurrentPosition);
			Assert.Equal(TimeSpan.Zero, mediaElement.Position);
			// The MediaElement stops when ExoPlayer reports the idle player through OnPlayerStateChanged.
			await WaitForStateAsync(mediaElement, MediaElementState.Stopped);
		});
	}

	[Fact]
	public async Task PlayPauseAndStopRequestsAreIgnoredWithoutASource()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			var player = GetPlayer(mediaElement);

			mediaElement.Play();
			Assert.False(player.PlayWhenReady);
			Assert.Equal(PlayerConsts.StateIdle, player.PlaybackState);

			// State that Pause (PlayWhenReady = false) and Stop (SeekTo(0), Position = 0) would change if they reached the player.
			player.PlayWhenReady = true;
			((IMediaElement)mediaElement).Position = TimeSpan.FromSeconds(1);

			mediaElement.Pause();
			mediaElement.Stop();

			Assert.True(player.PlayWhenReady);
			Assert.Equal(TimeSpan.FromSeconds(1), mediaElement.Position);
			Assert.Equal(PlayerConsts.StateIdle, player.PlaybackState);
			Assert.Equal(MediaElementState.None, mediaElement.CurrentState);
		});
	}

	[Fact]
	public async Task SeekRequestMovesThePlayerAndRaisesSeekCompleted()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var player = GetPlayer(mediaElement);

			await mediaElement.SeekTo(TimeSpan.FromSeconds(1.5));

			Assert.Equal(1500, player.CurrentPosition);
			Assert.Equal(1, recorder.SeekCompletedCount);
		});
	}

	[Fact]
	public async Task StatusUpdatesCopyThePlayerPositionToTheMediaElement()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var player = GetPlayer(mediaElement);

			// A seek that does not go through MediaElement.SeekTo: only the 200 ms status timer (StatusUpdated command) can notice it.
			player.SeekTo(1200);

			await WaitUntilAsync(() => mediaElement.Position == TimeSpan.FromMilliseconds(1200), "The status timer never copied the player position.");
		});
	}

	[Fact]
	public async Task SeekRequestWithoutArgumentsThrowsArgumentNull()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			Exception? raised;
			using (var capture = new AsyncVoidExceptionCapture())
			{
				MediaElementHandler.MapSeekRequested(handler, mediaElement, null);
				raised = capture.Exception;
			}

			Assert.IsType<ArgumentNullException>(raised);
		});
	}

	[Fact]
	public async Task SeekRequestWithWrongArgumentsDoesNothing()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			Exception? raised;
			using (var capture = new AsyncVoidExceptionCapture())
			{
				MediaElementHandler.MapSeekRequested(handler, mediaElement, EventArgs.Empty);
				raised = capture.Exception;
			}

			Assert.Null(raised);
			Assert.False(((IMediaElement)mediaElement).SeekCompletedTCS.Task.IsCompleted);
		});
	}

	[Fact]
	public async Task SeekRequestOnADisposedHandlerStillCompletesTheSeek()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();

		await WithHandlerAsync(mediaElement, async handler =>
		{
			DisposePlatformViewThenHandler(mediaElement, handler);
			var seekCompleted = ((IMediaElement)mediaElement).SeekCompletedTCS;
			Assert.False(seekCompleted.Task.IsCompleted);

			MediaElementHandler.MapSeekRequested(handler, mediaElement, new MediaSeekRequestedEventArgs(TimeSpan.FromSeconds(1)));

			await seekCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		});
	}

	// MapSeekRequested takes the task of the SeekTo that requested the seek before it waits for the seek, so the late answer to a cancelled
	// SeekTo does not complete the next one.
	[Fact]
	public async Task SeekToCancelledByItsTokenDoesNotCompleteTheNextSeekTo()
	{
		SetupProbeBuilder();
		// A source without data: ExoPlayer never answers a seek by itself, the test answers it through the player listener.
		var mediaElement = await CreateMediaElementAsync(element => element.Source = new UriMediaSource());

		await WithHandlerAsync(mediaElement, async handler =>
		{
			using (var cancellation = new CancellationTokenSource())
			{
				var first = mediaElement.SeekTo(TimeSpan.FromSeconds(1), cancellation.Token);
				Assert.False(first.IsCompleted);
				cancellation.Cancel();
				await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
			}

			var second = mediaElement.SeekTo(TimeSpan.FromSeconds(2));
			Assert.False(second.IsCompleted);

			handler.Manager!.OnPlaybackStateChanged(PlayerConsts.StateReady);

			Assert.False(await AssertHelpers.Wait(() => second.IsCompleted, timeout: 1000), "The answer to the first seek completed the second SeekTo, whose own seek had not been answered.");

			handler.Manager.OnPlaybackStateChanged(PlayerConsts.StateReady);
			await second.WaitAsync(TimeSpan.FromSeconds(5));
		});
	}

	// SeekTo stops waiting when its token is cancelled, and the seek goes on. When that seek fails later (here the SeekCompleted handler of the app
	// throws, which fails the platform seek), nothing awaits the task of the seek request any more: the failure must not fault it, which would
	// raise TaskScheduler.UnobservedTaskException (apps log it, or crash on it) once the task is collected.
	[Fact]
	public async Task FailureOfASeekAfterItsTokenWasCancelledLeavesNoUnobservedTaskException()
	{
		SetupProbeBuilder();
		// A source without data: ExoPlayer never answers a seek by itself, the test answers it through the player listener.
		var mediaElement = await CreateMediaElementAsync(element => element.Source = new UriMediaSource());
		var failureMessage = $"The SeekCompleted handler of the app failed ({Guid.NewGuid():N}).";
		var unobservedFailures = new ConcurrentQueue<AggregateException>();
		void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
		{
			// The handler's exception comes wrapped (MAUI's WeakEventManager invokes handlers through reflection): the text of the whole exception
			// holds the unique message.
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
			await WithHandlerAsync(mediaElement, async handler =>
			{
				var seekEnded = false;
				mediaElement.SeekCompleted += (_, _) =>
				{
					seekEnded = true;
					throw new InvalidOperationException(failureMessage);
				};

				using (var cancellation = new CancellationTokenSource())
				{
					var seek = mediaElement.SeekTo(TimeSpan.FromSeconds(1), cancellation.Token);
					seekRequestTask = GetSeekRequestTask(mediaElement);
					cancellation.Cancel();
					await Assert.ThrowsAnyAsync<OperationCanceledException>(() => seek);
				}

				handler.Manager!.OnPlaybackStateChanged(PlayerConsts.StateReady);
				await AssertionExtensions.AssertEventually(() => seekEnded, timeout: 5000, message: "The seek never ended.");
			});

			// Collecting the task of the seek request runs the finalizer that reports an exception nothing observed.
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

	// The MediaManager is the ExoPlayer's listener and never removes itself: disposing the handler (hence the manager) while the player
	// is alive crashes the process on the player's next event ("Unable to activate instance of type MediaManager"). DisconnectHandler
	// releases the player first, and so do these tests before they dispose the handler.
	static void DisposePlatformViewThenHandler(MediaElement mediaElement, AndroidMediaElementProbeHandler handler)
	{
		GetMauiMediaElement(mediaElement).Dispose();
		handler.InvokeDispose(true);
	}

	static int GetResourceId(PlayerView playerView, string name) =>
		playerView.Context!.Resources!.GetIdentifier(name, "id", playerView.Context.PackageName);

	static AColor GetShutterColor(PlayerView playerView)
	{
		var shutter = playerView.FindViewById(GetResourceId(playerView, "exo_shutter"));
		Assert.NotNull(shutter);
		return Assert.IsType<ColorDrawable>(shutter.Background).Color;
	}
}