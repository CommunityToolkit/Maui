using System.Reflection;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests.Tests.MediaElement;

public class MediaElementStateEnumTests
{
	[Theory]
	[InlineData(MediaElementState.None, 0)]
	[InlineData(MediaElementState.Opening, 1)]
	[InlineData(MediaElementState.Buffering, 2)]
	[InlineData(MediaElementState.Playing, 3)]
	[InlineData(MediaElementState.Paused, 4)]
	[InlineData(MediaElementState.Stopped, 5)]
	[InlineData(MediaElementState.Failed, 6)]
	public void MediaElementState_HasExpectedValues(MediaElementState state, int expected)
	{
		Assert.Equal(expected, (int)state);
	}
}

public class AndroidViewTypeEnumTests
{
	[Theory]
	[InlineData(AndroidViewType.SurfaceView, 0)]
	[InlineData(AndroidViewType.TextureView, 1)]
	public void AndroidViewType_HasExpectedValues(AndroidViewType viewType, int expected)
	{
		Assert.Equal(expected, (int)viewType);
	}
}

public class MediaElementDefaultsTests
{
	static readonly Type mediaElementDefaultsType = typeof(CommunityToolkit.Maui.Views.MediaElement).Assembly
		.GetType("CommunityToolkit.Maui.Core.MediaElementDefaults")
		?? throw new InvalidOperationException("MediaElementDefaults type not found");

	[Fact]
	public void MediaElementDefaults_CurrentState_IsNone()
	{
		var field = mediaElementDefaultsType.GetField("CurrentState", BindingFlags.Public | BindingFlags.Static);
		Assert.NotNull(field);
		Assert.Equal(MediaElementState.None, field.GetValue(null));
	}

	[Fact]
	public void MediaElementDefaults_Speed_IsOne()
	{
		var field = mediaElementDefaultsType.GetField("Speed", BindingFlags.Public | BindingFlags.Static);
		Assert.NotNull(field);
		Assert.Equal(1.0, field.GetValue(null));
	}

	[Fact]
	public void MediaElementDefaults_Volume_IsOne()
	{
		var field = mediaElementDefaultsType.GetField("Volume", BindingFlags.Public | BindingFlags.Static);
		Assert.NotNull(field);
		Assert.Equal(1.0, field.GetValue(null));
	}

	[Fact]
	public void MediaElementDefaults_ShouldAutoPlay_IsFalse()
	{
		var field = mediaElementDefaultsType.GetField("ShouldAutoPlay", BindingFlags.Public | BindingFlags.Static);
		Assert.NotNull(field);
		Assert.Equal(false, field.GetValue(null));
	}

	[Fact]
	public void MediaElementDefaults_ShouldLoopPlayback_IsFalse()
	{
		var field = mediaElementDefaultsType.GetField("ShouldLoopPlayback", BindingFlags.Public | BindingFlags.Static);
		Assert.NotNull(field);
		Assert.Equal(false, field.GetValue(null));
	}

	[Fact]
	public void MediaElementDefaults_ShouldKeepScreenOn_IsFalse()
	{
		var field = mediaElementDefaultsType.GetField("ShouldKeepScreenOn", BindingFlags.Public | BindingFlags.Static);
		Assert.NotNull(field);
		Assert.Equal(false, field.GetValue(null));
	}

	[Fact]
	public void MediaElementDefaults_ShouldMute_IsFalse()
	{
		var field = mediaElementDefaultsType.GetField("ShouldMute", BindingFlags.Public | BindingFlags.Static);
		Assert.NotNull(field);
		Assert.Equal(false, field.GetValue(null));
	}

	[Fact]
	public void MediaElementDefaults_ShouldShowPlaybackControls_IsFalse()
	{
		var field = mediaElementDefaultsType.GetField("ShouldShowPlaybackControls", BindingFlags.Public | BindingFlags.Static);
		Assert.NotNull(field);
		Assert.Equal(false, field.GetValue(null));
	}

	[Fact]
	public void MediaElementDefaults_Aspect_IsAspectFit()
	{
		var field = mediaElementDefaultsType.GetField("Aspect", BindingFlags.Public | BindingFlags.Static);
		Assert.NotNull(field);
		Assert.Equal(Aspect.AspectFit, field.GetValue(null));
	}

