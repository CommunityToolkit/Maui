using System.Reflection;
using AVFoundation;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CommunityToolkit.Maui.Views;
using ObjCRuntime;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.MediaElementApple)]
[Collection(TestCollections.DisableParallelization)]
public class MediaManagerStreamSourceAppleTests : AppleMediaElementTestBase
{
	const string defaultContentType = "public.mpeg-4";
	const string quickTimeContentType = "com.apple.quicktime-movie";

	static FileStream OpenBundleFile(string name, string extension) => File.OpenRead(BundlePath(name, extension));

	static string GetStreamContentType(Stream stream) =>
		(string)typeof(MediaManager).GetMethod("GetStreamContentType", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [stream])!;

	// An ISO base media header: size, "ftyp", major brand, minor version, padding.
	static byte[] FileTypeBox(string brand, int totalLength = 24)
	{
		var bytes = new byte[totalLength];
		bytes[3] = 0x14;
		"ftyp"u8.CopyTo(bytes.AsSpan(4));
		System.Text.Encoding.ASCII.GetBytes(brand).CopyTo(bytes, 8);
		return bytes;
	}

	[Theory]
	[InlineData("video", "mp4")]
	[InlineData("video_moovend", "mp4")]
	[InlineData("video", "mov")]
	public async Task SeekableStreamPlaysAsMedia(string name, string extension)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);
		using var stream = new AppleRecordingStream(OpenBundleFile(name, extension));

		await HostAsync(mediaElement, async handler =>
		{
			await LoadAsync(mediaElement, handler, MediaSource.FromStream(stream));

			var item = handler.Manager!.NativePlayerItem!;
			Assert.Equal("stream://media", ((AVUrlAsset)item.Asset).Url.AbsoluteString);
			Assert.Equal((64, 48), (mediaElement.MediaWidth, mediaElement.MediaHeight));
			Assert.Equal(VideoDuration.TotalSeconds, item.Duration.Seconds, precision: 1);
			Assert.Equal(1, events.Opened);
			Assert.Empty(events.Failures);
			Assert.Contains(stream.Calls, call => call.StartsWith("Read@", StringComparison.Ordinal));
			Assert.True(stream.CanRead, "The loader must leave the lifetime of the stream to the caller.");
		});
	}

	[Fact]
	public async Task NonSeekableStreamThatThePlayerCannotOpenIsReportedAsFailed()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);
		using var stream = new AppleRecordingStream(OpenBundleFile("video", "mp4"), canSeek: false);

		await HostAsync(mediaElement, async handler =>
		{
			mediaElement.Source = MediaSource.FromStream(stream);
			await Record.ExceptionAsync(handler.Manager!.TakeSourceUpdates);

			// Without a length and range support (FillContentInformation reports 0 and false) AVFoundation cannot open the media. This
			// is a limitation of the Apple implementation (the Android player plays such streams), so what is asserted is that the failure
			// is reported.
			await WaitUntilAsync(() => events.Failures.Count > 0, "MediaFailed was not raised.");
			Assert.Equal(AVPlayerItemStatus.Failed, handler.Manager.NativePlayerItem!.Status);
			Assert.Equal(MediaElementState.Failed, mediaElement.CurrentState);
			Assert.True(stream.CanRead);
		});
	}

	[Fact]
	public async Task StreamSourceWithoutStreamClearsThePlayerItem()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));

			mediaElement.Source = new StreamMediaSource();
			await manager.TakeSourceUpdates();

			Assert.Null(manager.NativePlayer!.CurrentItem);
			Assert.Null(GetStreamResourceLoader(manager));
		});
	}

	[Fact]
	public async Task SwitchingFromAStreamToAnotherSourceDisposesTheStreamLoader()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		using var stream = OpenBundleFile("video", "mp4");

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await LoadAsync(mediaElement, handler, MediaSource.FromStream(stream));
			var loader = GetStreamResourceLoader(manager);
			Assert.NotNull(loader);
			Assert.NotEqual(NativeHandle.Zero, loader.Handle);

			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));

			Assert.Null(GetStreamResourceLoader(manager));
			Assert.Equal(NativeHandle.Zero, loader.Handle);
			Assert.True(stream.CanRead);
		});
	}

	[Fact]
	public async Task SwitchingToAnotherStreamReplacesTheStreamLoader()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		using var first = OpenBundleFile("video", "mp4");
		using var second = OpenBundleFile("video", "mov");

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await LoadAsync(mediaElement, handler, MediaSource.FromStream(first));
			var firstLoader = GetStreamResourceLoader(manager);

			await LoadAsync(mediaElement, handler, MediaSource.FromStream(second));

			var secondLoader = GetStreamResourceLoader(manager);
			Assert.NotNull(secondLoader);
			Assert.NotSame(firstLoader, secondLoader);
			Assert.Equal(NativeHandle.Zero, firstLoader!.Handle);
			Assert.NotEqual(NativeHandle.Zero, secondLoader.Handle);
			Assert.True(first.CanRead);
		});
	}

	[Fact]
	public async Task DisposingTheManagerDisposesTheStreamLoaderButNotTheStream()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		using var stream = OpenBundleFile("video", "mp4");

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await LoadAsync(mediaElement, handler, MediaSource.FromStream(stream));
			var loader = GetStreamResourceLoader(manager)!;

			manager.Dispose();

			Assert.Null(GetStreamResourceLoader(manager));
			Assert.Equal(NativeHandle.Zero, loader.Handle);
			Assert.True(stream.CanRead);
		});
	}

	[Fact]
	public async Task StreamThatFailsToReadPutsTheStreamPositionBack()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		using var stream = new AppleRecordingStream(OpenBundleFile("video", "mp4")) { ThrowOnRead = true };

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			stream.Position = 5;

			// How the read failure is reported is StreamThatFailsToReadIsReportedAsFailedAndReplacesThePreviousMedia below.
			await SetSourceAsync(mediaElement, handler, MediaSource.FromStream(stream));

			// The content type detection reads the 12-byte file type header at position 0, recorded as Read@<position>x<count>.
			Assert.Contains("Read@0x12", stream.Calls);
			Assert.Equal(5, stream.Position);
		});
	}

	[Fact]
	public async Task StreamThatFailsToReadIsReportedAsFailedAndReplacesThePreviousMedia()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);
		using var stream = new AppleRecordingStream(OpenBundleFile("video", "mp4")) { ThrowOnRead = true };

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));
			var previousItem = manager.NativePlayer!.CurrentItem;

			var setterException = Record.Exception(() => mediaElement.Source = MediaSource.FromStream(stream));
			await Record.ExceptionAsync(manager.TakeSourceUpdates);
			await WaitUntilAsync(() => events.Failures.Count > 0, "MediaFailed was not raised.", timeout: 2000);

			Assert.Equal(MediaElementState.Failed, mediaElement.CurrentState);
			Assert.NotSame(previousItem, manager.NativePlayer!.CurrentItem);
			Assert.Null(setterException);
		});
	}

	[Fact]
	public void ContentTypeOfAnMp4StreamIsMpeg4()
	{
		using var stream = new MemoryStream(FileTypeBox("isom"));

		Assert.Equal(defaultContentType, GetStreamContentType(stream));
	}

	[Fact]
	public void ContentTypeOfAQuickTimeStreamIsQuickTimeMovie()
	{
		using var stream = new MemoryStream(FileTypeBox("qt  "));

		Assert.Equal(quickTimeContentType, GetStreamContentType(stream));
	}

	[Fact]
	public void ContentTypeOfARealQuickTimeFileIsQuickTimeMovie()
	{
		using var stream = OpenBundleFile("video", "mov");

		Assert.Equal(quickTimeContentType, GetStreamContentType(stream));
	}

	[Fact]
	public void ContentTypeOfUnrecognizedDataDefaultsToMpeg4()
	{
		var bytes = new byte[24];
		"RIFF....WAVEfmt "u8.CopyTo(bytes);
		using var stream = new MemoryStream(bytes);

		Assert.Equal(defaultContentType, GetStreamContentType(stream));
	}

	[Fact]
	public void ContentTypeOfANonSeekableStreamIsTheDefaultEvenForQuickTime()
	{
		using var stream = new AppleRecordingStream(new MemoryStream(FileTypeBox("qt  ")), canSeek: false);

		Assert.Equal(defaultContentType, GetStreamContentType(stream));
		Assert.Empty(stream.Calls);
	}

	[Theory]
	[InlineData("fXyp")]
	[InlineData("ftXp")]
	[InlineData("ftyX")]
	public void ContentTypeOfDataThatOnlyPartlyMatchesAFileTypeBoxIsMpeg4(string signature)
	{
		// With a QuickTime brand behind it: the data would be reported as QuickTime if the box were recognized.
		var bytes = FileTypeBox("qt  ");
		System.Text.Encoding.ASCII.GetBytes(signature).CopyTo(bytes, 4);
		using var stream = new MemoryStream(bytes);

		Assert.Equal(defaultContentType, GetStreamContentType(stream));
	}

	[Fact]
	public void ContentTypeDetectionAfterAShortReadPutsTheStreamPositionBack()
	{
		using var stream = new AppleRecordingStream(new MemoryStream(FileTypeBox("qt  ", 40))) { MaxBytesPerRead = 4, Position = 9 };

		var contentType = GetStreamContentType(stream);

		// The type of such a stream is ContentTypeIsDetectedWhenTheStreamReturnsShortReads below.
		Assert.Equal(9, stream.Position);
		Assert.Contains(contentType, new[] { defaultContentType, quickTimeContentType });
	}

	[Fact]
	public void ContentTypeIsDetectedForAStreamOneByteLongerThanTheFileTypeHeader()
	{
		using var stream = new MemoryStream(FileTypeBox("qt  ", 13));

		Assert.Equal(quickTimeContentType, GetStreamContentType(stream));
	}

	[Fact]
	public void ContentTypeOfAStreamShorterThanTheFileTypeHeaderIsMpeg4()
	{
		using var stream = new AppleRecordingStream(new MemoryStream(FileTypeBox("qt  ", 12)[..11]));

		Assert.Equal(defaultContentType, GetStreamContentType(stream));
		Assert.Empty(stream.Calls);
	}

	[Fact]
	public void ContentTypeIsDetectedForAStreamThatIsExactlyTheFileTypeHeader()
	{
		using var stream = new MemoryStream(FileTypeBox("qt  ", 12));

		Assert.Equal(quickTimeContentType, GetStreamContentType(stream));
	}

	[Theory]
	[InlineData("qt  ")]
	[InlineData("isom")]
	public void ContentTypeDetectionPutsTheStreamPositionBack(string brand)
	{
		using var stream = new MemoryStream(FileTypeBox(brand, 40)) { Position = 7 };

		GetStreamContentType(stream);

		Assert.Equal(7, stream.Position);
	}

	[Fact]
	public void ContentTypeDetectionPutsTheStreamPositionBackWhenReadingFails()
	{
		using var stream = new AppleRecordingStream(new MemoryStream(FileTypeBox("qt  ", 40))) { ThrowOnRead = true, Position = 7 };

		var exception = Assert.Throws<TargetInvocationException>(() => GetStreamContentType(stream));

		Assert.IsType<IOException>(exception.InnerException);
		Assert.Equal(7, stream.Position);
	}

	[Fact]
	public void ContentTypeIsDetectedWhenTheStreamReturnsShortReads()
	{
		using var stream = new AppleRecordingStream(new MemoryStream(FileTypeBox("qt  ", 40))) { MaxBytesPerRead = 4 };

		Assert.Equal(quickTimeContentType, GetStreamContentType(stream));
	}
}