using System.Reflection;
using AndroidX.Media3.Common;
using AndroidX.Media3.ExoPlayer;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CommunityToolkit.Maui.Views;
using PlayerConsts = AndroidX.Media3.Common.BasePlayer.InterfaceConsts;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.MediaElementAndroid)]
[Collection(TestCollections.DisableParallelization)]
public class MediaManagerSourceAndroidTests : MediaElementAndroidTestBase
{
	const string package = "com.communitytoolkit.maui.mediaelement.devicetests";

	public enum EmptySourceKind
	{
		UriWithoutUri,
		FileWithoutPath,
		FileWithWhitespacePath,
		ResourceWithoutPath,
		StreamWithoutStream,
	}

	[Fact]
	public async Task ResourceSourcePlaysTheAssetFromTheApk()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.ShouldAutoPlay = true;
			element.Source = MediaSource.FromResource(shortToneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilAsync(() => recorder.EndedCount > 0, "The 0.6 s asset never played to its end.");
			var player = GetPlayer(mediaElement);

			Assert.Equal(1, recorder.OpenedCount);
			Assert.Equal(1, recorder.EndedCount);
			Assert.Empty(recorder.FailureMessages);
			Assert.Equal((MediaElementState.None, MediaElementState.Opening), recorder.Transitions[0]);
			Assert.Contains(MediaElementState.Playing, recorder.States);
			Assert.Equal(MediaElementState.Stopped, mediaElement.CurrentState);
			Assert.Equal(shortToneDuration, mediaElement.Duration);
			Assert.Equal(PlayerConsts.StateEnded, player.PlaybackState);
			Assert.Equal(1, player.MediaItemCount);
			Assert.Equal($"asset://{package}:Assets/{shortToneAsset}", player.CurrentMediaItem!.MediaId);
			Assert.Equal($"asset://{package}:Assets/{shortToneAsset}", player.CurrentMediaItem.PlaybackProperties!.Uri!.ToString());
		});
	}

	[Fact]
	public async Task ResourceSourceThatDoesNotExistRaisesMediaFailed()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource("exoplayer/does_not_exist.wav"));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilAsync(() => recorder.FailureMessages.Count > 0, "MediaFailed was never raised.");
			var player = GetPlayer(mediaElement);

			Assert.Equal(MediaElementState.Failed, mediaElement.CurrentState);
			Assert.Contains("Error code:", recorder.FailureMessages[0]);
			Assert.Equal(TimeSpan.Zero, mediaElement.Duration);
			Assert.Equal(TimeSpan.Zero, mediaElement.Position);
			Assert.NotNull(player.PlayerError);
		});
	}

	[Fact]
	public async Task ValidSourceAfterAFailedSourcePlaysAgain()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource("exoplayer/does_not_exist.wav"));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitForStateAsync(mediaElement, MediaElementState.Failed);
			var openedBefore = recorder.OpenedCount;
			var player = GetPlayer(mediaElement);

			mediaElement.Source = MediaSource.FromResource(toneAsset);

			await WaitUntilAsync(() => mediaElement.Duration == toneDuration, "The valid source was never loaded.");
			Assert.Equal(openedBefore + 1, recorder.OpenedCount);
			Assert.Null(player.PlayerError);
			Assert.Equal(MediaElementState.Paused, mediaElement.CurrentState);
			Assert.Equal(1, player.MediaItemCount);
		});
	}

	[Fact]
	public async Task FileSourceLoadsAVideoAndReportsItsSize()
	{
		SkipWhereTheVideoDecoderIsUnreliable();
		SetupProbeBuilder();
		var path = await CopyAssetToCacheAsync(videoAsset);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromFile(path));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder, videoWaitTimeout);
			var player = GetPlayer(mediaElement);
			await WaitUntilVideoSizeIsKnownAsync(mediaElement, recorder);

			Assert.Equal(TimeSpan.FromSeconds(2), mediaElement.Duration);
			Assert.Equal((64, 48), (mediaElement.MediaWidth, mediaElement.MediaHeight));
			Assert.Equal((64, 48), (player.VideoSize!.Width, player.VideoSize.Height));
			Assert.Equal(path, player.CurrentMediaItem!.MediaId);
			Assert.Equal(path, player.CurrentMediaItem.PlaybackProperties!.Uri!.ToString());
		});
	}

	[Fact]
	public async Task UriSourceIsDownloadedFromAnHttpServer()
	{
		using var server = new LoopbackHttpServer(new Dictionary<string, (byte[], string)> { ["/tone.wav"] = (await ReadAssetAsync(toneAsset), "audio/wav") });
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromUri(server.GetUri("/tone.wav")));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var player = GetPlayer(mediaElement);

			Assert.Equal(toneDuration, mediaElement.Duration);
			Assert.True(server.HasRequest("GET", "/tone.wav"));
			Assert.Null(server.GetRequestHeader("GET", "/tone.wav", "X-Device-Test"));
			Assert.Equal(server.GetUri("/tone.wav").ToString(), player.CurrentMediaItem!.PlaybackProperties!.Uri!.ToString());
		});
	}

	[Fact]
	public async Task UriSourceWithHeadersSendsThemWithTheRequest()
	{
		using var server = new LoopbackHttpServer(new Dictionary<string, (byte[], string)> { ["/tone.wav"] = (await ReadAssetAsync(toneAsset), "audio/wav") });
		SetupProbeBuilder();
		var headers = new Dictionary<string, string> { ["X-Device-Test"] = "42", ["Authorization"] = "Bearer abc" };
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromUri(server.GetUri("/tone.wav"), headers));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);

			Assert.Equal(toneDuration, mediaElement.Duration);
			Assert.Equal("42", server.GetRequestHeader("GET", "/tone.wav", "X-Device-Test"));
			Assert.Equal("Bearer abc", server.GetRequestHeader("GET", "/tone.wav", "Authorization"));
		});
	}

	// UriMediaSource reports a change of its HttpHeaders as a source change, so the source mapping runs again.
	[Fact]
	public async Task ChangingTheHeadersOfAUriSourceLoadsTheMediaAgainWithTheNewValues()
	{
		using var server = new LoopbackHttpServer(new Dictionary<string, (byte[], string)> { ["/tone.wav"] = (await ReadAssetAsync(toneAsset), "audio/wav") });
		SetupProbeBuilder();
		var source = (UriMediaSource)MediaSource.FromUri(server.GetUri("/tone.wav"), new Dictionary<string, string> { ["X-Device-Test"] = "first" });
		var mediaElement = await CreateMediaElementAsync(element => element.Source = source);
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			Assert.Equal("first", server.GetRequestHeader("GET", "/tone.wav", "X-Device-Test"));

			source.HttpHeaders["X-Device-Test"] = "second";

			await WaitUntilAsync(() => server.Requests.Any(request => request.Contains("X-Device-Test: second", StringComparison.OrdinalIgnoreCase)), "The media was never requested again with the new header.");
			Assert.Equal(2, recorder.OpenedCount);
		});
	}

	[Fact]
	public async Task FileSourceAcceptsAFileUri()
	{
		SetupProbeBuilder();
		var path = await CopyAssetToCacheAsync(shortToneAsset);
		var fileUri = new Uri(path).AbsoluteUri;
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromFile(fileUri));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);

			Assert.StartsWith("file:///", fileUri);
			Assert.Equal(shortToneDuration, mediaElement.Duration);
			Assert.Equal(fileUri, GetPlayer(mediaElement).CurrentMediaItem!.MediaId);
		});
	}

	[Fact]
	public async Task UriSourceThatTheServerDoesNotHaveRaisesMediaFailed()
	{
		using var server = new LoopbackHttpServer(new Dictionary<string, (byte[], string)>());
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromUri(server.GetUri("/missing.wav")));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilAsync(() => recorder.FailureMessages.Count > 0, "MediaFailed was never raised.");

			Assert.Contains("ERROR_CODE_IO_BAD_HTTP_STATUS", recorder.FailureMessages[0]);
			Assert.Equal(MediaElementState.Failed, mediaElement.CurrentState);
			Assert.True(server.HasRequest("GET", "/missing.wav"));
		});
	}

	[Fact]
	public async Task StreamSourcePlaysFromTheStream()
	{
		var tone = await ReadAssetAsync(toneAsset);
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromStream(new MemoryStream(tone)));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var player = GetPlayer(mediaElement);

			Assert.Equal(toneDuration, mediaElement.Duration);
			Assert.Equal(MediaElementState.Paused, mediaElement.CurrentState);
			Assert.Equal("stream://media", player.CurrentMediaItem!.MediaId);
			Assert.Equal("stream://media", player.CurrentMediaItem.PlaybackProperties!.Uri!.ToString());

			mediaElement.Play();

			await WaitUntilAsync(() => player.IsPlaying, "The stream never played.");
			// Seeking makes ExoPlayer open the data source again at the new position.
			await mediaElement.SeekTo(TimeSpan.FromSeconds(2));
			Assert.True(player.CurrentPosition >= 2000);
			await WaitForStateAsync(mediaElement, MediaElementState.Playing);
		});
	}

	// A stream that cannot seek is read once, from its start, so a seek back to a part of the media that the player no longer holds (to 0.2 s
	// while it plays at 1.5 s) fails the source (MediaFailed) and the app goes on. The data source used to throw a managed exception there, on
	// ExoPlayer's loading thread, which ended the app.
	[Fact]
	public async Task SeekBackInAStreamThatCannotSeekRaisesMediaFailed()
	{
		var tone = await ReadAssetAsync(toneAsset);
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.ShouldMute = true;
			element.Source = MediaSource.FromStream(new NonSeekableStream(tone));
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var player = GetPlayer(mediaElement);
			mediaElement.Play();
			await WaitUntilAsync(() => player.CurrentPosition > 1500, "The stream never played past 1.5 s.");

			// Not awaited: what is tested is the failure of the source.
			_ = mediaElement.SeekTo(TimeSpan.FromSeconds(0.2));

			await WaitUntilAsync(() => recorder.FailureMessages.Count > 0, "The seek that the stream cannot serve was never reported.");
			var failure = Assert.Single(recorder.FailureMessages);
			Assert.Contains("ERROR_CODE_IO_READ_POSITION_OUT_OF_RANGE", failure);
			Assert.Equal(MediaElementState.Failed, mediaElement.CurrentState);
		});
	}

	// The managed exception of a stream that fails to read used to reach ExoPlayer's loading thread, which ended the app.
	[Fact]
	public async Task StreamThatFailsToReadIsReportedAsFailed()
	{
		var tone = await ReadAssetAsync(toneAsset);
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			mediaElement.Source = MediaSource.FromStream(new FailingStream(tone));

			// ExoPlayer retries a failed load three times, the last one 2 s after the previous failure, before it reports the error.
			await WaitUntilAsync(() => recorder.FailureMessages.Count > 0, "The stream that fails to read was never reported.", timeout: 15000);
			var failure = Assert.Single(recorder.FailureMessages);
			Assert.Contains("ERROR_CODE_IO_UNSPECIFIED", failure);
			Assert.Equal(MediaElementState.Failed, mediaElement.CurrentState);
		});
	}

	[Fact]
	public async Task ReplacingAStreamSourceWithAnotherSourceDisposesTheStreamDataSourceFactory()
	{
		var tone = await ReadAssetAsync(toneAsset);
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromStream(new MemoryStream(tone)));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var factory = GetStreamDataSourceFactory(handler.Manager!);
			Assert.NotNull(factory);
			Assert.NotEqual(IntPtr.Zero, factory.Handle);

			mediaElement.Source = MediaSource.FromResource(shortToneAsset);

			await WaitUntilAsync(() => mediaElement.Duration == shortToneDuration, "The replacing source was never loaded.");
			Assert.Null(GetStreamDataSourceFactory(handler.Manager!));
			Assert.Equal(IntPtr.Zero, factory.Handle);
		});
	}

	[Fact]
	public async Task ReplacingAStreamSourceWithAnotherStreamDisposesThePreviousFactory()
	{
		var tone = await ReadAssetAsync(toneAsset);
		var shortTone = await ReadAssetAsync(shortToneAsset);
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromStream(new MemoryStream(tone)));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var first = GetStreamDataSourceFactory(handler.Manager!);
			Assert.NotNull(first);

			mediaElement.Source = MediaSource.FromStream(new MemoryStream(shortTone));

			await WaitUntilAsync(() => mediaElement.Duration == shortToneDuration, "The second stream was never loaded.");
			var second = GetStreamDataSourceFactory(handler.Manager!);
			Assert.NotNull(second);
			Assert.NotSame(first, second);
			Assert.Equal(IntPtr.Zero, first.Handle);
			Assert.NotEqual(IntPtr.Zero, second.Handle);
		});
	}

	[Fact]
	public async Task ClearingAStreamSourceDisposesTheStreamDataSourceFactoryAndEmptiesThePlayer()
	{
		var tone = await ReadAssetAsync(toneAsset);
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromStream(new MemoryStream(tone)));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var factory = GetStreamDataSourceFactory(handler.Manager!);
			Assert.NotNull(factory);

			mediaElement.Source = null;

			await WaitUntilAsync(() => mediaElement.CurrentState == MediaElementState.None, "The state never went back to None.");
			Assert.Null(GetStreamDataSourceFactory(handler.Manager!));
			Assert.Equal(IntPtr.Zero, factory.Handle);
			Assert.Equal(0, GetPlayer(mediaElement).MediaItemCount);
		});
	}

	[Fact]
	public async Task ClearingTheSourceEmptiesThePlayerAndResetsDurationAndState()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var player = GetPlayer(mediaElement);
			Assert.Equal(1, player.MediaItemCount);

			mediaElement.Source = null;

			Assert.Equal(0, player.MediaItemCount);
			Assert.Equal(TimeSpan.Zero, mediaElement.Duration);
			Assert.Equal(MediaElementState.None, mediaElement.CurrentState);
		});
	}

	[Fact]
	public async Task ChangingTheSourceReplacesTheMediaItem()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var player = GetPlayer(mediaElement);
			var tokenSource = (CancellationTokenSource?)GetPrivateField(handler.Manager!, "cancellationTokenSource");
			Assert.NotNull(tokenSource);

			mediaElement.Source = MediaSource.FromResource(shortToneAsset);

			await WaitUntilAsync(() => mediaElement.Duration == shortToneDuration, "The new source was never loaded.");
			Assert.Equal(1, player.MediaItemCount);
			Assert.EndsWith(shortToneAsset, player.CurrentMediaItem!.MediaId);
			Assert.Equal(2, recorder.OpenedCount);
			Assert.NotSame(tokenSource, GetPrivateField(handler.Manager!, "cancellationTokenSource"));
			Assert.True(tokenSource.IsCancellationRequested);
		});
	}

	[Fact]
	public async Task ShouldAutoPlayStartsPlaybackWhenTheSourceIsSet()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.ShouldAutoPlay = true;
			element.Source = MediaSource.FromResource(toneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			var player = GetPlayer(mediaElement);
			Assert.True(player.PlayWhenReady);

			await WaitForStateAsync(mediaElement, MediaElementState.Playing);
			await WaitUntilAsync(() => player.IsPlaying, "The player never started to play.");
		});
	}

	[Fact]
	public async Task WithoutShouldAutoPlayTheLoadedMediaWaitsPaused()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			var player = GetPlayer(mediaElement);
			Assert.False(player.PlayWhenReady);

			await WaitUntilLoadedAsync(mediaElement, recorder);
			await WaitForStateAsync(mediaElement, MediaElementState.Paused);
			Assert.False(player.IsPlaying);
			Assert.Equal(0, player.CurrentPosition);
		});
	}

	[Theory]
	[InlineData(EmptySourceKind.UriWithoutUri)]
	[InlineData(EmptySourceKind.FileWithoutPath)]
	[InlineData(EmptySourceKind.FileWithWhitespacePath)]
	[InlineData(EmptySourceKind.ResourceWithoutPath)]
	[InlineData(EmptySourceKind.StreamWithoutStream)]
	public async Task SourceWithoutDataLoadsNothing(EmptySourceKind kind)
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = CreateEmptySource(kind));
		using var recorder = new MediaElementRecorder(mediaElement);

		await WithHandlerAsync(mediaElement, handler =>
		{
			var player = GetPlayer(mediaElement);

			Assert.Equal(0, player.MediaItemCount);
			Assert.Equal(PlayerConsts.StateIdle, player.PlaybackState);
			Assert.Equal(0, recorder.OpenedCount);
			Assert.Empty(recorder.FailureMessages);
		});
	}

	// The state is set to Opening before the player data is built, so a source without data has to report None again, like a null Source (and
	// like iOS for a source without data, MediaManager.macios.cs).
	[Theory]
	[InlineData(EmptySourceKind.UriWithoutUri)]
	[InlineData(EmptySourceKind.FileWithoutPath)]
	[InlineData(EmptySourceKind.ResourceWithoutPath)]
	[InlineData(EmptySourceKind.StreamWithoutStream)]
	public async Task SourceWithoutDataReportsStateNone(EmptySourceKind kind)
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = CreateEmptySource(kind));

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilAsync(() => mediaElement.CurrentState is not MediaElementState.Opening, $"The MediaElement still reports {mediaElement.CurrentState} for a source without data.", 2000);

			Assert.Equal(MediaElementState.None, mediaElement.CurrentState);
		});
	}

	// Like a null Source, a source without data takes the previous media out of the player without playing it to its end. It is not a failure, on
	// every platform.
	[Theory]
	[InlineData(EmptySourceKind.UriWithoutUri)]
	[InlineData(EmptySourceKind.FileWithoutPath)]
	[InlineData(EmptySourceKind.FileWithWhitespacePath)]
	[InlineData(EmptySourceKind.ResourceWithoutPath)]
	[InlineData(EmptySourceKind.StreamWithoutStream)]
	public async Task SourceWithoutDataAfterAValidSourceEmptiesThePlayer(EmptySourceKind kind)
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var player = GetPlayer(mediaElement);

			mediaElement.Source = CreateEmptySource(kind);

			Assert.Equal(0, player.MediaItemCount);
			Assert.Equal(PlayerConsts.StateIdle, player.PlaybackState);
			Assert.Equal(TimeSpan.Zero, mediaElement.Duration);
			Assert.Equal(MediaElementState.None, mediaElement.CurrentState);
			Assert.Equal(0, recorder.EndedCount);
			Assert.Empty(recorder.FailureMessages);
		});
	}

	[Fact]
	public async Task SourceWithoutDataAfterAStreamSourceDisposesTheStreamDataSourceFactory()
	{
		var tone = await ReadAssetAsync(toneAsset);
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromStream(new MemoryStream(tone)));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var factory = GetStreamDataSourceFactory(handler.Manager!);
			Assert.NotNull(factory);

			mediaElement.Source = new StreamMediaSource();

			Assert.Equal(MediaElementState.None, mediaElement.CurrentState);
			Assert.Null(GetStreamDataSourceFactory(handler.Manager!));
			Assert.Equal(IntPtr.Zero, factory.Handle);
			Assert.Equal(0, GetPlayer(mediaElement).MediaItemCount);
		});
	}

	// UpdateSource returns before it asks for the player data when the Source is null, so the null guard of the private SetPlayerData can only be
	// reached by calling it. Without the guard the null Source would end in the "not yet supported" arm.
	[Fact]
	public async Task PlayerDataOfAMediaElementWithoutASourceIsNothing()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();

		await WithHandlerAsync(mediaElement, async handler =>
		{
			var setPlayerData = typeof(MediaManager).GetMethod("SetPlayerData", BindingFlags.Instance | BindingFlags.NonPublic)!;
			Assert.Null(mediaElement.Source);

			var result = await (Task<MediaItem.Builder?>)setPlayerData.Invoke(handler.Manager, [CancellationToken.None])!;

			Assert.Null(result);
			Assert.Equal(0, GetPlayer(mediaElement).MediaItemCount);
		});
	}

	// The asset path of a resource source starts with the package name of the PlayerView's context, so a manager that has a player but no
	// PlayerView (a subclass can give it one) has no package name to put there.
	[Fact]
	public async Task ResourceSourceOfAManagerWithoutAPlayerViewUsesAnEmptyPackageName()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		DisposeWhenTheTestEnds(mediaElement);

		await InvokeOnMainThreadAsync(async () =>
		{
			var player = new ExoPlayerBuilder(MauiContext.Context!).Build()!;
			try
			{
				using var manager = new AndroidMediaManagerProbe(MauiContext, mediaElement, TestDispatcher.Current);
				manager.AttachPlayer(player);
				Assert.Null(manager.ExposedPlayerView);

				await manager.UpdateSource();

				Assert.Equal(1, player.MediaItemCount);
				Assert.Equal($"asset://{Path.PathSeparator}Assets/{toneAsset}", player.CurrentMediaItem!.MediaId);
			}
			finally
			{
				// The manager is not the player's listener here (it did not create the player), so the player can simply be released.
				player.Release();
			}
		});
	}

	// Every load has a token source of its own, which the next source cancels, and a load that was replaced applies nothing: a source that
	// waits for a slow artwork cannot replace the source that replaced it.
	[Fact]
	public async Task ChangingTheSourceWhilePreviousArtworkDownloadsKeepsTheNewSource()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		using var server = new LoopbackHttpServer(new Dictionary<string, (byte[], string)> { ["/art.png"] = (artwork, "image/png") });
		var heldArtworkResponse = server.HoldResponses("/art.png");
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.MetadataArtworkUrl = server.GetUri("/art.png").ToString();
			element.Source = MediaSource.FromResource(toneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilAsync(() => server.HasRequest("HEAD", "/art.png"), "The artwork of the first source was never requested.");
			Assert.Equal(0, recorder.OpenedCount);

			// The second source has no artwork, so it loads at once.
			mediaElement.MetadataArtworkUrl = string.Empty;
			mediaElement.Source = MediaSource.FromResource(shortToneAsset);
			await WaitUntilLoadedAsync(mediaElement, recorder);
			Assert.EndsWith(shortToneAsset, GetPlayer(mediaElement).CurrentMediaItem!.MediaId);

			heldArtworkResponse.SetResult();
			await AssertHelpers.Wait(() => recorder.OpenedCount >= 2, timeout: 3000);

			Assert.EndsWith(shortToneAsset, GetPlayer(mediaElement).CurrentMediaItem!.MediaId);
			Assert.Equal(1, recorder.OpenedCount);
		});
	}

	// CreateMediaItemFromStream sets the manager's StreamDataSourceFactory once the artwork is there, so the source that was replaced while its
	// artwork downloaded must not put its stream behind the data source of the stream that replaced it.
	[Fact]
	public async Task ChangingTheStreamSourceWhilePreviousArtworkDownloadsKeepsTheDataSourceOfTheNewStream()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		var tone = await ReadAssetAsync(toneAsset);
		var shortTone = new MemoryStream(await ReadAssetAsync(shortToneAsset));
		using var server = new LoopbackHttpServer(new Dictionary<string, (byte[], string)> { ["/art.png"] = (artwork, "image/png") });
		var heldArtworkResponse = server.HoldResponses("/art.png");
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.MetadataArtworkUrl = server.GetUri("/art.png").ToString();
			element.Source = MediaSource.FromStream(new MemoryStream(tone));
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			await WaitUntilAsync(() => server.HasRequest("HEAD", "/art.png"), "The artwork of the first source was never requested.");

			mediaElement.MetadataArtworkUrl = string.Empty;
			mediaElement.Source = MediaSource.FromStream(shortTone);
			await WaitUntilLoadedAsync(mediaElement, recorder);

			heldArtworkResponse.SetResult();
			await AssertHelpers.Wait(() => recorder.OpenedCount >= 2, timeout: 3000);

			var factory = GetStreamDataSourceFactory(handler.Manager!);
			Assert.NotNull(factory);
			Assert.NotEqual(IntPtr.Zero, factory.Handle);
			Assert.Same(shortTone, typeof(StreamDataSourceFactory).GetField("stream", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(factory));
			Assert.Equal(shortToneDuration, mediaElement.Duration);
			Assert.Equal(1, recorder.OpenedCount);
		});
	}

	// During the handler's initial mapping MAUI applies at once a Source that a StateChanged handler sets while Opening is raised, inside the
	// update that raised Opening (a Source set later waits for the running update instead): that update was replaced and loads nothing, and a
	// Source set after both cancels the load of the one that replaced it.
	[Fact]
	public async Task SourceSetWhileOpeningIsRaisedDuringTheInitialMappingCannotReplaceALaterSource()
	{
		var artwork = await ReadAssetAsync(artworkAsset);
		using var server = new LoopbackHttpServer(new Dictionary<string, (byte[], string)> { ["/art.png"] = (artwork, "image/png") });
		var heldArtworkResponse = server.HoldResponses("/art.png");
		SetupProbeBuilder();
		var isSourceReplaced = false;
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.MetadataArtworkUrl = server.GetUri("/art.png").ToString();
			element.Source = MediaSource.FromResource(videoAsset);

			// MAUI's WeakEventManager, which raises StateChanged, holds the target of a handler weakly. A handler that captured `element` would be
			// the only reference to its closure, which a collection before the handler connects removes (and the handler with it): this one
			// captures only the test's own state, which the running test keeps, and sets the Source through the sender.
			element.StateChanged += (sender, e) =>
			{
				if (e.NewState is MediaElementState.Opening && !isSourceReplaced)
				{
					isSourceReplaced = true;
					((MediaElement)sender!).Source = MediaSource.FromResource(shortToneAsset);
				}
			};
		});

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilAsync(() => isSourceReplaced && server.HasRequest("HEAD", "/art.png"), "The source set while Opening was raised never requested its artwork.");

			// The latest source has no artwork, so it loads at once.
			mediaElement.MetadataArtworkUrl = string.Empty;
			mediaElement.Source = MediaSource.FromResource(toneAsset);
			await WaitUntilAsync(() => GetPlayer(mediaElement).CurrentMediaItem?.MediaId?.EndsWith(toneAsset, StringComparison.Ordinal) == true, "The latest source was never loaded.");

			heldArtworkResponse.SetResult();
			await AssertHelpers.Wait(() => GetPlayer(mediaElement).CurrentMediaItem?.MediaId?.EndsWith(toneAsset, StringComparison.Ordinal) != true, timeout: 3000);

			Assert.EndsWith(toneAsset, GetPlayer(mediaElement).CurrentMediaItem!.MediaId);
		});
	}

	// An exception of UpdateSource is not a failure of the source (the source mapping lets it propagate), so the manager reports a source that
	// it cannot apply through MediaFailed, with the message of the NotSupportedException, instead of throwing it.
	[Fact]
	public async Task UnsupportedKindOfMediaSourceIsReportedByTheManagerWithoutThrowing()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();
		using var recorder = new MediaElementRecorder(mediaElement);

		await WithHandlerAsync(mediaElement, async handler =>
		{
			mediaElement.Source = new UnsupportedMediaSource();
			Assert.Single(recorder.FailureMessages);

			await handler.Manager!.UpdateSource();

			Assert.Equal(2, recorder.FailureMessages.Count);
			Assert.All(recorder.FailureMessages, failure =>
			{
				Assert.Contains(typeof(UnsupportedMediaSource).FullName!, failure);
				Assert.Contains("is not yet supported", failure);
			});
		});
	}

	[Fact]
	public async Task UnsupportedKindOfMediaSourceRaisesMediaFailed()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();
		using var recorder = new MediaElementRecorder(mediaElement);

		await WithHandlerAsync(mediaElement, handler =>
		{
			mediaElement.Source = new UnsupportedMediaSource();

			var failure = Assert.Single(recorder.FailureMessages);
			Assert.Contains(typeof(UnsupportedMediaSource).FullName!, failure);
			Assert.Contains("is not yet supported", failure);
			Assert.Equal(MediaElementState.Failed, mediaElement.CurrentState);
		});
	}

	// Like a source that cannot be played, a source that the manager cannot apply takes the previous media out of the player, so nothing is
	// reported for it afterwards (its MediaEnded, or a state that overwrites Failed).
	[Fact]
	public async Task SourceThatCannotBeAppliedStopsThePreviousMedia()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element =>
		{
			element.ShouldAutoPlay = true;
			element.Source = MediaSource.FromResource(toneAsset);
		});
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			var player = GetPlayer(mediaElement);
			await WaitUntilAsync(() => player.IsPlaying, "The first source never played.");

			mediaElement.Source = new UnsupportedMediaSource();

			Assert.Single(recorder.FailureMessages);
			Assert.Equal(MediaElementState.Failed, mediaElement.CurrentState);
			Assert.False(player.IsPlaying, $"The previous media still plays after MediaFailed: {player.CurrentMediaItem?.MediaId}");
			Assert.Equal(0, player.MediaItemCount);

			// Had the previous media kept playing, it would have ended within its duration.
			var statesAtTheFailure = recorder.States.Count;
			var isReported = await AssertHelpers.Wait(() => recorder.EndedCount > 0 || mediaElement.CurrentState is not MediaElementState.Failed, timeout: (int)toneDuration.TotalMilliseconds + 1000);
			Assert.False(isReported, $"After MediaFailed: MediaEnded raised {recorder.EndedCount} time(s), states [{string.Join(", ", recorder.States.Skip(statesAtTheFailure))}].");
		});
	}

	// Opening is raised before the source is applied, and the update runs synchronously up to its first await, so the exception of its handler
	// reaches the code that sets Source.
	[Fact]
	public async Task ExceptionOfAnEventHandlerDuringASourceChangePropagatesAndIsNotReportedAsMediaFailed()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();
		using var recorder = new MediaElementRecorder(mediaElement);
		var handlerException = new InvalidOperationException("The StateChanged handler of the app failed.");

		await WithHandlerAsync(mediaElement, handler =>
		{
			mediaElement.StateChanged += (_, e) =>
			{
				if (e.NewState is MediaElementState.Opening)
				{
					throw handlerException;
				}
			};

			var setterException = Record.Exception(() => mediaElement.Source = MediaSource.FromResource(toneAsset));

			Assert.Same(handlerException, ExceptionOfTheHandler(setterException));

			// A failure of the source raises MediaFailed. CurrentState cannot show one here: MAUI applies no change of CurrentState after a StateChanged
			// handler threw (BindableObject queues the changes of a property while it is being set, and the exception never ends that set).
			Assert.Empty(recorder.FailureMessages);
		});
	}

	// MediaOpened is raised after the part of the update whose failures are failures of the source, so an exception of its handler propagates
	// (without artwork the update runs synchronously, so to the code that sets Source).
	[Fact]
	public async Task ExceptionOfAMediaOpenedHandlerPropagatesAndTheSourceStaysApplied()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();
		using var recorder = new MediaElementRecorder(mediaElement);
		var handlerException = new InvalidOperationException("The MediaOpened handler of the app failed.");

		await HostAsync(mediaElement, async _ =>
		{
			var player = GetPlayer(mediaElement);
			mediaElement.MediaOpened += (_, _) => throw handlerException;

			Exception? setterException;
			using (var capture = new AsyncVoidExceptionCapture())
			{
				setterException = Record.Exception(() => mediaElement.Source = MediaSource.FromResource(toneAsset));
				Assert.Null(capture.Exception);
			}

			Assert.Same(handlerException, ExceptionOfTheHandler(setterException));
			Assert.Equal(1, recorder.OpenedCount);
			Assert.EndsWith(toneAsset, player.CurrentMediaItem!.MediaId);
			await WaitForStateAsync(mediaElement, MediaElementState.Paused);
			Assert.Empty(recorder.FailureMessages);
		});
	}

	// ExoPlayer raises its events synchronously while it takes the source (SetMediaItem and Prepare), here StateChanged for Buffering. There, only an
	// exception of the player itself (a Java exception) is a failure of the source, so the exception of an event handler propagates (without
	// artwork the update runs synchronously, so to the code that sets Source).
	[Fact]
	public async Task ExceptionOfABufferingHandlerWhileThePlayerTakesTheSourcePropagatesAndIsNotReportedAsMediaFailed()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();
		using var recorder = new MediaElementRecorder(mediaElement);
		var handlerException = new InvalidOperationException("The StateChanged handler of the app failed.");

		await HostAsync(mediaElement, _ =>
		{
			var player = GetPlayer(mediaElement);
			var isSettingTheSource = false;
			mediaElement.StateChanged += (_, e) =>
			{
				// Only while the source is set: an exception of a handler that ExoPlayer raises from its looper would end the app.
				if (isSettingTheSource && e.NewState is MediaElementState.Buffering)
				{
					throw handlerException;
				}
			};

			Exception? setterException;
			using (var capture = new AsyncVoidExceptionCapture())
			{
				isSettingTheSource = true;
				setterException = Record.Exception(() => mediaElement.Source = MediaSource.FromResource(toneAsset));
				isSettingTheSource = false;
				Assert.Null(capture.Exception);
			}

			Assert.Same(handlerException, ExceptionOfTheHandler(setterException));
			Assert.Empty(recorder.FailureMessages);
			Assert.Equal(1, player.MediaItemCount);
			Assert.EndsWith(toneAsset, player.CurrentMediaItem!.MediaId);
		});
	}

	// None is raised after the part of the update whose failures are failures of the source (once a source without data has emptied the player),
	// so an exception of its handler propagates to the code that sets Source.
	[Fact]
	public async Task ExceptionOfANoneHandlerAfterTheSourceIsAppliedPropagatesAndIsNotReportedAsMediaFailed()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);
		var handlerException = new InvalidOperationException("The StateChanged handler of the app failed.");

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var player = GetPlayer(mediaElement);
			var isSettingTheSource = false;
			mediaElement.StateChanged += (_, e) =>
			{
				if (isSettingTheSource && e.NewState is MediaElementState.None)
				{
					throw handlerException;
				}
			};

			isSettingTheSource = true;
			var setterException = Record.Exception(() => mediaElement.Source = new UriMediaSource());
			isSettingTheSource = false;

			Assert.Same(handlerException, ExceptionOfTheHandler(setterException));
			Assert.Empty(recorder.FailureMessages);
			Assert.Equal(0, player.MediaItemCount);
			Assert.Equal(MediaElementState.None, mediaElement.CurrentState);
		});
	}

	// With artwork the update has awaited its download by the time ExoPlayer takes the source, so the Source that the Buffering handler sets is
	// applied at once (MAUI defers only a Source that is set while the Source is being set).
	[Fact]
	public async Task SourceSetByABufferingHandlerWhileThePlayerTakesTheSourceReplacesIt()
	{
		using var server = new LoopbackHttpServer(new Dictionary<string, (byte[], string)> { ["/art.png"] = (await ReadAssetAsync(artworkAsset), "image/png") });
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.MetadataArtworkUrl = server.GetUri("/art.png").ToString());
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			var player = GetPlayer(mediaElement);
			var hasReplacedTheSource = false;
			mediaElement.StateChanged += (_, e) =>
			{
				if (!hasReplacedTheSource && e.NewState is MediaElementState.Buffering)
				{
					hasReplacedTheSource = true;
					mediaElement.Source = MediaSource.FromResource(shortToneAsset);
				}
			};

			mediaElement.Source = MediaSource.FromResource(toneAsset);

			// The update of the other source applies it and raises its MediaOpened in one turn of the main thread.
			await WaitUntilAsync(() => player.CurrentMediaItem?.MediaId?.EndsWith(shortToneAsset, StringComparison.Ordinal) is true, "The source that the Buffering handler set never reached the player.");
			await WaitForStateAsync(mediaElement, MediaElementState.Paused);
			Assert.True(hasReplacedTheSource);
			Assert.Equal(1, recorder.OpenedCount);
			Assert.Empty(recorder.FailureMessages);
		});
	}

	// MAUI's WeakEventManager, which raises the MediaElement's events, invokes the handlers through reflection: the exception of a handler reaches
	// the caller in a TargetInvocationException.
	static Exception? ExceptionOfTheHandler(Exception? exception) => exception is TargetInvocationException { InnerException: Exception inner } ? inner : exception;

	static MediaSource CreateEmptySource(EmptySourceKind kind) => kind switch
	{
		EmptySourceKind.UriWithoutUri => new UriMediaSource(),
		EmptySourceKind.FileWithoutPath => new FileMediaSource(),
		EmptySourceKind.FileWithWhitespacePath => new FileMediaSource { Path = "   " },
		EmptySourceKind.ResourceWithoutPath => new ResourceMediaSource(),
		EmptySourceKind.StreamWithoutStream => new StreamMediaSource(),
		_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
	};

	// The factory is private state of the manager; it is a Java peer, so its Handle going to zero shows that it was disposed.
	static StreamDataSourceFactory? GetStreamDataSourceFactory(MediaManager manager) =>
		(StreamDataSourceFactory?)GetPrivateField(manager, "currentStreamDataSourceFactory");

	static object? GetPrivateField(MediaManager manager, string name) =>
		typeof(MediaManager).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager);

	sealed class UnsupportedMediaSource : MediaSource;
}