	[Fact]
	public void MediaElementDefaults_Position_IsZero()
	{
		var property = mediaElementDefaultsType.GetProperty("Position", BindingFlags.Public | BindingFlags.Static);
		Assert.NotNull(property);
		Assert.Equal(TimeSpan.Zero, property.GetValue(null));
	}

	[Fact]
	public void MediaElementDefaults_Duration_IsZero()
	{
		var property = mediaElementDefaultsType.GetProperty("Duration", BindingFlags.Public | BindingFlags.Static);
		Assert.NotNull(property);
		Assert.Equal(TimeSpan.Zero, property.GetValue(null));
	}

	[Fact]
	public void MediaElementDefaults_MetadataTitle_IsEmpty()
	{
		var field = mediaElementDefaultsType.GetField("MetadataTitle", BindingFlags.Public | BindingFlags.Static);
		Assert.NotNull(field);
		Assert.Equal(string.Empty, field.GetValue(null));
	}

	[Fact]
	public void MediaElementDefaults_MetadataArtist_IsEmpty()
	{
		var field = mediaElementDefaultsType.GetField("MetadataArtist", BindingFlags.Public | BindingFlags.Static);
		Assert.NotNull(field);
		Assert.Equal(string.Empty, field.GetValue(null));
	}
}

public class MediaElementOptionsTests
{
	static readonly Type mediaElementOptionsType = typeof(CommunityToolkit.Maui.Views.MediaElement).Assembly
		.GetType("CommunityToolkit.Maui.Core.MediaElementOptions")
		?? throw new InvalidOperationException("MediaElementOptions type not found");

	[Fact]
	public void MediaElementOptions_CanBeCreatedViaInternalConstructor()
	{
		var constructor = mediaElementOptionsType.GetConstructor(
			BindingFlags.NonPublic | BindingFlags.Instance,
			[]);
		Assert.NotNull(constructor);

		var options = constructor.Invoke([]);
		Assert.NotNull(options);
	}

	[Fact]
	public void MediaElementOptions_SetDefaultAndroidViewType()
	{
		var constructor = mediaElementOptionsType.GetConstructor(
			BindingFlags.NonPublic | BindingFlags.Instance,
			[]);
		Assert.NotNull(constructor);

		var options = constructor.Invoke([]);
		var setMethod = mediaElementOptionsType.GetMethod("SetDefaultAndroidViewType", BindingFlags.Public | BindingFlags.Instance);
		Assert.NotNull(setMethod);

		setMethod.Invoke(options, [AndroidViewType.TextureView]);

		var defaultViewTypeProperty = mediaElementOptionsType.GetProperty("DefaultAndroidViewType", BindingFlags.NonPublic | BindingFlags.Static);
		Assert.NotNull(defaultViewTypeProperty);
		Assert.Equal(AndroidViewType.TextureView, defaultViewTypeProperty.GetValue(null));
	}

	[Fact]
	public void MediaElementOptions_SetIsAndroidForegroundServiceEnabled()
	{
		var constructor = mediaElementOptionsType.GetConstructor(
			BindingFlags.NonPublic | BindingFlags.Instance,
			[]);
		Assert.NotNull(constructor);

		var options = constructor.Invoke([]);
		var setMethod = mediaElementOptionsType.GetMethod("SetIsAndroidForegroundServiceEnabled", BindingFlags.Public | BindingFlags.Instance);
		Assert.NotNull(setMethod);

		setMethod.Invoke(options, [true]);

		var isEnabledProperty = mediaElementOptionsType.GetProperty("IsAndroidForegroundServiceEnabled", BindingFlags.NonPublic | BindingFlags.Static);
		Assert.NotNull(isEnabledProperty);
		Assert.Equal(true, isEnabledProperty.GetValue(null));
	}
}

public class MediaSourceTests
{
	[Fact]
	public void MediaSource_FromUri()
	{
		var source = MediaSource.FromUri("https://example.com/video.mp4");

		Assert.IsType<UriMediaSource>(source);
		Assert.Equal(new Uri("https://example.com/video.mp4"), ((UriMediaSource)source).Uri);
	}

