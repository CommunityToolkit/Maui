using System.Reflection;
using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Media3.Session;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Media.Services;
using CommunityToolkit.Maui.Services;
using CommunityToolkit.Maui.Views;
using MauiPlatform = Microsoft.Maui.ApplicationModel.Platform;

namespace CommunityToolkit.Maui.DeviceTests;

// The service is a MediaSessionService with one registration (ExoPlayer and MediaSession) for every MediaElement connection. These tests reach
// the running instance through MediaControlsService.Running, because the system creates the service. The notification and foreground-type
// branches depend on the Android version: each device checks the branch that its version takes (the CI matrix runs API 36 and API 28).
[Category(TestCategory.MediaElementAndroid)]
[Collection(TestCollections.DisableParallelization)]
public class MediaSessionServiceAndroidTests : MediaElementAndroidTestBase
{
	// An element that plays for as long as the test needs the foreground service and its notification
	static Task<MediaElement> CreatePlayingElementAsync(Action<MediaElement>? configure = null) =>
		CreateMediaElementAsync(element =>
		{
			element.ShouldAutoPlay = true;
			element.ShouldLoopPlayback = true;
			element.Source = MediaSource.FromResource(toneAsset);
			configure?.Invoke(element);
		});

	static Task WaitForForegroundAsync() =>
		WaitUntilAsync(() => FindMediaControlsService()?.Foreground == true, "MediaControlsService never entered the foreground.");

	static async Task<Notification> WaitForMediaNotificationAsync(MediaManager manager)
	{
		var session = GetMediaSession(manager)!;
		await WaitUntilAsync(() => FindMediaNotification(session) is not null, "Media3 never showed a media notification bound to the session of the MediaElement.");

		return FindMediaNotification(session)!.Notification!;
	}

