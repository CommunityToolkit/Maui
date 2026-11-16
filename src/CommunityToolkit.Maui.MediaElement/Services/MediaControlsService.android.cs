using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Android.App;
using Android.Content;
using Android.Content.PM;
using AndroidX.Media3.Common;
using AndroidX.Media3.DataSource;
using AndroidX.Media3.ExoPlayer;
using AndroidX.Media3.ExoPlayer.TrackSelection;
using AndroidX.Media3.Session;
using CommunityToolkit.Maui.Services;

namespace CommunityToolkit.Maui.Media.Services;

[SupportedOSPlatform("Android26.0")]
[IntentFilter(["androidx.media3.session.MediaSessionService"])]
[Service(Exported = false, Enabled = true, Name = "communityToolkit.maui.media.services", ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
sealed partial class MediaControlsService : MediaSessionService
{
	static readonly Lock runningServiceLock = new();
	static MediaControlsService? runningService;

	readonly Dictionary<string, PlayerRegistration> playerRegistrations = [];
	readonly Lock syncLock = new();

	// The system creates the service, so a MediaElement and the tests can only reach the running instance through the process
	internal static MediaControlsService? Running
	{
		get
		{
			lock (runningServiceLock)
			{
				return runningService;
			}
		}
	}

	internal IReadOnlyCollection<string> RegisteredPlayerIds
	{
		get
		{
			lock (syncLock)
			{
				return [.. playerRegistrations.Keys];
			}
		}
	}

	// A MediaController cannot carry a custom MediaSource (a stream, or a source with HTTP headers), so MediaManager hands it to the player of the service
	internal static bool TryGetExoPlayer(string playerId, [NotNullWhen(true)] out IExoPlayer? exoPlayer)
	{
		exoPlayer = null;
		return Running?.TryGetRegistration(playerId, out _, out exoPlayer) is true;
	}

	internal bool TryGetRegistration(string playerId, [NotNullWhen(true)] out MediaSession? mediaSession, [NotNullWhen(true)] out IExoPlayer? exoPlayer)
	{
		lock (syncLock)
		{
			if (playerRegistrations.TryGetValue(playerId, out var registration))
			{
				mediaSession = registration.MediaSession;
				exoPlayer = registration.ExoPlayer;
				return true;
			}
		}

		mediaSession = null;
		exoPlayer = null;
		return false;
	}

	public override void OnTaskRemoved(Intent? rootIntent)
	{
		PauseAllPlayersAndStopSelf();
		base.OnTaskRemoved(rootIntent);
	}

	public override void OnCreate()
	{
		base.OnCreate();

		lock (runningServiceLock)
		{
			runningService = this;
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			ForgetRunningService();
			ReleaseAllPlayers();
		}
		base.Dispose(disposing);
	}

	public override void OnDestroy()
	{
		ForgetRunningService();
		ReleaseAllPlayers();
		base.OnDestroy();
	}

	public override MediaSession? OnGetSession(MediaSession.ControllerInfo? p0)
	{
		var playerId = p0?.ConnectionHints?.GetString(MediaSessionCallback.PlayerIdKey);

		if (string.IsNullOrWhiteSpace(playerId))
		{
			return null;
		}

		return GetOrCreatePlayerRegistration(playerId).MediaSession;
	}

	internal void ReleasePlayer(string? playerId)
	{
		if (string.IsNullOrWhiteSpace(playerId))
		{
			return;
		}

		PlayerRegistration? registration;
		bool shouldStopService;
		lock (syncLock)
		{
			if (!playerRegistrations.Remove(playerId, out registration))
			{
				return;
			}

			shouldStopService = playerRegistrations.Count is 0;
		}

		registration.Dispose();

		if (shouldStopService)
		{
			StopSelf();
		}
	}

	PlayerRegistration GetOrCreatePlayerRegistration(string playerId)
	{
		lock (syncLock)
		{
			if (playerRegistrations.TryGetValue(playerId, out var registration))
			{
				return registration;
			}

			registration = CreatePlayerRegistration(playerId);
			playerRegistrations.Add(playerId, registration);
			return registration;
		}
	}

	void ForgetRunningService()
	{
		lock (runningServiceLock)
		{
			if (ReferenceEquals(runningService, this))
			{
				runningService = null;
			}
		}
	}

	void ReleaseAllPlayers()
	{
		PlayerRegistration[] registrations;

		lock (syncLock)
		{
			registrations = [.. playerRegistrations.Values];
			playerRegistrations.Clear();
		}

		foreach (var registration in registrations)
		{
			registration.Dispose();
		}
	}

	PlayerRegistration CreatePlayerRegistration(string playerId)
	{
		var (exoPlayer, trackSelector) = ExoPlayerFactory.Create(this);

		var mediaSessionBuilder = new MediaSession.Builder(this, exoPlayer);
		mediaSessionBuilder.SetId(playerId);
		mediaSessionBuilder.SetBitmapLoader(new DataSourceBitmapLoader(this));
		mediaSessionBuilder.SetCallback(new MediaSessionCallback(this));
		if (CreateLaunchAppPendingIntent() is PendingIntent sessionActivity)
		{
			mediaSessionBuilder.SetSessionActivity(sessionActivity);
		}

		var mediaSession = mediaSessionBuilder.Build() ?? throw new InvalidOperationException("MediaSession.Builder.Build() returned null");

		return new PlayerRegistration(mediaSession, exoPlayer, trackSelector);
	}

	PendingIntent? CreateLaunchAppPendingIntent()
	{
		var launchIntent = PackageManager?.GetLaunchIntentForPackage(PackageName ?? string.Empty);
		if (launchIntent is null)
		{
			return null;
		}

		launchIntent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
		return PendingIntent.GetActivity(this, 0, launchIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
	}

	sealed class PlayerRegistration(MediaSession mediaSession, IExoPlayer exoPlayer, DefaultTrackSelector trackSelector) : IDisposable
	{
		public MediaSession MediaSession { get; } = mediaSession;
		public IExoPlayer ExoPlayer { get; } = exoPlayer;
		public DefaultTrackSelector TrackSelector { get; } = trackSelector;

		public void Dispose()
		{
			MediaSession.Release();
			MediaSession.Dispose();
			ExoPlayer.Release();
			ExoPlayer.Dispose();
			TrackSelector.Dispose();
		}
	}
}