	[Fact]
	public void MediaSource_FromFile()
	{
		var source = MediaSource.FromFile("/path/to/video.mp4");

		Assert.IsType<FileMediaSource>(source);
		Assert.Equal("/path/to/video.mp4", ((FileMediaSource)source).Path);
	}

	[Fact]
	public void MediaSource_FromResource()
	{
		var source = MediaSource.FromResource("video.mp4");

		Assert.IsType<ResourceMediaSource>(source);
	}

	[Fact]
	public void MediaSource_FromStream()
	{
		using var stream = new MemoryStream([1, 2, 3]);
		var source = MediaSource.FromStream(stream);

		Assert.IsType<StreamMediaSource>(source);
		Assert.Equal(stream, source.Stream);
	}

	[Fact]
	public void MediaSource_ImplicitFromString_Uri()
	{
		MediaSource source = "https://example.com/video.mp4";

		Assert.IsType<UriMediaSource>(source);
	}

	[Fact]
	public void MediaSource_ImplicitFromString_File()
	{
		MediaSource source = "/local/path/video.mp4";

		Assert.IsType<FileMediaSource>(source);
	}

	[Fact]
	public void MediaSource_ImplicitFromUri()
	{
		MediaSource source = new Uri("https://example.com/video.mp4");

		Assert.IsType<UriMediaSource>(source);
	}

	[Fact]
	public void MediaSource_FromUri_WithHttpHeaders()
	{
		var headers = new Dictionary<string, string>
		{
			["Authorization"] = "Bearer token123",
		};

		var source = MediaSource.FromUri(new Uri("https://example.com/video.mp4"), headers);

		Assert.IsType<UriMediaSource>(source);
		var uriSource = (UriMediaSource)source;
		Assert.Equal("Bearer token123", uriSource.HttpHeaders["Authorization"]);
	}

	[Fact]
	public void MediaSource_FromUri_RelativeUri_Throws()
	{
		var thrown = false;

		try
		{
			MediaSource.FromUri(new Uri("relative/path", UriKind.Relative));
		}
		catch (ArgumentException)
		{
			thrown = true;
		}

		Assert.True(thrown);
	}
}

public class FileMediaSourceTests
{
	[Fact]
	public void FileMediaSource_CanSetPath()
	{
		var source = new FileMediaSource
		{
			Path = "/videos/test.mp4"
		};

		Assert.Equal("/videos/test.mp4", source.Path);
	}

	[Fact]
	public void FileMediaSource_ImplicitFromString()
	{
		FileMediaSource source = "/videos/test.mp4";

		Assert.Equal("/videos/test.mp4", source.Path);
	}

	[Fact]
	public void FileMediaSource_ImplicitToString()
	{
		var source = new FileMediaSource { Path = "/videos/test.mp4" };
		string? path = source;

		Assert.Equal("/videos/test.mp4", path);
	}

	[Fact]
	public void FileMediaSource_ToString_ContainsPath()
	{
		var source = new FileMediaSource { Path = "/videos/test.mp4" };

		Assert.Contains("/videos/test.mp4", source.ToString());
	}
}

public class ResourceMediaSourceTests
{
	[Fact]
	public void ResourceMediaSource_CanSetPath()
	{
		var source = new ResourceMediaSource
		{
			Path = "embedded_video.mp4"
		};

		Assert.Equal("embedded_video.mp4", source.Path);
	}
}

public class StreamMediaSourceTests
{
	[Fact]
	public void StreamMediaSource_CanSetStream()
	{
		var stream = new MemoryStream([1, 2, 3]);
		var source = new StreamMediaSource
		{
			Stream = stream
		};

		Assert.Equal(stream, source.Stream);
	}

	[Fact]
	public void StreamMediaSource_ImplicitFromStream()
	{
		var stream = new MemoryStream([4, 5, 6]);
		StreamMediaSource? source = stream;

		Assert.NotNull(source);
		Assert.Equal(stream, source.Stream);
	}

	[Fact]
	public void StreamMediaSource_ImplicitToStream()
	{
		var stream = new MemoryStream([7, 8, 9]);
		var source = new StreamMediaSource { Stream = stream };
		Stream? result = source;

		Assert.Equal(stream, result);
	}
}

