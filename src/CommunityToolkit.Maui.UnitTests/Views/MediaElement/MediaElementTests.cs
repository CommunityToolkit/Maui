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