using AndroidX.Media3.UI;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.MediaElementAndroid)]
[Collection(TestCollections.DisableParallelization)]
public class MediaManagerPropertyAndroidTests : MediaElementAndroidTestBase
{
	// A change that is applied to the player may be reported back asynchronously (OnVolumeChanged, OnPlaybackParametersChanged),
	// so floating point values are compared to float precision.
	const float tolerance = 0.0001f;

	[Theory]
	[InlineData(Aspect.AspectFill, AspectRatioFrameLayout.ResizeModeZoom)]
	[InlineData(Aspect.Fill, AspectRatioFrameLayout.ResizeModeFill)]
	[InlineData(Aspect.AspectFit, AspectRatioFrameLayout.ResizeModeFit)]
	[InlineData(Aspect.Center, AspectRatioFrameLayout.ResizeModeFit)]
	public async Task AspectMapsToTheResizeModeOfThePlayerView(Aspect aspect, int expectedResizeMode)
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			var playerView = GetPlayerView(mediaElement);
			Assert.Equal(AspectRatioFrameLayout.ResizeModeFit, playerView.ResizeMode);

			// Start from a different mode so that the mapping of the target has to change something.
			mediaElement.Aspect = expectedResizeMode == AspectRatioFrameLayout.ResizeModeFill ? Aspect.AspectFill : Aspect.Fill;
			Assert.NotEqual(expectedResizeMode, playerView.ResizeMode);

			mediaElement.Aspect = aspect;