	[Fact]
	public async Task DefaultOptionsUseALocalExoPlayerAndNeverStartTheService()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);
		Assert.False(mediaElement.IsAndroidForegroundServiceEnabled);

		await HostAsync(mediaElement, async handler =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);

			Assert.IsType<AndroidX.Media3.ExoPlayer.IExoPlayer>(GetPlayerView(mediaElement).Player, exactMatch: false);
			Assert.Null(FindMediaControlsService());
			Assert.Null(MediaControlsService.Running);
			Assert.Null(GetMediaSession(handler.Manager!));
			Assert.Empty(GetRegisteredPlayerIds());
			Assert.Empty(GetActiveNotifications());
		});
	}

	[Fact]
	public async Task ServiceCanBeEnabledWithTheMediaElementOptions()
	{
		// A variable, not the literal true: the MediaElement source generator turns a literal into assembly-level manifest permissions and into a
		// CommunityToolkit.Maui.Android namespace that would hide every Android.* name of this app. The manifest of the test app declares them.
		var enableForegroundService = true;
		SetupProbeBuilder(configureMediaElementOptions: options => options.SetIsAndroidForegroundServiceEnabled(enableForegroundService));
		var mediaElement = await CreateMediaElementAsync();
		Assert.True(mediaElement.IsAndroidForegroundServiceEnabled);

		await HostAsync(mediaElement, async handler =>
		{
			await WaitForRegistrationAsync(handler.Manager!);
			await WaitForControllerAsync(mediaElement);

			Assert.IsType<MediaController>(GetPlayerView(mediaElement).Player, exactMatch: false);
			var sessionId = Assert.Single(GetRegisteredPlayerIds());
			Assert.StartsWith(((IMediaElement)mediaElement).AndroidPlayerId, sessionId, StringComparison.Ordinal);
		});
	}

	// The service used to be started with a plain intent when the controller connected. Media3 ignores that intent, so nothing called
	// startForeground, and Android ends the app when a service started with startForegroundService does not call it within about 10 seconds.
	[Fact]
	public async Task ConnectingWithoutPlaybackDoesNotCrashTheApp()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync();

		await HostAsync(mediaElement, async handler =>
		{
			await WaitForRegistrationAsync(handler.Manager!);

			Assert.NotNull(MediaControlsService.Running);
			Assert.Single(GetRegisteredPlayerIds());
			var service = FindMediaControlsService();
			Assert.NotNull(service);
			Assert.False(service.Started, "The service was started although nothing plays: the system would end the app when it does not call startForeground in time.");
			Assert.False(service.Foreground, "The service is in the foreground although nothing plays.");
		});
	}

	[Fact]
	public async Task PlayingWithTheServiceRunsItInTheForegroundAsAMediaPlaybackService()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreatePlayingElementAsync();

		await HostAsync(mediaElement, async handler =>
		{
			await WaitForRegistrationAsync(handler.Manager!);
			await WaitForForegroundAsync();

			if (OperatingSystem.IsAndroidVersionAtLeast(29))
			{
				Assert.True(MediaControlsService.Running!.ForegroundServiceType.HasFlag(ForegroundService.TypeMediaPlayback));
			}

			var notification = await WaitForMediaNotificationAsync(handler.Manager!);
			var notificationManager = (NotificationManager)MauiPlatform.AppContext.GetSystemService(Android.Content.Context.NotificationService)!;
			var channel = notificationManager.GetNotificationChannel(notification.ChannelId);
			Assert.NotNull(channel);
			Assert.Equal(NotificationImportance.Low, channel.Importance);
		});
	}

	[Fact]
	public async Task PlayingShowsAMediaNotificationBoundToTheSessionOfThePlayer()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreatePlayingElementAsync();

		await HostAsync(mediaElement, async handler =>
		{
			await WaitForRegistrationAsync(handler.Manager!);

			var notification = await WaitForMediaNotificationAsync(handler.Manager!);

			Assert.True(IsMediaStyle(notification));
			Assert.Equal(NotificationVisibility.Public, notification.Visibility);
			Assert.NotNull(notification.SmallIcon);
		});
	}

	// Media3's DefaultMediaNotificationProvider adds actions on every Android version. Android 13 and later draw the transport controls of a
	// MediaStyle notification from the session, so there only the style and the session token are the contract.
	[Fact]
	public async Task NotificationMatchesTheAndroidVersion()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreatePlayingElementAsync();

		await HostAsync(mediaElement, async handler =>
		{
			await WaitForRegistrationAsync(handler.Manager!);

			var notification = await WaitForMediaNotificationAsync(handler.Manager!);

			Assert.True(IsMediaStyle(notification));
			Assert.True(IsBoundToSession(notification, GetMediaSession(handler.Manager!)!));
			if (!OperatingSystem.IsAndroidVersionAtLeast(33))
			{
				Assert.NotEmpty(notification.Actions ?? []);
			}
		});
	}

	[Fact]
	public async Task NotificationOpensTheAppWhenTapped()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreatePlayingElementAsync();

		await HostAsync(mediaElement, async handler =>
		{
			await WaitForRegistrationAsync(handler.Manager!);

			var notification = await WaitForMediaNotificationAsync(handler.Manager!);

			Assert.NotNull(notification.ContentIntent);
			Assert.Equal(MauiPlatform.AppContext.PackageName, notification.ContentIntent.CreatorPackage);
		});
	}

	[Fact]
	public async Task MetadataOfTheSourceAppearsInTheNotification()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreatePlayingElementAsync(element =>
		{
			element.MetadataTitle = "Title of the tone";
			element.MetadataArtist = "Artist of the tone";
		});

		await HostAsync(mediaElement, async handler =>
		{
			await WaitForRegistrationAsync(handler.Manager!);
			var session = GetMediaSession(handler.Manager!)!;

			await WaitUntilAsync(() => FindMediaNotification(session)?.Notification?.Extras?.GetCharSequence(Notification.ExtraTitle)?.ToString() == "Title of the tone",
				"The notification never showed the title of the source.");

			Assert.Equal("Artist of the tone", FindMediaNotification(session)!.Notification!.Extras!.GetCharSequence(Notification.ExtraText)?.ToString());
		});
	}

	[Fact]
	public async Task ChangingTheSourceUpdatesTheNotificationMetadata()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreatePlayingElementAsync(element => element.MetadataTitle = "First title");

		await HostAsync(mediaElement, async handler =>
		{
			await WaitForRegistrationAsync(handler.Manager!);
			var session = GetMediaSession(handler.Manager!)!;
			await WaitUntilAsync(() => FindMediaNotification(session)?.Notification?.Extras?.GetCharSequence(Notification.ExtraTitle)?.ToString() == "First title",
				"The notification never showed the title of the first source.");

			mediaElement.MetadataTitle = "Second title";
			mediaElement.Source = MediaSource.FromResource(shortToneAsset);

			await WaitUntilAsync(() => FindMediaNotification(session)?.Notification?.Extras?.GetCharSequence(Notification.ExtraTitle)?.ToString() == "Second title",
				"The notification never showed the title of the new source.");
		});
	}

	[Fact]
	public async Task EachElementGetsItsOwnRegistrationAndSession()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);

		await InvokeOnMainThreadAsync(async () =>
		{
			var first = await CreateMediaElementAsync();
			var second = await CreateMediaElementAsync();
			var firstHandler = await ConnectWithServiceAsync(first);
			var secondHandler = await ConnectWithServiceAsync(second);

			Assert.Equal(2, GetRegisteredPlayerIds().Count);
			var firstSession = GetMediaSession(firstHandler.Manager!)!;
			var secondSession = GetMediaSession(secondHandler.Manager!)!;
			Assert.StartsWith(((IMediaElement)first).AndroidPlayerId, firstSession.Id, StringComparison.Ordinal);
			Assert.StartsWith(((IMediaElement)second).AndroidPlayerId, secondSession.Id, StringComparison.Ordinal);
			Assert.NotEqual(firstSession.Id, secondSession.Id);
			Assert.NotEqual(firstSession.Handle, secondSession.Handle);
			Assert.NotEqual(GetServicePlayer(firstHandler.Manager!)!.Handle, GetServicePlayer(secondHandler.Manager!)!.Handle);
		});
	}

	[Fact]
	public async Task ReleasePlayerCommandReleasesOnlyThatRegistration()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);

		await InvokeOnMainThreadAsync(async () =>
		{
			var first = await CreateMediaElementAsync();
			var second = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
			using var secondRecorder = new MediaElementRecorder(second);
			var firstHandler = await ConnectWithServiceAsync(first);
			var secondHandler = await ConnectWithServiceAsync(second);
			var firstKey = firstHandler.Manager!.AndroidPlayerConnectionKey;
			var secondKey = secondHandler.Manager!.AndroidPlayerConnectionKey;
			var controller = await ConnectTestControllerAsync(firstKey);

			using var arguments = new Bundle();
			arguments.PutString(MediaSessionCallback.PlayerIdKey, firstKey);
			controller.SendCustomCommand(new SessionCommand(MediaSessionCallback.ReleasePlayer, new Bundle()), arguments);

			await WaitUntilAsync(() => !GetRegisteredPlayerIds().Contains(firstKey), "The service never released the registration that the command named.");
			Assert.Contains(secondKey, GetRegisteredPlayerIds());
			BuildAndReleaseSession(firstKey);
			await WaitUntilLoadedAsync(second, secondRecorder);
			second.Play();
			await WaitForStateAsync(second, MediaElementState.Playing);
		});
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("unknown")]
	public async Task ReleasePlayerWithABlankOrUnknownIdDoesNothing(string playerId) => await ReleasePlayerDoesNothingAsync(playerId);

	[Fact]
	public async Task ReleasePlayerWithoutAnIdDoesNothing() => await ReleasePlayerDoesNothingAsync(null);

	async Task ReleasePlayerDoesNothingAsync(string? playerId)
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync();

		await HostAsync(mediaElement, async handler =>
		{
			await WaitForRegistrationAsync(handler.Manager!);
			var service = MediaControlsService.Running!;
			var registeredBefore = service.RegisteredPlayerIds.ToArray();

			var exception = Record.Exception(() => service.ReleasePlayer(playerId));

			Assert.Null(exception);
			Assert.Equal(registeredBefore, service.RegisteredPlayerIds);
			Assert.Same(service, MediaControlsService.Running);
		});
	}

	[Fact]
	public async Task DisconnectingTheLastElementReleasesItsPlayerStopsTheServiceAndRemovesTheNotification()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreatePlayingElementAsync();

		await HostAsync(mediaElement, async handler =>
		{
			await WaitForRegistrationAsync(handler.Manager!);
			var session = GetMediaSession(handler.Manager!)!;
			await WaitForMediaNotificationAsync(handler.Manager!);

			await DisconnectAsync(mediaElement);

			await WaitUntilAsync(() => GetRegisteredPlayerIds().Count == 0, "The registration of the disconnected element was never released.");
			await WaitForMediaControlsServiceToStopAsync();
			Assert.Null(FindMediaNotification(session));
		});
	}

	[Fact]
	public async Task DisconnectingOneElementLeavesTheOtherRegistrationAndItsSessionWorking()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);

		await InvokeOnMainThreadAsync(async () =>
		{
			var disconnecting = await CreateMediaElementAsync();
			var remaining = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
			using var recorder = new MediaElementRecorder(remaining);
			await ConnectWithServiceAsync(disconnecting);
			var remainingHandler = await ConnectWithServiceAsync(remaining);
			var remainingKey = remainingHandler.Manager!.AndroidPlayerConnectionKey;
			await WaitUntilLoadedAsync(remaining, recorder);
			var remainingController = await ConnectTestControllerAsync(remainingKey);

			await DisconnectAsync(disconnecting);

			await WaitUntilAsync(() => GetRegisteredPlayerIds().Count == 1, "The registration of the disconnected element was never released.");
			Assert.Equal(remainingKey, Assert.Single(GetRegisteredPlayerIds()));
			remainingController.Play();
			await WaitForStateAsync(remaining, MediaElementState.Playing);
			var notification = await WaitForMediaNotificationAsync(remainingHandler.Manager!);
			Assert.True(IsMediaStyle(notification));
			Assert.True(IsBoundToSession(notification, GetMediaSession(remainingHandler.Manager!)!));
		});
	}

	// Media3 posts the paused notification again with stopForeground(false) after the players are paused, so it stays until its session is
	// released: the notification is not asserted to be gone.
	[Fact]
	public async Task TaskRemovedPausesEveryPlayerAndLeavesTheForeground()
	{
		EnsureNotificationPermission();
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);

		await InvokeOnMainThreadAsync(async () =>
		{
			var playing = await CreatePlayingElementAsync();
			var idle = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(shortToneAsset));
			using var idleRecorder = new MediaElementRecorder(idle);
			var playingHandler = await ConnectWithServiceAsync(playing);
			var idleHandler = await ConnectWithServiceAsync(idle);
			await WaitForStateAsync(playing, MediaElementState.Playing);
			await WaitUntilLoadedAsync(idle, idleRecorder);
			await WaitForForegroundAsync();
			var playingSession = GetMediaSession(playingHandler.Manager!)!;

			MediaControlsService.Running!.OnTaskRemoved(null);

			await WaitUntilAsync(() => !GetServicePlayer(playingHandler.Manager!)!.PlayWhenReady && !GetServicePlayer(idleHandler.Manager!)!.PlayWhenReady,
				"A player was not paused by the removal of the task.");
			await WaitForStateAsync(playing, MediaElementState.Paused);
			await WaitUntilAsync(() => FindMediaControlsService()?.Foreground != true, "The service stayed in the foreground.");
			await WaitUntilAsync(() => FindMediaNotification(playingSession)?.Notification is Notification notification && !notification.Flags.HasFlag(NotificationFlags.ForegroundService),
				"The notification of the paused player still is the foreground notification (or was never posted again).");
		});
	}

	[Fact]
	public async Task TaskRemovedWithoutRegistrationsDoesNotThrow()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync();

		await HostAsync(mediaElement, async handler =>
		{
			await WaitForRegistrationAsync(handler.Manager!);
			var service = MediaControlsService.Running!;
			service.ReleasePlayer(handler.Manager!.AndroidPlayerConnectionKey);
			Assert.Empty(service.RegisteredPlayerIds);

			var exception = Record.Exception(() => service.OnTaskRemoved(null));

			Assert.Null(exception);
		});
	}

	// The system destroys the service once its last controller is gone, and the service releases what it still holds. OnDestroy is never
	// called by the test: Media3's own onDestroy, which the system calls later, throws for a service that was destroyed twice.
	[Fact]
	public async Task DestroyingTheServiceReleasesEveryRegistration()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		const string connectionKey = "destroyed-with-the-service";
		var controller = await ConnectTestControllerAsync(connectionKey);
		using var playerReference = await InvokeOnMainThreadAsync(() =>
		{
			Assert.True(MediaControlsService.TryGetExoPlayer(connectionKey, out var exoPlayer));
			return new JavaObjectReference((Java.Lang.Object)exoPlayer);
		});

		await ReleaseTestControllerAsync(controller);
		await WaitUntilAsync(() => FindMediaControlsService() is null, "The system never destroyed the service after its last controller was released.");

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.Null(MediaControlsService.Running);
			Assert.True(playerReference.GetPeer<AndroidX.Media3.ExoPlayer.IExoPlayer>().IsReleased);
			BuildAndReleaseSession(connectionKey);
		});
	}

	[Fact]
	public async Task DisposingTheServiceAfterTheSystemDestroyedItIsSafe()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync();
		MediaControlsService? service = null;

		await HostAsync(mediaElement, async handler =>
		{
			await WaitForRegistrationAsync(handler.Manager!);
			service = MediaControlsService.Running;
			await DisconnectAsync(mediaElement);
			await WaitForMediaControlsServiceToStopAsync();
		});

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.NotNull(service);
			Assert.Null(Record.Exception(service.Dispose));
			Assert.Null(Record.Exception(service.Dispose));
		});
	}

	[Fact]
	public async Task DisposingAServiceThatNeverStartedDoesNotThrow()
	{
		await InvokeOnMainThreadAsync(() =>
		{
			var service = new MediaControlsService();

			Assert.Null(Record.Exception(service.Dispose));
		});
	}

	// Dispose(bool) is protected on a sealed class: reflection is the only way to run the finalizer path, which must leave the registrations alone.
	[Fact]
	public async Task DisposeWithoutDisposingReleasesNoRegistration()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			await WaitForRegistrationAsync(handler.Manager!);
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var disposeCore = typeof(MediaControlsService).GetMethod("Dispose", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, [typeof(bool)])!;

			disposeCore.Invoke(MediaControlsService.Running, [false]);

			Assert.Single(GetRegisteredPlayerIds());
			Assert.False(GetServicePlayer(handler.Manager!)!.IsReleased);
			mediaElement.Play();
			await WaitForStateAsync(mediaElement, MediaElementState.Playing);
		});
	}

	[Fact]
	public async Task ControllerWithoutAPlayerIdIsRejected()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);

		await Assert.ThrowsAnyAsync<Exception>(() => ConnectTestControllerAsync(connectionKey: null));

		Assert.Empty(GetRegisteredPlayerIds());
	}

	[Fact]
	public async Task ServiceStartsInTheForegroundAgainForANewElementAfterItStopped()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);

		await InvokeOnMainThreadAsync(async () =>
		{
			var first = await CreatePlayingElementAsync();
			await ConnectWithServiceAsync(first);
			await WaitForForegroundAsync();
			await DisconnectAsync(first);
			await WaitForMediaControlsServiceToStopAsync();

			var second = await CreatePlayingElementAsync();
			var secondHandler = await ConnectWithServiceAsync(second);

			await WaitForForegroundAsync();
			var notification = await WaitForMediaNotificationAsync(secondHandler.Manager!);
			Assert.True(IsMediaStyle(notification));
		});
	}
}