public class UriMediaSourceTests
{
	[Fact]
	public void UriMediaSource_CanSetUri()
	{
		var source = new UriMediaSource
		{
			Uri = new Uri("https://example.com/video.mp4")
		};

		Assert.Equal(new Uri("https://example.com/video.mp4"), source.Uri);
	}

	[Fact]
	public void UriMediaSource_HttpHeaders()
	{
		var source = new UriMediaSource
		{
			Uri = new Uri("https://example.com/video.mp4")
		};

		source.HttpHeaders.Add("Authorization", "Bearer token123");

		Assert.Single(source.HttpHeaders);
		Assert.Equal("Bearer token123", source.HttpHeaders["Authorization"]);
	}

	[Fact]
	public void UriMediaSource_ImplicitFromString()
	{
		UriMediaSource source = "https://example.com/video.mp4";

		Assert.Equal(new Uri("https://example.com/video.mp4"), source.Uri);
	}

	[Fact]
	public void UriMediaSource_ImplicitToString()
	{
		var source = new UriMediaSource { Uri = new Uri("https://example.com/video.mp4") };
		string? uriString = source;

		Assert.Equal("https://example.com/video.mp4", uriString);
	}
}

public class MediaSourceConverterTests
{
	[Fact]
	public void MediaSourceConverter_ConvertsUriString()
	{
		var converter = new MediaSourceConverter();

		var result = converter.ConvertFromInvariantString("https://example.com/video.mp4");

		Assert.IsType<UriMediaSource>(result);
	}

	[Fact]
	public void MediaSourceConverter_ConvertsFileString()
	{
		var converter = new MediaSourceConverter();

		var result = converter.ConvertFromInvariantString("filesystem:///path/to/video.mp4");

		Assert.IsType<FileMediaSource>(result);
	}

	[Fact]
	public void MediaSourceConverter_ConvertsEmbeddedString()
	{
		var converter = new MediaSourceConverter();

		var result = converter.ConvertFromInvariantString("embed://video.mp4");

		Assert.IsType<ResourceMediaSource>(result);
	}

	[Fact]
	public void MediaSourceConverter_EmptyString_ReturnsNull()
	{
		var converter = new MediaSourceConverter();

		var result = converter.ConvertFromInvariantString(string.Empty);

		Assert.Null(result);
	}

	[Fact]
	public void MediaSourceConverter_CanConvertFrom_String()
	{
		var converter = new MediaSourceConverter();

		Assert.True(converter.CanConvertFrom(typeof(string)));
	}

	[Fact]
	public void MediaSourceConverter_CanConvertTo_String()
	{
		var converter = new MediaSourceConverter();

		Assert.True(converter.CanConvertTo(typeof(string)));
	}
}

public class FileMediaSourceConverterTests
{
	[Fact]
	public void FileMediaSourceConverter_EmptyString_ReturnsFileMediaSource()
	{
		var converter = new FileMediaSourceConverter();

		var result = converter.ConvertFrom(string.Empty);

		Assert.IsType<FileMediaSource>(result);
	}

	[Fact]
	public void FileMediaSourceConverter_NonEmptyString_Throws()
	{
		var converter = new FileMediaSourceConverter();
		var thrown = false;

		try
		{
			converter.ConvertFrom("/path/to/video.mp4");
		}
		catch (InvalidOperationException)
		{
			thrown = true;
		}

		Assert.True(thrown);
	}
}

public partial class StreamExtensionsTests
{
	static readonly Type streamExtensionsType = typeof(CommunityToolkit.Maui.Views.MediaElement).Assembly
		.GetType("CommunityToolkit.Maui.Core.Extensions.StreamExtensions")
		?? throw new InvalidOperationException("StreamExtensions type not found");

	static readonly MethodInfo getMimeTypeMethod = streamExtensionsType
		.GetMethod("GetMimeType", BindingFlags.NonPublic | BindingFlags.Static)
		?? throw new InvalidOperationException("GetMimeType method not found");

	static string InvokeGetMimeType(Stream stream)
	{
		var result = getMimeTypeMethod.Invoke(null, [stream]);
		Assert.NotNull(result);
		return (string)result;
	}

