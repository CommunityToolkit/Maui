using System.Reflection;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Service.Notification;
using AndroidX.Core.App;
using AndroidX.Media3.ExoPlayer;
using AndroidX.Media3.Session;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CommunityToolkit.Maui.DeviceTests.Runners.HeadlessRunner;
using CommunityToolkit.Maui.Media.Services;
using CommunityToolkit.Maui.Services;
using CommunityToolkit.Maui.Views;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;
using PlayerConsts = AndroidX.Media3.Common.BasePlayer.InterfaceConsts;

namespace CommunityToolkit.Maui.DeviceTests;

// MediaControlsService (MediaControlsService.android.cs, BoundServiceBinder.android.cs) and the service glue of MediaManager
// (StartService, StopService, UpdateNotifications, the TaskRemoved subscription). With the foreground service enabled the manager
// starts and binds the real service; the tests read what the system reports about it (ActivityManager, NotificationManager) and call the
// service's public lifecycle methods the way the system would. The notification and foreground-type branches depend on the Android
// version: each device checks the branch that its version takes (the CI matrix runs API 36 and API 28).
[Category(TestCategory.MediaElementAndroid)]
[Collection(TestCollections.DisableParallelization)]
public class MediaControlsServiceAndroidTests : MediaElementAndroidTestBase
{
	static readonly FieldInfo playerNotificationManagerField = typeof(MediaControlsService).GetField("playerNotificationManager", BindingFlags.Instance | BindingFlags.NonPublic)!;
	static readonly FieldInfo notificationBuilderField = typeof(MediaControlsService).GetField("notificationBuilder", BindingFlags.Instance | BindingFlags.NonPublic)!;

	// Hosts the element and waits until its MediaManager is bound to the service (the system connects it on a later turn of the UI thread).
	async Task HostBoundAsync(MediaElement mediaElement, Func<AndroidMediaElementProbeHandler, BoundServiceConnection, Task> test)
	{
		await HostAsync(mediaElement, async handler =>
		{
			var connection = GetServiceConnection(handler.Manager!) ?? throw new InvalidOperationException("The MediaManager has no service connection.");
			await WaitUntilAsync(() => connection.IsConnected, "The MediaManager never connected to MediaControlsService.");
			await test(handler, connection);
		});
	}

	static Task WaitForForegroundNotificationAsync() =>
		WaitUntilAsync(() => FindMediaControlsService()?.Foreground == true && FindServiceNotification() is not null, "MediaControlsService never showed its foreground notification.");

	[Fact]
	public async Task EnabledForegroundServiceRunsMediaControlsServiceInTheForegroundWithItsChannel()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		Assert.True(mediaElement.IsAndroidForegroundServiceEnabled);
		Assert.Null(FindMediaControlsService());

