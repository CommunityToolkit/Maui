#pragma warning disable CS0618 // These tests pin the obsolete MetadataArtworkUrl, which keeps working
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class MediaElementTests : BaseViewTest
{
	public MediaElementTests()
	{
		Assert.IsType<IMediaElement>(new MediaElement(), exactMatch: false);
	}

	[Fact]
	public void VerifyDefaults()
	{
		// Arrange
		MediaElement mediaElement = new();

		// Act 

		// Assert
		Assert.Equal(MediaElementDefaults.MediaHeight, mediaElement.MediaHeight);
		Assert.Equal(MediaElementDefaults.Aspect, mediaElement.Aspect);
		Assert.Equal(MediaElementDefaults.CurrentState, mediaElement.CurrentState);
		Assert.Equal(MediaElementDefaults.Duration, mediaElement.Duration);
		Assert.Equal(MediaElementDefaults.MediaWidth, mediaElement.MediaWidth);
		Assert.Equal(MediaElementDefaults.MetadataArtist, mediaElement.MetadataArtist);
		Assert.Equal(MediaElementDefaults.MetadataArtworkUrl, mediaElement.MetadataArtworkUrl);
		Assert.Equal(MediaElementDefaults.MetadataArtworkSource, mediaElement.MetadataArtworkSource);
		Assert.Null(mediaElement.MetadataArtworkSource);
		Assert.Equal(MediaElementDefaults.Position, mediaElement.Position);
		Assert.Equal(MediaElementDefaults.ShouldAutoPlay, mediaElement.ShouldAutoPlay);
		Assert.Equal(MediaElementDefaults.ShouldKeepScreenOn, mediaElement.ShouldKeepScreenOn);
		Assert.Equal(MediaElementDefaults.ShouldLoopPlayback, mediaElement.ShouldLoopPlayback);
		Assert.Equal(MediaElementDefaults.ShouldMute, mediaElement.ShouldMute);
		Assert.Equal(MediaElementDefaults.ShouldShowPlaybackControls, mediaElement.ShouldShowPlaybackControls);
		Assert.Equal(MediaElementDefaults.Speed, mediaElement.Speed);
		Assert.Equal(MediaElementDefaults.Volume, mediaElement.Volume);
		Assert.Equal(MediaElementDefaults.MetadataTitle, mediaElement.MetadataTitle);
	}

	[Fact]
	public void PosterIsNotStringEmptyOrNull()
	{
		MediaElement mediaElement = new();
		mediaElement.MetadataArtworkUrl = "https://www.example.com/image.jpg";
		Assert.False(string.IsNullOrEmpty(mediaElement.MetadataArtworkUrl));
	}

	[Fact]
	public void PosterIsStringEmptyDoesNotThrow()
	{
		MediaElement mediaElement = new();
		mediaElement.MetadataArtworkUrl = string.Empty;
		Assert.True(string.IsNullOrEmpty(mediaElement.MetadataArtworkUrl));
		Assert.True(mediaElement.MetadataArtworkUrl == string.Empty);
	}

	[Theory]
	[InlineData("uri")]
	[InlineData("file")]
	[InlineData("resource")]
	public void MetadataArtworkSourceTakesEachMediaSourceType(string kind)
	{
		MediaElement mediaElement = new();
		MediaSource artworkSource = kind switch
		{
			"uri" => MediaSource.FromUri("https://www.example.com/image.jpg"),
			"file" => MediaSource.FromFile(Path.Combine(Path.GetTempPath(), "image.jpg")),
			_ => MediaSource.FromResource("image.jpg")
		};

		mediaElement.MetadataArtworkSource = artworkSource;

		mediaElement.MetadataArtworkSource.Should().BeSameAs(artworkSource);
	}

	[Theory]
	[InlineData("en-US", "https://www.example.com/image.jpg", typeof(UriMediaSource))]
	[InlineData("tr-TR", "https://www.example.com/image.jpg", typeof(UriMediaSource))]
	[InlineData("az-Latn-AZ", "https://www.example.com/image.jpg", typeof(UriMediaSource))]
	[InlineData("en-US", "FILE", typeof(FileMediaSource))]
	[InlineData("tr-TR", "FILE", typeof(FileMediaSource))]
	[InlineData("az-Latn-AZ", "FILE", typeof(FileMediaSource))]
	[InlineData("en-US", "image.png", typeof(FileMediaSource))]
	[InlineData("tr-TR", "image.png", typeof(FileMediaSource))]
	[InlineData("az-Latn-AZ", "image.png", typeof(FileMediaSource))]
	public void MetadataArtworkSourceConvertsAStringLikeSource(string cultureName, string text, Type expectedSourceType)
	{
		SetCurrentCulture(cultureName);
		var rootedPath = Path.Combine(Path.GetTempPath(), "image.png");
		var textToConvert = text is "FILE" ? new Uri(rootedPath).AbsoluteUri : text;
		MediaElement mediaElement = new();

		mediaElement.MetadataArtworkSource = textToConvert;

		mediaElement.MetadataArtworkSource.Should().BeOfType(expectedSourceType);
	}

	[Fact]
	public void MetadataArtworkSourceAndMetadataArtworkUrlAreIndependent()
	{
		MediaElement artworkSourceSet = new() { MetadataArtworkSource = MediaSource.FromUri("https://www.example.com/image.jpg") };
		MediaElement artworkUrlSet = new() { MetadataArtworkUrl = "https://www.example.com/image.jpg" };

		artworkSourceSet.MetadataArtworkUrl.Should().Be(MediaElementDefaults.MetadataArtworkUrl);
		artworkUrlSet.MetadataArtworkSource.Should().BeNull();
	}

	[Fact]
	public void MetadataArtworkSourceGetsTheBindingContext()
	{
		object context = new();
		MediaElement mediaElement = new();
		FileMediaSource artworkSourceSetBeforeTheContext = new();
		FileMediaSource artworkSourceSetAfterTheContext = new();

		mediaElement.MetadataArtworkSource = artworkSourceSetBeforeTheContext;
		mediaElement.BindingContext = context;
		artworkSourceSetBeforeTheContext.BindingContext.Should().Be(context);

		mediaElement.MetadataArtworkSource = artworkSourceSetAfterTheContext;
		artworkSourceSetAfterTheContext.BindingContext.Should().Be(context);
	}

	[Fact]
	public void MetadataArtworkSourceHasTheMediaSourceConverter()
	{
		var property = typeof(MediaElement).GetProperty(nameof(MediaElement.MetadataArtworkSource));

		var converter = property?.GetCustomAttribute<TypeConverterAttribute>();

		converter.Should().NotBeNull();
		converter?.ConverterTypeName.Should().Be(typeof(MediaSourceConverter).AssemblyQualifiedName);
	}

	[Fact]
	public void MetadataArtworkUrlIsObsoleteAndPointsToMetadataArtworkSource()
	{
		MemberInfo?[] obsoleteMembers =
		[
			typeof(MediaElement).GetProperty(nameof(MediaElement.MetadataArtworkUrl)),
			typeof(MediaElement).GetField(nameof(MediaElement.MetadataArtworkUrlProperty)),
			typeof(IMediaElement).GetProperty(nameof(IMediaElement.MetadataArtworkUrl))
		];

		foreach (var member in obsoleteMembers)
		{
			var obsolete = member?.GetCustomAttribute<ObsoleteAttribute>();

			obsolete.Should().NotBeNull();
			obsolete?.Message.Should().Contain(nameof(MediaElement.MetadataArtworkSource));
			obsolete?.IsError.Should().BeFalse();
		}
	}

	[Fact]
	public void IMediaElementExposesMetadataArtworkSource()
	{
		var artworkSource = MediaSource.FromUri("https://www.example.com/image.jpg");
		MediaElement mediaElement = new();
		IMediaElement mediaElementInterface = mediaElement;

		mediaElementInterface.MetadataArtworkSource = artworkSource;

		mediaElementInterface.MetadataArtworkSource.Should().BeSameAs(artworkSource);
		mediaElement.MetadataArtworkSource.Should().BeSameAs(artworkSource);
	}

	[Fact]
	public void SettingMetadataArtworkSourceRequestsAnArtworkUpdate()
	{
		MediaElement mediaElement = new();
		var requests = CountArtworkUpdateRequests(mediaElement);

		mediaElement.MetadataArtworkSource = MediaSource.FromUri("https://www.example.com/image.jpg");
		mediaElement.MetadataArtworkSource = MediaSource.FromResource("image.jpg");
		mediaElement.MetadataArtworkSource = null;

		requests().Should().Be(3);
	}

	[Theory]
	[InlineData("uri")]
	[InlineData("headers")]
	[InlineData("file")]
	[InlineData("resource")]
	[InlineData("stream")]
	public void ChangingThePropertyOfTheArtworkSourceRequestsAnArtworkUpdate(string kind)
	{
		using var stream = new MemoryStream();
		MediaElement mediaElement = new();
		MediaSource artworkSource = kind switch
		{
			"uri" or "headers" => MediaSource.FromUri("https://www.example.com/image.jpg"),
			"file" => MediaSource.FromFile(Path.Combine(Path.GetTempPath(), "image.jpg")),
			"resource" => MediaSource.FromResource("image.jpg"),
			_ => MediaSource.FromStream(new MemoryStream())
		};
		mediaElement.MetadataArtworkSource = artworkSource;
		var requests = CountArtworkUpdateRequests(mediaElement);

		switch (artworkSource)
		{
			case UriMediaSource uriSource when kind is "headers":
				uriSource.HttpHeaders["Authorization"] = "Bearer token";
				break;
			case UriMediaSource uriSource:
				uriSource.Uri = new Uri("https://www.example.com/other.jpg");
				break;
			case FileMediaSource fileSource:
				fileSource.Path = Path.Combine(Path.GetTempPath(), "other.jpg");
				break;
			case ResourceMediaSource resourceSource:
				resourceSource.Path = "other.jpg";
				break;
			case StreamMediaSource streamSource:
				streamSource.Stream = stream;
				break;
		}

		requests().Should().Be(1);
	}

	[Fact]
	public void AReplacedArtworkSourceRequestsNoArtworkUpdate()
	{
		MediaElement mediaElement = new();
		var replaced = (UriMediaSource)MediaSource.FromUri("https://www.example.com/image.jpg");
		mediaElement.MetadataArtworkSource = replaced;
		mediaElement.MetadataArtworkSource = MediaSource.FromUri("https://www.example.com/other.jpg");
		var requests = CountArtworkUpdateRequests(mediaElement);

		replaced.Uri = new Uri("https://www.example.com/changed.jpg");

		requests().Should().Be(0);
	}

	[Fact]
	public void SettingMetadataArtworkUrlRequestsNoArtworkUpdate()
	{
		MediaElement mediaElement = new();
		var requests = CountArtworkUpdateRequests(mediaElement);

		mediaElement.MetadataArtworkUrl = "https://www.example.com/image.jpg";

		requests().Should().Be(0);
	}

	[Fact]
	public void ArtworkUpdateRequestsGoToTheMetadataArtworkCommandOfTheHandler()
	{
		Core.Handlers.MediaElementHandler.CommandMapper.GetCommand(nameof(MediaElement.MetadataArtworkUpdateRequested)).Should().NotBeNull();
	}

	static Func<int> CountArtworkUpdateRequests(MediaElement mediaElement)
	{
		var count = 0;
		mediaElement.MetadataArtworkUpdateRequested += (_, _) => count++;
		return () => count;
	}

	static void SetCurrentCulture(string cultureName)
	{
		var culture = CultureInfo.GetCultureInfo(cultureName);
		CultureInfo.CurrentCulture = culture;
		CultureInfo.CurrentUICulture = culture;
	}

	[Fact]
	public void BindingContextPropagation()
	{
		object context = new();
		MediaElement mediaElement = new();
		FileMediaSource mediaSource = new();

		mediaElement.Source = mediaSource;

		mediaElement.BindingContext = context;

		mediaSource.BindingContext.Should().Be(context);
	}

	[Fact]
	public void CorrectDimensionsForVideoTest()
	{
		MediaElement mediaElement = new();
		var mediaSource = MediaSource.FromUri("https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4");

		mediaElement.MediaOpened += (_, _) =>
		{
			mediaElement.MediaWidth.Should().Be(1280);
			mediaElement.MediaHeight.Should().Be(720);
		};

		mediaElement.Source = mediaSource;
	}

	[Fact]
	public void CorrectDimensionsForNullTest()
	{
		object context = new();
		MediaElement mediaElement = new();
		var mediaSource = MediaSource.FromUri("https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4");

		mediaElement.MediaOpened += (_, _) =>
		{
			// We set it to an actual media source first, when the is opened, set the source to null
			if (mediaSource is not null)
			{
				mediaElement.MediaWidth.Should().Be(1280);
				mediaElement.MediaHeight.Should().Be(720);

				mediaElement.Source = null;
				return;
			}

			// When the source is null, the dimensions should be 0
			if (mediaElement.Source is null)
			{
				mediaElement.MediaWidth.Should().Be(0);
				mediaElement.MediaHeight.Should().Be(0);
			}
		};

		// Set the first (actual) media source, which will trigger the above event
		mediaElement.Source = mediaSource;
	}

	[Fact]
	public void MediaElementShouldBeAssignedToIMediaElement()
	{
		new MediaElement().Should().BeAssignableTo<IMediaElement>();
	}

	[Fact]
	public void MediaElementVolumeShouldNotBeMoreThan1()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() =>
		{
			MediaElement mediaElement = new()
			{
				Volume = 1 + Math.Pow(10, -15)
			};
		});
	}

	[Fact]
	public void MediaElementVolumeShouldNotBeLessThan0()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() =>
		{
			MediaElement mediaElement = new()
			{
				Volume = -double.Epsilon
			};
		});
	}
}