	// GetMimeType is a media-format detector (audio/video containers), not a general image/pdf sniffer.
	[Theory]
	[InlineData(new byte[] { 0x1A, 0x45, 0xDF, 0xA3, 0x00, 0x00, 0x00, 0x00 }, "video/webm")]
	[InlineData(new byte[] { 0x4F, 0x67, 0x67, 0x53 }, "application/ogg")]
	[InlineData(new byte[] { 0x66, 0x4C, 0x61, 0x43 }, "audio/flac")]
	[InlineData(new byte[] { 0x49, 0x44, 0x33, 0x00 }, "audio/mpeg")]
	public void GetMimeType_DetectsCorrectType(byte[] magicBytes, string expectedMime)
	{
		using var stream = new MemoryStream(magicBytes);

		var mimeType = InvokeGetMimeType(stream);

		Assert.Equal(expectedMime, mimeType);
	}

	[Fact]
	public void GetMimeType_UnknownBytes_ReturnsOctetStream()
	{
		using var stream = new MemoryStream([0x00, 0x01, 0x02, 0x03]);

		var mimeType = InvokeGetMimeType(stream);

		Assert.Equal("application/octet-stream", mimeType);
	}

	[Fact]
	public void GetMimeType_Mp4Container_ReturnsVideoMp4()
	{
		// MP4: bytes 4-7 == "ftyp"
		var mp4Header = new byte[] { 0x00, 0x00, 0x00, 0x18, (byte)'f', (byte)'t', (byte)'y', (byte)'p', (byte)'m', (byte)'p', (byte)'4', (byte)'2' };
		using var stream = new MemoryStream(mp4Header);

		var mimeType = InvokeGetMimeType(stream);

		Assert.Equal("video/mp4", mimeType);
	}

	[Fact]
	public void GetMimeType_WebMContainer_ReturnsVideoWebm()
	{
		var webmHeader = new byte[] { 0x1A, 0x45, 0xDF, 0xA3, 0x00, 0x00, 0x00, 0x00 };
		using var stream = new MemoryStream(webmHeader);

		var mimeType = InvokeGetMimeType(stream);

		Assert.Equal("video/webm", mimeType);
	}

	[Fact]
	public void GetMimeType_NonSeekableStream_ReturnsOctetStream()
	{
		using var stream = new NonSeekableStream();

		var mimeType = InvokeGetMimeType(stream);

		Assert.Equal("application/octet-stream", mimeType);
	}

	[Fact]
	public void GetMimeType_RestoresPosition()
	{
		using var stream = new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x00]);
		stream.Position = 3;

		InvokeGetMimeType(stream);

		Assert.Equal(3, stream.Position);
	}

	sealed partial class NonSeekableStream : Stream
	{
		public override bool CanRead => true;

		public override bool CanSeek => false;

		public override bool CanWrite => false;

		public override long Length => throw new NotSupportedException();

		public override long Position
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		public override void Flush()
		{
		}

		public override int Read(byte[] buffer, int offset, int count) => 0;

		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

		public override void SetLength(long value) => throw new NotSupportedException();

		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
	}
}

public class MediaEventArgsTests
{
	[Fact]
	public void MediaFailedEventArgs_CarriesMessage()
	{
		var args = new MediaFailedEventArgs("Playback error");

		Assert.Equal("Playback error", args.ErrorMessage);
	}

	[Fact]
	public void MediaPositionChangedEventArgs_CarriesPosition()
	{
		var args = new MediaPositionChangedEventArgs(TimeSpan.FromSeconds(30));

		Assert.Equal(TimeSpan.FromSeconds(30), args.Position);
	}

	[Fact]
	public void MediaStateChangedEventArgs_CarriesStates()
	{
		var args = new MediaStateChangedEventArgs(MediaElementState.Buffering, MediaElementState.Playing);

		Assert.Equal(MediaElementState.Buffering, args.PreviousState);
		Assert.Equal(MediaElementState.Playing, args.NewState);
	}
}

public class MediaElementBindablePropertyTests
{
	[Fact]
	public void MediaElement_DefaultState()
	{
		var mediaElement = new CommunityToolkit.Maui.Views.MediaElement();

		Assert.Equal(MediaElementState.None, mediaElement.CurrentState);
	}

	[Fact]
	public void MediaElement_DefaultSpeed()
	{
		var mediaElement = new CommunityToolkit.Maui.Views.MediaElement();

		Assert.Equal(1.0, mediaElement.Speed);
	}

