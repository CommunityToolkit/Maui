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
	readonly Dictionary<string, PlayerRegistration> playerRegistrations = [];
	readonly Lock syncLock = new();

	public override void OnTaskRemoved(Intent? rootIntent)
	{
		PauseAllPlayersAndStopSelf();
		base.OnTaskRemoved(rootIntent);
	}

	public override void OnCreate()
	{
		base.OnCreate();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			ReleaseAllPlayers();
		}
		base.Dispose(disposing);
	}

	public override void OnDestroy()
	{
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
		var audioAttribute = new AndroidX.Media3.Common.AudioAttributes.Builder()?
			.SetContentType(C.AudioContentTypeMusic)?
			.SetUsage(C.UsageMedia)?
			.Build();

		var trackSelector = new DefaultTrackSelector(this);
		var trackSelectionParameters = trackSelector.BuildUponParameters()?
			.SetPreferredAudioLanguage(C.LanguageUndetermined)?
			.SetPreferredTextLanguage(C.LanguageUndetermined)?
			.SetIgnoredTextSelectionFlags(C.SelectionFlagAutoselect);
		trackSelector.SetParameters((DefaultTrackSelector.Parameters.Builder?)trackSelectionParameters);

		var loadControlBuilder = new DefaultLoadControl.Builder();
		loadControlBuilder.SetBufferDurationsMs(
			minBufferMs: 15000,
			maxBufferMs: 50000,
			bufferForPlaybackMs: 2500,
			bufferForPlaybackAfterRebufferMs: 5000);

		var builder = new ExoPlayerBuilder(this) ?? throw new InvalidOperationException("ExoPlayerBuilder returned null");
		builder.SetTrackSelector(trackSelector);
		builder.SetAudioAttributes(audioAttribute, true);
		builder.SetHandleAudioBecomingNoisy(true);
		builder.SetLoadControl(loadControlBuilder.Build());
		var exoPlayer = builder.Build() ?? throw new InvalidOperationException("ExoPlayerBuilder.Build() returned null");

		var mediaSessionBuilder = new MediaSession.Builder(this, exoPlayer);
		mediaSessionBuilder.SetId(playerId);
		mediaSessionBuilder.SetBitmapLoader(new DataSourceBitmapLoader(this));
		mediaSessionBuilder.SetCallback(new MediaSessionCallback(this));
		var mediaSession = mediaSessionBuilder.Build() ?? throw new InvalidOperationException("MediaSession.Builder.Build() returned null");

		return new PlayerRegistration(mediaSession, exoPlayer, trackSelector);
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