			Assert.Equal(expectedResizeMode, playerView.ResizeMode);
		});
	}

	[Fact]
	public async Task UnsupportedAspectThrowsNotSupported()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			var exception = Assert.Throws<NotSupportedException>(() => mediaElement.Aspect = (Aspect)99);

			Assert.Contains("99", exception.Message);
			Assert.Contains("is not yet supported", exception.Message);
			Assert.Equal(AspectRatioFrameLayout.ResizeModeFit, GetPlayerView(mediaElement).ResizeMode);
		});
	}

	[Fact]
	public async Task ShouldShowPlaybackControlsMapsToUseController()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.ShouldShowPlaybackControls = true);

		await WithHandlerAsync(mediaElement, handler =>
		{
			var playerView = GetPlayerView(mediaElement);
			Assert.True(playerView.UseController);

			mediaElement.ShouldShowPlaybackControls = false;
			Assert.False(playerView.UseController);

			mediaElement.ShouldShowPlaybackControls = true;
			Assert.True(playerView.UseController);
		});
	}

	[Fact]
	public async Task ShouldKeepScreenOnMapsToKeepScreenOnOfThePlayerView()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.ShouldKeepScreenOn = true);

		await WithHandlerAsync(mediaElement, handler =>
		{
			var playerView = GetPlayerView(mediaElement);
			Assert.True(playerView.KeepScreenOn);

			mediaElement.ShouldKeepScreenOn = false;
			Assert.False(playerView.KeepScreenOn);

			mediaElement.ShouldKeepScreenOn = true;
			Assert.True(playerView.KeepScreenOn);
		});
	}

	// The MediaElement is set up so that the Player or PlayerView guard is the only thing that stops each member from using the missing
	// object: it has a Source (Play, Pause and Stop return without one), it is not muted (UpdateVolume and UpdateShouldMute take another path
	// while muted), it has a duration (UpdateStatus does nothing without one), a positive speed and an Aspect that has no resize mode.
	// Without its guard a member throws (NullReferenceException, or NotSupportedException for the Aspect).
	[Fact]
	public async Task UpdatesWithoutAPlayerViewOrPlayerAreIgnored()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.Aspect = (Aspect)99;
			element.ShouldShowPlaybackControls = true;
			element.ShouldKeepScreenOn = true;
			element.ShouldMute = false;
			element.ShouldLoopPlayback = true;
			element.Speed = 2;
			element.Volume = 0.5;
			element.Source = MediaSource.FromResource(toneAsset);
			((IMediaElement)element).Duration = TimeSpan.FromSeconds(1);
		});
		DisposeWhenTheTestEnds(mediaElement);

		await InvokeOnMainThreadAsync(() =>
		{
			using var manager = new AndroidMediaManagerProbe(MauiContext, mediaElement, TestDispatcher.Current);
			var members = new Dictionary<string, Action>
			{
				[nameof(MediaManager.UpdateAspect)] = manager.UpdateAspect,
				[nameof(MediaManager.UpdateShouldShowPlaybackControls)] = manager.UpdateShouldShowPlaybackControls,
				[nameof(MediaManager.UpdateShouldKeepScreenOn)] = manager.UpdateShouldKeepScreenOn,
				[nameof(MediaManager.UpdateShouldMute)] = manager.UpdateShouldMute,
				[nameof(MediaManager.UpdateShouldLoopPlayback)] = manager.UpdateShouldLoopPlayback,
				[nameof(MediaManager.UpdateSpeed)] = manager.UpdateSpeed,
				[nameof(MediaManager.UpdateVolume)] = manager.UpdateVolume,
				[nameof(MediaManager.UpdateStatus)] = manager.UpdateStatus,
				[nameof(MediaManager.Play)] = manager.Play,
				[nameof(MediaManager.Pause)] = manager.Pause,
				[nameof(MediaManager.Stop)] = manager.Stop,
			};

			var failures = members
				.Select(member => (member.Key, Exception: Record.Exception(member.Value)))
				.Where(result => result.Exception is not null)
				.Select(result => $"{result.Key}: {result.Exception!.GetType().Name}")
				.ToArray();

			Assert.Empty(failures);
			Assert.Null(manager.ExposedPlayer);
			Assert.Null(manager.ExposedPlayerView);
			Assert.Equal(TimeSpan.FromSeconds(1), mediaElement.Duration);
		});
	}

	[Fact]
	public async Task VolumeMapsToTheVolumeOfThePlayer()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			var player = GetPlayer(mediaElement);

			mediaElement.Volume = 0.3;
			Assert.Equal(0.3f, player.Volume, tolerance);

			mediaElement.Volume = 0;
			Assert.Equal(0f, player.Volume, tolerance);

			mediaElement.Volume = 1;
			Assert.Equal(1f, player.Volume, tolerance);
		});
	}

	[Fact]
	public async Task MutingSilencesThePlayerAndUnmutingRestoresTheVolume()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			var player = GetPlayer(mediaElement);
			mediaElement.Volume = 0.6;
			Assert.Equal(0.6f, player.Volume, tolerance);

			mediaElement.ShouldMute = true;
			Assert.Equal(0f, player.Volume, tolerance);

			mediaElement.ShouldMute = false;
			Assert.Equal(0.6f, player.Volume, tolerance);
		});
	}

	[Fact]
	public async Task VolumeChangedWhileMutedIsAppliedWhenUnmuting()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			var player = GetPlayer(mediaElement);
			mediaElement.Volume = 0.6;
			mediaElement.ShouldMute = true;

			mediaElement.Volume = 0.9;

			Assert.Equal(0f, player.Volume, tolerance);

			mediaElement.ShouldMute = false;
			Assert.Equal(0.9f, player.Volume, tolerance);
		});
	}

	// When the handler connects, Volume is mapped before ShouldMute, whose unmuted branch compares the player volume with volumeBeforeMute
	// (which starts at 1): the volume was reset to 1 while AreFloatingPointNumbersEqual was inverted (#3327).
	[Fact]
	public async Task VolumeSetBeforeTheHandlerConnectsSurvivesTheConnection()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Volume = 0.4);

		await WithHandlerAsync(mediaElement, handler =>
		{
			var player = GetPlayer(mediaElement);

			Assert.Equal(0.4f, player.Volume, tolerance);
			Assert.Equal(0.4, mediaElement.Volume, 3);
		});
	}

	// The same comparison, reached by calling the public UpdateShouldMute again while not muted.
	[Fact]
	public async Task UpdateShouldMuteWhileUnmutedKeepsTheCurrentVolume()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();

		await WithHandlerAsync(mediaElement, handler =>
		{
			var player = GetPlayer(mediaElement);
			mediaElement.Volume = 0.5;
			Assert.Equal(0.5f, player.Volume, tolerance);

			handler.Manager!.UpdateShouldMute();

			Assert.Equal(0.5f, player.Volume, tolerance);
		});
	}

	// When the MediaElement starts muted, the player is still at its default volume of 1 when ShouldMute records the volume to restore, so
	// that volume must come from the MediaElement.
	[Fact]
	public async Task MediaElementThatStartsMutedUnmutesToItsVolume()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.Volume = 0.4;
			element.ShouldMute = true;
		});

		await WithHandlerAsync(mediaElement, handler =>
		{
			var player = GetPlayer(mediaElement);
			Assert.Equal(0f, player.Volume, tolerance);

			mediaElement.ShouldMute = false;

			Assert.Equal(0.4f, player.Volume, tolerance);
		});
	}

	[Fact]
	public async Task SpeedMapsToThePlaybackSpeedOfThePlayer()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Speed = 1.5);

		await WithHandlerAsync(mediaElement, handler =>
		{
			var player = GetPlayer(mediaElement);
			Assert.Equal(1.5f, player.PlaybackParameters!.Speed, tolerance);

			mediaElement.Speed = 0.5;
			Assert.Equal(0.5f, player.PlaybackParameters.Speed, tolerance);
		});
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public async Task SpeedZeroOrBelowPausesThePlayerAndKeepsItsPlaybackSpeed(double speed)
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Speed = 2);

		await WithHandlerAsync(mediaElement, handler =>
		{
			var player = GetPlayer(mediaElement);
			player.PlayWhenReady = true;

			mediaElement.Speed = speed;

			Assert.False(player.PlayWhenReady);
			// ExoPlayer rejects a playback speed of zero or less, so the player keeps the last valid speed.
			Assert.Equal(2f, player.PlaybackParameters!.Speed, tolerance);
		});
	}

	[Fact]
	public async Task RestoringAPositiveSpeedAfterZeroResumesPlayback()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Speed = 1);

		await WithHandlerAsync(mediaElement, handler =>
		{
			var player = GetPlayer(mediaElement);
			mediaElement.Speed = 0;
			Assert.False(player.PlayWhenReady);

			mediaElement.Speed = 1.25;

			Assert.True(player.PlayWhenReady);
			Assert.Equal(1.25f, player.PlaybackParameters!.Speed, tolerance);
		});
	}

	[Fact]
	public async Task ChangingBetweenPositiveSpeedsDoesNotStartPlayback()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Speed = 1);

		await WithHandlerAsync(mediaElement, handler =>
		{
			var player = GetPlayer(mediaElement);
			Assert.False(player.PlayWhenReady);

			mediaElement.Speed = 2;

			Assert.False(player.PlayWhenReady);
			Assert.Equal(2f, player.PlaybackParameters!.Speed, tolerance);
		});
	}
}