	[Fact]
	public void MediaElement_DefaultVolume()
	{
		var mediaElement = new CommunityToolkit.Maui.Views.MediaElement();

		Assert.Equal(1.0, mediaElement.Volume);
	}

	[Fact]
	public void MediaElement_DefaultShouldAutoPlay()
	{
		var mediaElement = new CommunityToolkit.Maui.Views.MediaElement();

		Assert.False(mediaElement.ShouldAutoPlay);
	}

	[Fact]
	public void MediaElement_DefaultShouldLoopPlayback()
	{
		var mediaElement = new CommunityToolkit.Maui.Views.MediaElement();

		Assert.False(mediaElement.ShouldLoopPlayback);
	}

	[Fact]
	public void MediaElement_DefaultShouldMute()
	{
		var mediaElement = new CommunityToolkit.Maui.Views.MediaElement();

		Assert.False(mediaElement.ShouldMute);
	}

	[Fact]
	public void MediaElement_DefaultShouldShowPlaybackControls()
	{
		var mediaElement = new CommunityToolkit.Maui.Views.MediaElement();

		Assert.False(mediaElement.ShouldShowPlaybackControls);
	}

	[Fact]
	public void MediaElement_DefaultAspect()
	{
		var mediaElement = new CommunityToolkit.Maui.Views.MediaElement();

		Assert.Equal(Aspect.AspectFit, mediaElement.Aspect);
	}

	[Fact]
	public void MediaElement_DefaultPosition()
	{
		var mediaElement = new CommunityToolkit.Maui.Views.MediaElement();

		Assert.Equal(TimeSpan.Zero, mediaElement.Position);
	}

	[Fact]
	public void MediaElement_DefaultDuration()
	{
		var mediaElement = new CommunityToolkit.Maui.Views.MediaElement();

		Assert.Equal(TimeSpan.Zero, mediaElement.Duration);
	}

	[Fact]
	public void MediaElement_CanSetSpeed()
	{
		var mediaElement = new CommunityToolkit.Maui.Views.MediaElement
		{
			Speed = 2.0
		};

		Assert.Equal(2.0, mediaElement.Speed);
	}

	[Fact]
	public void MediaElement_CanSetVolume()
	{
		var mediaElement = new CommunityToolkit.Maui.Views.MediaElement
		{
			Volume = 0.5
		};

		Assert.Equal(0.5, mediaElement.Volume);
	}

	[Fact]
	public void MediaElement_CanSetSource()
	{
		var mediaElement = new CommunityToolkit.Maui.Views.MediaElement
		{
			Source = MediaSource.FromUri("https://example.com/video.mp4")
		};

		Assert.NotNull(mediaElement.Source);
		Assert.IsType<UriMediaSource>(mediaElement.Source);
	}

	[Fact]
	public void MediaElement_CanSetShouldAutoPlay()
	{
		var mediaElement = new CommunityToolkit.Maui.Views.MediaElement
		{
			ShouldAutoPlay = true
		};

		Assert.True(mediaElement.ShouldAutoPlay);
	}

	[Fact]
	public void MediaElement_CanSetShouldLoopPlayback()
	{
		var mediaElement = new CommunityToolkit.Maui.Views.MediaElement
		{
			ShouldLoopPlayback = true
		};

		Assert.True(mediaElement.ShouldLoopPlayback);
	}

	[Fact]
	public void MediaElement_CanSetShouldMute()
	{
		var mediaElement = new CommunityToolkit.Maui.Views.MediaElement
		{
			ShouldMute = true
		};

		Assert.True(mediaElement.ShouldMute);
	}

	[Fact]
	public void MediaElement_CanSetMetadataTitle()
	{
		var mediaElement = new CommunityToolkit.Maui.Views.MediaElement
		{
			MetadataTitle = "Test Title"
		};

		Assert.Equal("Test Title", mediaElement.MetadataTitle);
	}

	[Fact]
	public void MediaElement_CanSetMetadataArtist()
	{
		var mediaElement = new CommunityToolkit.Maui.Views.MediaElement
		{
			MetadataArtist = "Test Artist"
		};

		Assert.Equal("Test Artist", mediaElement.MetadataArtist);
	}
}