		await HostBoundAsync(mediaElement, async (handler, connection) =>
		{
			await WaitUntilAsync(() => FindMediaControlsService()?.Foreground == true, "MediaControlsService never entered the foreground.");

			Assert.True(FindMediaControlsService()!.Started);
			var service = connection.Binder!.Service;
			if (OperatingSystem.IsAndroidVersionAtLeast(29))
			{
				Assert.Equal(ForegroundService.TypeMediaPlayback, service.ForegroundServiceType);
			}

			var channel = service.NotificationManager!.GetNotificationChannel("1");
			Assert.NotNull(channel);
			Assert.Equal("1", channel.Name);
			Assert.Equal(NotificationImportance.Low, channel.Importance);
		});
	}

	[Fact]
	public async Task ForegroundServiceCanBeEnabledWithTheMediaElementOptions()
	{
		// A variable, not the literal true: the MediaElement source generator turns a literal into assembly-level manifest permissions and into a
		// CommunityToolkit.Maui.Android namespace that would hide every Android.* name of this app. The manifest of the test app declares them.
		var enableForegroundService = true;
		SetupProbeBuilder(configureMediaElementOptions: options => options.SetIsAndroidForegroundServiceEnabled(enableForegroundService));
		var mediaElement = await CreateMediaElementAsync();
		Assert.True(mediaElement.IsAndroidForegroundServiceEnabled);

		await HostBoundAsync(mediaElement, async (handler, connection) =>
		{
			// No Source is needed: the service starts when the handler connects.
			await WaitUntilAsync(() => FindMediaControlsService()?.Foreground == true, "MediaControlsService never entered the foreground.");
		});
	}

	[Fact]
	public async Task DefaultOptionsNeverStartMediaControlsService()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);
		Assert.False(mediaElement.IsAndroidForegroundServiceEnabled);

		await HostAsync(mediaElement, async handler =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);

			// Without the foreground service there is no notification to update either.
			handler.Manager!.UpdateNotifications();

			Assert.Null(GetServiceConnection(handler.Manager));
			Assert.Null(FindMediaControlsService());
			Assert.Empty(GetActiveNotifications());
		});
	}

	[Fact]
	public async Task ServiceNotificationIsTheMediaNotificationOfTheMediaSession()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));

		await HostBoundAsync(mediaElement, async (handler, connection) =>
		{
			// The connection refreshes the notification when it connects (UpdateNotifications found no service while the source was loaded).
			// Before Android 13 the PlayerNotificationManager then posts its own version under the same id, which the system marks as the
			// foreground notification again some milliseconds later (the flags are missing in between): wait for the settled notification.
			await WaitUntilAsync(() => FindServiceNotification() is Notification posted && IsMediaStyle(posted) && posted.Flags.HasFlag(NotificationFlags.ForegroundService),
				"The service never showed a media notification as its foreground notification.");
			var notification = FindServiceNotification()!;
			var session = GetMediaSession(handler.Manager!)!;

			Assert.Equal("1", notification.ChannelId);
			Assert.Equal(NotificationVisibility.Public, notification.Visibility);
			Assert.True(notification.Flags.HasFlag(NotificationFlags.NoClear), $"The foreground notification can be swiped away: {notification.Flags}.");
			Assert.NotNull(notification.SmallIcon);
			Assert.True(session.PlatformToken!.Equals(notification.Extras!.Get(Notification.ExtraMediaSession)), "The notification is not bound to the MediaSession of the manager.");
		});
	}

	// Android 13 and later render the transport controls of a MediaStyle notification themselves; before that the service lets a
	// PlayerNotificationManager add the actions (SetLegacyNotifications).
	[Fact]
	public async Task NotificationHasTransportActionsOnlyBeforeAndroid13()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));

		await HostBoundAsync(mediaElement, async (handler, connection) =>
		{
			await WaitUntilAsync(() => IsMediaStyle(FindServiceNotification()), "The service never posted a media notification.");

			if (OperatingSystem.IsAndroidVersionAtLeast(33))
			{
				Assert.Empty(FindServiceNotification()!.Actions ?? []);
				Assert.Null(playerNotificationManagerField.GetValue(connection.Binder!.Service));
			}
			else
			{
				await WaitUntilAsync(() => FindServiceNotification()?.Actions?.Count > 0, "The PlayerNotificationManager never added the transport actions.");
				Assert.NotEmpty(FindServiceNotification()!.Extras!.GetIntArray(Notification.ExtraCompactActions)!);
				Assert.NotNull(playerNotificationManagerField.GetValue(connection.Binder!.Service));
			}
		});
	}

	// Before Android 13 the PlayerNotificationManager posts its own notification under the same id and replaces the one StartForegroundServices
	// built: both have the content intent (CreateActivityPendingIntent), so tapping the media notification opens the app on every version.
	[Fact]
	public async Task NotificationOpensTheAppWhenTapped()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));

		await HostBoundAsync(mediaElement, async (handler, connection) =>
		{
			await WaitUntilAsync(() => IsMediaStyle(FindServiceNotification()), "The service never posted a media notification.");

			var contentIntent = FindServiceNotification()!.ContentIntent;

			Assert.NotNull(contentIntent);
			Assert.Equal(Microsoft.Maui.ApplicationModel.Platform.AppContext.PackageName, contentIntent.CreatorPackage);
		});
	}

	[Fact]
	public async Task UpdatingNotificationsPostsTheNotificationAgain()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));

		await HostBoundAsync(mediaElement, async (handler, connection) =>
		{
			await WaitUntilAsync(() => IsMediaStyle(FindServiceNotification()), "The service never posted a media notification.");
			var postedBefore = GetActiveNotifications().Single(n => n.Id == serviceNotificationId).PostTime;

			handler.Manager!.UpdateNotifications();

			await WaitUntilAsync(() => GetActiveNotifications().Single(n => n.Id == serviceNotificationId).PostTime > postedBefore, "UpdateNotifications did not post the notification again.");
			Assert.True(IsMediaStyle(FindServiceNotification()));
		});
	}

	[Fact]
	public async Task UpdatingNotificationsBeforeTheServiceIsConnectedDoesNothing()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync();

		await WithHandlerAsync(mediaElement, async handler =>
		{
			// The source mapping started the service when the handler connected; the system connects the manager on a later turn of this thread.
			var connection = GetServiceConnection(handler.Manager!);
			Assert.NotNull(connection);
			Assert.False(connection.IsConnected);

			var exception = Record.Exception(() => handler.Manager!.UpdateNotifications());

			Assert.Null(exception);
			Assert.Null(FindServiceNotification());
			// Do not tear the service down before it was created.
			await WaitUntilAsync(() => connection.IsConnected, "The MediaManager never connected to MediaControlsService.");
		});
	}

	[Fact]
	public async Task LaterSourcesReuseTheServiceConnection()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));

		await HostBoundAsync(mediaElement, async (handler, connection) =>
		{
			var service = connection.Binder!.Service;
			await WaitUntilAsync(() => IsMediaStyle(FindServiceNotification()), "The service never posted a media notification.");
			var postedBefore = GetServiceNotificationPostTime();

			mediaElement.Source = MediaSource.FromResource(shortToneAsset);
			await WaitUntilAsync(() => mediaElement.Duration == shortToneDuration, "The second source was never loaded.");

			Assert.Same(connection, GetServiceConnection(handler.Manager!));
			Assert.Same(handler.Manager, connection.Activity);
			Assert.Same(service, connection.Binder.Service);
			Assert.True(FindMediaControlsService()!.Foreground);
			// A source loaded while the service runs refreshes its notification (PlatformUpdateSource calls UpdateNotifications).
			await WaitUntilAsync(() => GetServiceNotificationPostTime() > postedBefore, "Loading the second source did not post the notification again.");
		});
	}

	[Fact]
	public async Task DisposingTheHandlerStopsTheServiceAndRemovesItsNotification()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		MediaManager? manager = null;
		BoundServiceConnection? connection = null;

		await HostBoundAsync(mediaElement, async (handler, serviceConnection) =>
		{
			manager = handler.Manager;
			connection = serviceConnection;
			await WaitForForegroundNotificationAsync();
		});
		// HostAsync disconnected the handler, which disposed the manager: it stops and unbinds the service.

		await WaitForMediaControlsServiceToStopAsync();

		Assert.Null(FindMediaControlsService());
		Assert.Empty(GetActiveNotifications());
		Assert.Null(GetServiceConnection(manager!));
		// The connection is a Java peer: disposing it released its JNI reference.
		Assert.Equal(IntPtr.Zero, connection!.Handle);
	}

	[Fact]
	public async Task TaskRemovedStopsThePlayerAndIsForwardedToTheConnection()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostBoundAsync(mediaElement, async (handler, connection) =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var player = GetPlayer(mediaElement);
			mediaElement.Play();
			await WaitUntilAsync(() => player.IsPlaying, "The player never started to play.");
			var subscriber = new TaskRemovedSubscriber();
			connection.MediaControlsServiceTaskRemoved += subscriber.Handle;

			// What the system does when the user swipes the app away from the recent apps.
			connection.Binder!.Service.OnTaskRemoved(null);

			Assert.Equal(1, subscriber.Count);
			Assert.Equal(PlayerConsts.StateIdle, player.PlaybackState);
			Assert.False(player.IsPlaying);
			GC.KeepAlive(subscriber);
		});
	}

	// Before Android 13 the service's notification is the legacy PlayerNotificationManager's, and its Play action (a broadcast to that manager's
	// receiver) controls the player. When the task is removed the service detaches the manager from the player (SetPlayer(null)): the same tap,
	// which reached the player before, no longer does.
	[Fact]
	public async Task TaskRemovedDetachesTheLegacyNotificationFromThePlayer()
	{
		Skip.If(OperatingSystem.IsAndroidVersionAtLeast(33), "The legacy PlayerNotificationManager is only set up before Android 13.");
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostBoundAsync(mediaElement, async (handler, connection) =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var player = GetPlayer(mediaElement);
			var play = await GetLegacyPlayActionAsync();
			play.Send();
			await WaitUntilAsync(() => player.PlayWhenReady, "The Play action of the legacy notification never reached the player.");
			mediaElement.Pause();
			Assert.False(player.PlayWhenReady);

			connection.Binder!.Service.OnTaskRemoved(null);

			play.Send();
			Assert.False(await Wait(() => player.PlayWhenReady, timeout: 1500), "The Play action of the legacy notification still controls the player after the task was removed.");
		});
	}

	// OnTaskRemoved and OnDestroy cancel every notification of the app (NotificationManager.CancelAll). The foreground service's own notification
	// cannot be cancelled by the app while the service runs, so a notification that the test posts shows whether the call was made. From
	// Android 13 the app needs the POST_NOTIFICATIONS permission to post it: the headless runner grants it through the test instrumentation.
	[Fact]
	public async Task TaskRemovedCancelsEveryNotificationOfTheApp()
	{
		EnsureNotificationPermission();
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));

		try
		{
			await HostBoundAsync(mediaElement, async (handler, connection) =>
			{
				await WaitForForegroundNotificationAsync();
				await PostSentinelNotificationAsync();

				connection.Binder!.Service.OnTaskRemoved(null);

				await WaitUntilAsync(() => FindSentinelNotification() is null, "OnTaskRemoved did not cancel the notification of the app.");
			});
		}
		finally
		{
			// A regression must not leave the notification behind for the teardown checks of every later test.
			CancelSentinelNotification();
		}
	}

	[Fact]
	public async Task DestroyingTheServiceCancelsEveryNotificationOfTheApp()
	{
		EnsureNotificationPermission();
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));

		try
		{
			await HostBoundAsync(mediaElement, async (handler, connection) =>
			{
				await WaitForForegroundNotificationAsync();
				await PostSentinelNotificationAsync();
			});
			// HostAsync disconnected the handler: the manager stopped the service, and the system destroys it.

			await WaitUntilAsync(() => FindMediaControlsService() is null, "MediaControlsService was never stopped.");
			await WaitUntilAsync(() => FindSentinelNotification() is null, "Destroying the service did not cancel the notification of the app.");
		}
		finally
		{
			CancelSentinelNotification();
		}
	}

	// Before Android 13 OnDestroy also takes the service out of the foreground itself (StopForeground) and detaches the legacy PlayerNotificationManager
	// from the player. The system calls OnDestroy only after it stopped the service, when neither can be seen any more, so the test calls it on the
	// running service, which the manager is still bound to: the binding keeps the service alive through OnDestroy's StopSelf.
	[Fact]
	public async Task DestroyingTheServiceEndsItsForegroundAndDetachesTheLegacyNotification()
	{
		Skip.If(OperatingSystem.IsAndroidVersionAtLeast(33), "From Android 13 OnDestroy neither calls StopForeground nor has a legacy PlayerNotificationManager to detach.");
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostBoundAsync(mediaElement, async (handler, connection) =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var service = connection.Binder!.Service;
			var player = GetPlayer(mediaElement);
			await WaitForForegroundNotificationAsync();
			var play = await GetLegacyPlayActionAsync();
			play.Send();
			await WaitUntilAsync(() => player.PlayWhenReady, "The Play action of the legacy notification never reached the player.");
			mediaElement.Pause();

			// StopSelf, which OnDestroy calls too, is a synchronous call to the ActivityManager; it does not end the foreground of a service that a
			// client is bound to. Only StopForeground can.
			service.StopSelf();
			Assert.True(FindMediaControlsService()?.Foreground, "StopSelf alone took the bound service out of the foreground.");

			service.OnDestroy();

			var runningService = FindMediaControlsService();
			Assert.NotNull(runningService);
			Assert.False(runningService.Foreground, "OnDestroy left the service in the foreground.");
			await WaitUntilAsync(() => FindServiceNotification() is null, "OnDestroy did not remove the service notification.");
			play.Send();
			Assert.False(await Wait(() => player.PlayWhenReady, timeout: 1500), "The Play action of the legacy notification still controls the player after OnDestroy.");
		});
	}

	// StartForegroundServices (run by OnCreate and OnRebind) asks the base context for the system's NotificationManager. A service object that
	// gets a base context whose notification service is something else reaches the guard.
	[Fact]
	public void ServiceWhoseContextHasNoNotificationManagerFailsToStart()
	{
		var service = new MediaControlsService();
		var attachBaseContext = typeof(ContextWrapper).GetMethod("AttachBaseContext", BindingFlags.Instance | BindingFlags.NonPublic)!;
		attachBaseContext.Invoke(service, [new ContextWithoutNotificationManager(Microsoft.Maui.ApplicationModel.Platform.AppContext)]);

		var exception = Assert.Throws<InvalidOperationException>(service.OnCreate);

		Assert.Equal("NotificationManager cannot be null", exception.Message);
		Assert.Null(service.NotificationManager);
	}

	public enum MissingPiece
	{
		ForegroundServiceFlag,
		Session,
		Player,
	}

	// UpdateNotifications only talks to the service while the manager is bound to it, has the foreground service switched on, and has a media
	// session and a player to show. The notification is taken away first (a plain StopForeground), so that only a post by the update under test
	// could bring it back; the same update with everything in place afterwards shows that the wait for "nothing" is not vacuous. Before Android 13
	// the legacy PlayerNotificationManager also posts the notification, on every playback event of the player: the source is loaded first, and the
	// legacy manager is detached from the player for the wait (the update with everything in place attaches it again).
	[Theory]
	[InlineData(MissingPiece.ForegroundServiceFlag)]
	[InlineData(MissingPiece.Session)]
	[InlineData(MissingPiece.Player)]
	public async Task UpdatingNotificationsDoesNothingWithoutTheForegroundServiceFlagTheSessionOrThePlayer(MissingPiece missing)
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostBoundAsync(mediaElement, async (handler, connection) =>
		{
			var manager = handler.Manager!;
			var service = connection.Binder!.Service;
			await WaitUntilLoadedAsync(mediaElement, recorder);
			await WaitForForegroundNotificationAsync();
			DetachLegacyNotificationManager(service);
			service.StopForeground(StopForegroundFlags.Remove);
			await WaitUntilAsync(() => FindServiceNotification() is null, "The service never took its notification away.");

			// UpdateNotifications decides synchronously whether to post: the missing piece is put back right after the call, so the status timer and
			// ExoPlayer's callbacks (which also run on this thread) never see the manager without its player or session.
			var restore = TakeAway(manager, missing);
			Exception? exception;
			try
			{
				exception = Record.Exception(manager.UpdateNotifications);
			}
			finally
			{
				restore();
			}

			Assert.Null(exception);
			Assert.False(await Wait(() => FindServiceNotification() is not null, timeout: 700), $"The notification was posted although the manager had no {missing}.");

			manager.UpdateNotifications();

			await WaitUntilAsync(() => FindServiceNotification() is not null, "The update with everything in place never posted the notification.");
		});
	}

	// The first guard of UpdateNotifications also returns when the binder of the connection has no service. The system only ever hands the manager
	// the binder that MediaControlsService.OnBind created with the service itself, so only internal misuse gets there: a BoundServiceBinder made
	// without a service and given to the connection's OnServiceConnected, which keeps it and then fails at Binder.Service.TaskRemoved. The manager
	// has everything else that UpdateNotifications needs, so without this guard the call would go to the missing service and throw.
	[Fact]
	public async Task UpdatingNotificationsDoesNothingWhenTheConnectedBinderHasNoService()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));

		await HostBoundAsync(mediaElement, (handler, connection) =>
		{
			const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
			var manager = handler.Manager!;
			var systemBinder = connection.Binder!;
			Assert.True((bool)typeof(MediaManager).GetField("isAndroidForegroundServiceEnabled", instance)!.GetValue(manager)!);
			Assert.NotNull(GetMediaSession(manager));
			Assert.NotNull(typeof(MediaManager).GetProperty("Player", instance)!.GetValue(manager));
			try
			{
				var connecting = Record.Exception(() => ((IServiceConnection)connection).OnServiceConnected(null, new BoundServiceBinder(null!)));
				Assert.IsType<NullReferenceException>(connecting);
				Assert.NotNull(connection.Binder);
				Assert.Null(connection.Binder.Service);

				var exception = Record.Exception(manager.UpdateNotifications);

				Assert.Null(exception);
			}
			finally
			{
				// The teardown unbinds the connection: give it back the binder the system connected it with.
				typeof(BoundServiceConnection).GetProperty(nameof(BoundServiceConnection.Binder))!.SetValue(connection, systemBinder);
			}

			return Task.CompletedTask;
		});
	}

	// StartService and StopService are only called with the foreground service switched on (the source mapping and Dispose check first), but
	// they guard themselves too. Without the guard, StartService would create a connection and start the real service, and StopService would
	// unbind a connection that was never bound, which Android refuses (IllegalArgumentException).
	[Fact]
	public async Task StartingOrStoppingTheServiceDoesNothingWhileTheForegroundServiceIsDisabled()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			var manager = handler.Manager!;
			Assert.False(mediaElement.IsAndroidForegroundServiceEnabled);
			var startService = typeof(MediaManager).GetMethod("StartService", BindingFlags.Instance | BindingFlags.NonPublic)!;
			var stopService = typeof(MediaManager).GetMethod("StopService", BindingFlags.Instance | BindingFlags.NonPublic)!;
			using var neverBound = new BoundServiceConnection(manager);

			var exception = Record.Exception(() =>
			{
				startService.Invoke(manager, null);
				stopService.Invoke(manager, [neverBound]);
			});

			Assert.Null(exception);
			Assert.Null(GetServiceConnection(manager));
			Assert.Null(FindMediaControlsService());
		});
	}

	// The manager unsubscribes from TaskRemoved when it is disposed, so the handler cannot normally run without a player; its null-conditional
	// keeps a late TaskRemoved from touching a player that does not exist.
	[Fact]
	public async Task TaskRemovedOfAManagerWithoutAPlayerDoesNothing()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			using var manager = new AndroidMediaManagerProbe(MauiContext, mediaElement, TestDispatcher.Current);
			var handleTaskRemoved = typeof(MediaManager).GetMethod("HandleMediaControlsServiceTaskRemoved", BindingFlags.Instance | BindingFlags.NonPublic)!;
			Assert.Null(manager.ExposedPlayer);

			var exception = Record.Exception(() => handleTaskRemoved.Invoke(manager, [null, EventArgs.Empty]));

			Assert.Null(exception);
		});
	}

	[Fact]
	public async Task ManagerStopsListeningForTaskRemovedWhenItIsDisposed()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync();
		MediaManager? manager = null;
		BoundServiceConnection? connection = null;
		await HostBoundAsync(mediaElement, (handler, serviceConnection) =>
		{
			manager = handler.Manager;
			connection = serviceConnection;
			return Task.CompletedTask;
		});
		await WaitForMediaControlsServiceToStopAsync();
		var raiseTaskRemoved = typeof(BoundServiceConnection).GetMethod("HandleTaskRemoved", BindingFlags.Instance | BindingFlags.NonPublic)!;

		// The manager's player was released: a manager that still listened would try to stop it and fail.
		var exception = Record.Exception(() => raiseTaskRemoved.Invoke(connection, [null, EventArgs.Empty]));

		Assert.Null(exception);
		GC.KeepAlive(manager);
	}

	// With the foreground service the system keeps the destroyed service's binder (BoundServiceBinder.Service is a strong reference). OnDestroy
	// takes the MediaStyle out of the notification builder, which would otherwise hold the MediaSession and, through its released player, the
	// PlayerView and its full-screen listener. HandlerAndPlayerDoNotLeak (MediaElementHandlerAndroidTests) is the same test without the service.
	[Fact]
	public async Task ElementWithTheForegroundServiceDoesNotLeak()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		WeakReference viewReference = null!, handlerReference = null!, platformViewReference = null!, managerReference = null!, connectionReference = null!, playerReference = null!;

		await InvokeOnMainThreadAsync(() =>
		{
			var mediaElement = new MediaElement { WidthRequest = 200, HeightRequest = 100 };
			var handler = CreateHandler<AndroidMediaElementProbeHandler>(mediaElement);
			var platformView = (MauiMediaElement)handler.PlatformView!;
			viewReference = new(mediaElement);
			handlerReference = new(handler);
			platformViewReference = new(platformView);
			managerReference = new(handler.Manager);
			connectionReference = new(GetServiceConnection(handler.Manager!));
			playerReference = new(GetPlayerView(platformView).Player);
		});
		// The system binds the manager to the service on a later turn of the UI thread: stopping it earlier would stop a service that is still starting.
		await WaitUntilAsync(() => ((BoundServiceConnection?)connectionReference.Target)?.IsConnected == true, "The MediaManager never connected to MediaControlsService.");
		await InvokeOnMainThreadAsync(() =>
		{
			((IElementHandler)handlerReference.Target!).DisconnectHandler();
			((MediaElement)viewReference.Target!).Handler = null;
		});
		await WaitForMediaControlsServiceToStopAsync();

		await AssertCollectedAsync(("MediaElement", viewReference), ("handler", handlerReference), ("MauiMediaElement", platformViewReference), ("MediaManager", managerReference),
			("BoundServiceConnection", connectionReference), ("ExoPlayer", playerReference));
	}

	// The service keeps running while another element is bound to it, so OnDestroy does not run when an element disconnects: the MediaManager of
	// the element tells the service to forget its session, which the notification would otherwise hold (its MediaStyle, and before Android 13
	// the PlayerNotificationManager's player). This happens when a page with a MediaElement is pushed over a page with another one and popped.
	[Fact]
	public async Task ElementDisconnectedWhileAnotherKeepsTheForegroundServiceRunningDoesNotLeak()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var remainingElement = await CreateMediaElementAsync();
		DisposeWhenTheTestEnds(remainingElement);
		var remainingConnection = await ConnectToTheServiceAsync(remainingElement);
		WeakReference viewReference = null!, handlerReference = null!, platformViewReference = null!, managerReference = null!, connectionReference = null!, playerReference = null!;

		await InvokeOnMainThreadAsync(() =>
		{
			var mediaElement = new MediaElement { WidthRequest = 200, HeightRequest = 100 };
			var handler = CreateHandler<AndroidMediaElementProbeHandler>(mediaElement);
			var platformView = (MauiMediaElement)handler.PlatformView!;
			viewReference = new(mediaElement);
			handlerReference = new(handler);
			platformViewReference = new(platformView);
			managerReference = new(handler.Manager);
			connectionReference = new(GetServiceConnection(handler.Manager!));
			playerReference = new(GetPlayerView(platformView).Player);
		});
		// The element that connects last posts the notification: it is built for this element's session.
		await WaitUntilAsync(() => ((BoundServiceConnection?)connectionReference.Target)?.IsConnected == true, "The second MediaManager never connected to MediaControlsService.");
		await InvokeOnMainThreadAsync(() =>
		{
			((IElementHandler)handlerReference.Target!).DisconnectHandler();
			((MediaElement)viewReference.Target!).Handler = null;
		});
		Assert.NotNull(FindMediaControlsService());
		Assert.True(remainingConnection.IsConnected);

		await AssertCollectedAsync(("MediaElement", viewReference), ("handler", handlerReference), ("MauiMediaElement", platformViewReference), ("MediaManager", managerReference),
			("BoundServiceConnection", connectionReference), ("ExoPlayer", playerReference));
	}

	// The service forgets only the session of the element that disconnects: the notification of the element that posted it last keeps its media
	// style (the service builds its next foreground notification from the same builder, for example when a client binds again).
	[Fact]
	public async Task ElementDisconnectingKeepsTheMediaStyleOfTheElementThatPostedTheNotification()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var disconnectingElement = await CreateMediaElementAsync();
		DisposeWhenTheTestEnds(disconnectingElement);
		await ConnectToTheServiceAsync(disconnectingElement);
		var remainingElement = await CreateMediaElementAsync();
		DisposeWhenTheTestEnds(remainingElement);
		var remainingConnection = await ConnectToTheServiceAsync(remainingElement);

		await DisconnectAsync(disconnectingElement);

		await InvokeOnMainThreadAsync(() =>
		{
			var builder = (NotificationCompat.Builder)notificationBuilderField.GetValue(remainingConnection.Binder!.Service)!;
			Assert.True(IsMediaStyle(builder.Build()), "The notification of the element that is still bound lost its media style.");
		});
	}

	// Before Android 13 the notification is the legacy PlayerNotificationManager's, which controls the player of the element that updated the
	// notifications last. An element that disconnects must leave it on that player when it is another element's: the Play action of the
	// notification still reaches the player of the element that is still bound.
	// Catches: ForgetSession detaching the legacy notification from the player of another element.
	[Fact]
	public async Task ElementDisconnectingLeavesTheLegacyNotificationOnThePlayerOfTheElementThatPostedIt()
	{
		Skip.If(OperatingSystem.IsAndroidVersionAtLeast(33), "The legacy PlayerNotificationManager is only set up before Android 13.");
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var disconnectingElement = await CreateMediaElementAsync();
		DisposeWhenTheTestEnds(disconnectingElement);
		await ConnectToTheServiceAsync(disconnectingElement);
		var remainingElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		DisposeWhenTheTestEnds(remainingElement);
		using var recorder = new MediaElementRecorder(remainingElement);
		await ConnectToTheServiceAsync(remainingElement);
		await WaitUntilLoadedAsync(remainingElement, recorder);

		await DisconnectAsync(disconnectingElement);

		// ExoPlayer may only be used on the main thread.
		await InvokeOnMainThreadAsync(async () =>
		{
			var remainingPlayer = GetPlayer(remainingElement);
			var play = await GetLegacyPlayActionAsync();
			play.Send();
			await WaitUntilAsync(() => remainingPlayer.PlayWhenReady, "The Play action of the legacy notification no longer reaches the player of the element that is still bound.");
		});
	}

	// Creates the element's handler (without a window) and waits until its MediaManager is bound to the service.
	async Task<BoundServiceConnection> ConnectToTheServiceAsync(MediaElement mediaElement)
	{
		var connection = await InvokeOnMainThreadAsync(() =>
		{
			var handler = CreateHandler<AndroidMediaElementProbeHandler>(mediaElement);
			return GetServiceConnection(handler.Manager!) ?? throw new InvalidOperationException("The MediaManager has no service connection.");
		});
		await WaitUntilAsync(() => connection.IsConnected, "The MediaManager never connected to MediaControlsService.");

		return connection;
	}

	[Fact]
	public async Task RebindingTheServiceStartsItInTheForegroundAgain()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));

		await HostBoundAsync(mediaElement, async (handler, connection) =>
		{
			var service = connection.Binder!.Service;
			await WaitForForegroundNotificationAsync();
			service.StopForeground(StopForegroundFlags.Remove);
			await WaitUntilAsync(() => FindMediaControlsService()?.Foreground == false && FindServiceNotification() is null, "The service never left the foreground.");

			// The system calls it when a client binds again (and the service's onUnbind returned true).
			service.OnRebind(null);

			await WaitForForegroundNotificationAsync();
		});
	}

	[Fact]
	public async Task LegacyNotificationManagerIsCreatedOnceAndReused()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));

		await HostBoundAsync(mediaElement, async (handler, connection) =>
		{
			var service = connection.Binder!.Service;
			var session = GetMediaSession(handler.Manager!)!;
			var player = GetPlayer(mediaElement);

			service.SetLegacyNotifications(session, player);
			var first = playerNotificationManagerField.GetValue(service);
			service.SetLegacyNotifications(session, player);

			Assert.NotNull(first);
			Assert.Same(first, playerNotificationManagerField.GetValue(service));
			await WaitUntilAsync(() => FindServiceNotification()?.Actions?.Count > 0, "The PlayerNotificationManager never posted its transport actions.");
		});
	}

	[Fact]
	public async Task ServiceKeepsItsObjectsUntilItIsDisposedAndDisposingAfterTheSystemDestroyedItIsSafe()
	{
		EnsureNotificationPermission();
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		MediaControlsService? service = null;
		try
		{
			await HostBoundAsync(mediaElement, async (handler, connection) =>
			{
				service = connection.Binder!.Service;
				// The legacy notification manager exists on every Android version after this.
				service.SetLegacyNotifications(GetMediaSession(handler.Manager!)!, GetPlayer(mediaElement));
				await WaitForForegroundNotificationAsync();
				Assert.NotNull(service.NotificationManager);
				Assert.NotNull(playerNotificationManagerField.GetValue(service));
				// OnDestroy cancels every notification of the app: once this one is gone, the system has called OnDestroy.
				await PostSentinelNotificationAsync();
			});
			// HostAsync disconnected the handler: the manager stopped the service. The ActivityManager stops listing the service before the
			// app's UI thread runs OnDestroy, so the service's absence alone does not show that OnDestroy ran.
			await WaitUntilAsync(() => FindMediaControlsService() is null && FindSentinelNotification() is null, "The system never destroyed MediaControlsService.");
		}
		finally
		{
			CancelSentinelNotification();
		}

		// Its managed object is still around, which is when Dispose is safe. OnDestroy runs on the UI thread and was running when the notification
		// disappeared, so a Dispose posted to the UI thread runs after it returned. Disposing earlier crashes the app: OnDestroy's StopSelf then
		// throws ObjectDisposedException.
		Assert.NotNull(service);
		await InvokeOnMainThreadAsync(service.Dispose);

		Assert.Null(service.NotificationManager);
		Assert.Null(playerNotificationManagerField.GetValue(service));
		Assert.Equal(IntPtr.Zero, service.Handle);
		var again = Record.Exception(service.Dispose);
		Assert.Null(again);

		// Without a NotificationManager the service cannot update its notification.
		MediaSession session = null!;
		IExoPlayer player = null!;
		var exception = Assert.Throws<ArgumentNullException>(() => service.UpdateNotifications(session, player));
		Assert.Equal("NotificationManager", exception.ParamName);
	}

	[Fact]
	public void OnBindCreatesABinderThatExposesTheService()
	{
		var service = new MediaControlsService();
		Assert.Null(service.Binder);

		var binder = service.OnBind(null);

		var boundServiceBinder = Assert.IsType<BoundServiceBinder>(binder);
		Assert.Same(service, boundServiceBinder.Service);
		Assert.Same(boundServiceBinder, service.Binder);
	}

	[Fact]
	public void StartCommandIsNotSticky()
	{
		var service = new MediaControlsService();

		var result = service.OnStartCommand(null, 0, 1);

		Assert.Equal(Android.App.StartCommandResult.NotSticky, result);
	}

	[Fact]
	public void TaskRemovedOfAServiceWithoutNotificationsOnlyRaisesTheEvent()
	{
		var service = new MediaControlsService();
		var subscriber = new TaskRemovedSubscriber();
		service.TaskRemoved += subscriber.Handle;

		var exception = Record.Exception(() => service.OnTaskRemoved(null));

		Assert.Null(exception);
		Assert.Equal(1, subscriber.Count);
		Assert.Same(service, subscriber.Sender);
		GC.KeepAlive(subscriber);
	}

	[Fact]
	public void UpdatingNotificationsOfAServiceThatNeverStartedThrows()
	{
		var service = new MediaControlsService();
		MediaSession session = null!;
		IExoPlayer player = null!;

		var exception = Assert.Throws<ArgumentNullException>(() => service.UpdateNotifications(session, player));

		Assert.Equal("notificationBuilder", exception.ParamName);
	}

	[Fact]
	public void SettingUpLegacyNotificationsWithoutASessionThrows()
	{
		var service = new MediaControlsService();
		MediaSession session = null!;
		IExoPlayer player = null!;

		var exception = Assert.Throws<ArgumentNullException>(() => service.SetLegacyNotifications(session, player));

		Assert.Equal("session", exception.ParamName);
		Assert.Null(playerNotificationManagerField.GetValue(service));
	}

	// OnDestroy and Dispose leave the foreground before Android 13 (StopForeground), which an object that the system never attached cannot do:
	// it has no ActivityManager and the call throws a NullPointerException. The real service covers that branch (the two tests that follow
	// the system destroying it, DisposingTheHandlerStopsTheServiceAndRemovesItsNotification and the dispose test above).
	[Fact]
	public void DestroyingAServiceThatNeverStartedDoesNothingOnAndroid13AndLater()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(33), "Before Android 13 OnDestroy calls StopForeground, which needs the ActivityManager that only the system gives a service.");
		var service = new MediaControlsService();

		var exception = Record.Exception(() => service.OnDestroy());

		Assert.Null(exception);
		Assert.Null(service.NotificationManager);
	}

	[Fact]
	public void DisposingAServiceThatNeverStartedWorksOnAndroid13AndLater()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(33), "Before Android 13 Dispose calls StopForeground, which needs the ActivityManager that only the system gives a service.");
		var service = new MediaControlsService();

		var exception = Record.Exception(service.Dispose);

		Assert.Null(exception);
		Assert.Equal(IntPtr.Zero, service.Handle);
	}

	[Fact]
	public void DisposeWithoutDisposingOnlyMarksTheServiceDisposed()
	{
		var service = new MediaControlsService();
		var disposeCore = typeof(MediaControlsService).GetMethod("Dispose", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, [typeof(bool)])!;

		// The finalizer path: no cleanup that talks to the system, and later calls are ignored (the "isDisposed" guard).
		disposeCore.Invoke(service, [false]);
		Assert.NotEqual(IntPtr.Zero, service.Handle);

		var exception = Record.Exception(service.Dispose);

		Assert.Null(exception);
		Assert.Equal(IntPtr.Zero, service.Handle);
	}

	const int sentinelNotificationId = 777;
	const string sentinelChannelId = "sentinel";

	// Android 13 and later need the POST_NOTIFICATIONS runtime permission for a notification that is not the foreground service's. The headless
	// runner (XHarness instrumentation) grants it through UiAutomation; the visual runner has no instrumentation and skips when it is missing.
	static void EnsureNotificationPermission()
	{
		var context = Microsoft.Maui.ApplicationModel.Platform.AppContext;
		if (OperatingSystem.IsAndroidVersionAtLeast(33) && !NotificationManagerCompat.From(context)!.AreNotificationsEnabled())
		{
			MauiTestInstrumentation.Current?.UiAutomation?.GrantRuntimePermission(context.PackageName!, "android.permission.POST_NOTIFICATIONS");
		}

		Skip.IfNot(NotificationManagerCompat.From(context)!.AreNotificationsEnabled(),
			"Needs the POST_NOTIFICATIONS permission, which only the headless runner can grant without a person (Android 13 and later).");
	}

	// A notification that is not the foreground service's, for CancelAll to cancel.
	static async Task PostSentinelNotificationAsync()
	{
		var context = Microsoft.Maui.ApplicationModel.Platform.AppContext;
		var manager = (NotificationManager)context.GetSystemService(Context.NotificationService)!;
		manager.CreateNotificationChannel(new NotificationChannel(sentinelChannelId, sentinelChannelId, NotificationImportance.Low));
		var notification = new NotificationCompat.Builder(context, sentinelChannelId)
			.SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)!
			.SetContentTitle("Device test sentinel")!
			.Build()!;
		NotificationManagerCompat.From(context)!.Notify(sentinelNotificationId, notification);
		await WaitUntilAsync(() => FindSentinelNotification() is not null, "The notification of the test was never posted.");
	}

	static long GetServiceNotificationPostTime() => GetActiveNotifications().FirstOrDefault(notification => notification.Id == serviceNotificationId)?.PostTime ?? 0;

	// Before Android 13 the service's legacy PlayerNotificationManager posts the notification itself whenever the player reports an event. Detaching
	// it from the player (what OnTaskRemoved and OnDestroy do) ends that and drops its pending post; the service's next UpdateNotifications attaches
	// it again. From Android 13 there is none.
	static void DetachLegacyNotificationManager(MediaControlsService service) =>
		((AndroidX.Media3.UI.PlayerNotificationManager?)playerNotificationManagerField.GetValue(service))?.SetPlayer(null);

	static StatusBarNotification? FindSentinelNotification() => GetActiveNotifications().FirstOrDefault(notification => notification.Id == sentinelNotificationId);

	static void CancelSentinelNotification() =>
		NotificationManagerCompat.From(Microsoft.Maui.ApplicationModel.Platform.AppContext)!.Cancel(sentinelNotificationId);

	// The Play action of the legacy PlayerNotificationManager's notification (before Android 13), once the manager has shown it for the paused
	// player. Sending its PendingIntent is what a tap on the action does.
	static async Task<PendingIntent> GetLegacyPlayActionAsync()
	{
		var context = Microsoft.Maui.ApplicationModel.Platform.AppContext;
		var playTitle = context.GetString(context.Resources!.GetIdentifier("exo_controls_play_description", "string", context.PackageName));
		Notification.Action? play = null;
		await WaitUntilAsync(() => (play = FindServiceNotification()?.Actions?.FirstOrDefault(action => action.Title?.ToString() == playTitle)) is not null,
			"The legacy notification never showed its Play action.");
		return play!.ActionIntent ?? throw new InvalidOperationException("The Play action has no intent.");
	}

	// Removes one of the things UpdateNotifications needs from the manager (private state, set through reflection) and returns the way back.
	static Action TakeAway(MediaManager manager, MissingPiece missing)
	{
		const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
		switch (missing)
		{
			case MissingPiece.ForegroundServiceFlag:
				var flag = typeof(MediaManager).GetField("isAndroidForegroundServiceEnabled", instance)!;
				flag.SetValue(manager, false);
				return () => flag.SetValue(manager, true);
			case MissingPiece.Session:
				var sessionField = typeof(MediaManager).GetField("session", instance)!;
				var session = sessionField.GetValue(manager);
				sessionField.SetValue(manager, null);
				return () => sessionField.SetValue(manager, session);
			case MissingPiece.Player:
				var playerProperty = typeof(MediaManager).GetProperty("Player", instance)!;
				var player = playerProperty.GetValue(manager);
				playerProperty.SetValue(manager, null);
				return () => playerProperty.SetValue(manager, player);
			default:
				throw new ArgumentOutOfRangeException(nameof(missing), missing, null);
		}
	}

	// A context whose system services are strings: GetSystemService(Context.NotificationService) is not a NotificationManager.
	sealed class ContextWithoutNotificationManager(Context baseContext) : ContextWrapper(baseContext)
	{
		public override Java.Lang.Object? GetSystemService(string? name) => new Java.Lang.String("not a NotificationManager");
	}

	// The service keeps subscribers weakly (WeakEventManager): a lambda would be collected, so the subscriber is an object that the
	// test keeps alive.
	sealed class TaskRemovedSubscriber
	{
		public int Count { get; private set; }

		public object? Sender { get; private set; }

		public void Handle(object? sender, EventArgs e)
		{
			Sender = sender;
			Count++;
		}
	}
}