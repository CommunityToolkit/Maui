using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

public class SubtitleExtensionTests : BaseTest
{
	[Fact]
	public void LoadSubtitles_Validate()
	{
		// Arrange
		IMediaElement mediaElement = new MediaElement();

		// Act
		SubtitleExtensions subtitleExtensions = new();

		// Assert
		Assert.NotNull(subtitleExtensions.LoadSubtitles(mediaElement));
	}

	[Fact]
	public async Task LoadSubtitles_InvalidMediaElement_ThrowsArgumentExceptionAsync()
	{
		// Arrange
		IMediaElement mediaElement = null!;

		// Act
		SubtitleExtensions subtitleExtensions = new();

		// Assert
		await Assert.ThrowsAsync<ArgumentNullException>(async () => await subtitleExtensions.LoadSubtitles(mediaElement));
	}

	[Fact]
	public void SetSubtitleExtensions_ValidSubtitleExtension()
	{
		// Arrange
		SubtitleExtensions subtitleExtensions = new();

		// Act & Assert
		Assert.NotNull(subtitleExtensions);
	}

	[Fact]
	public void SetSubtitleSource_ValidString_SetsSubtitleSource()
	{
		// Arrange
		MediaElement mediaElement = new();
		var validUri = "https://example.com/subtitles.vtt";
		mediaElement.SubtitleUrl = validUri;

		// Act & Assert
		Assert.Equal(validUri, mediaElement.SubtitleUrl);
	}

	[Fact]
	public void SetSubtitleSource_EmtpyString_SetsSubtitleSource()
	{
		// Arrange
		MediaElement mediaElement = new();
		var emptyUri = string.Empty;
		mediaElement.SubtitleUrl = emptyUri;

		// Act & Assert
		Assert.Equal(emptyUri, mediaElement.SubtitleUrl);
	}

	[Fact]
	public async Task SetSubtitleSource_InvalidUri_ThrowsArgumentExceptionAsync()
	{
		// Arrange
		var mediaElement = new MediaElement();
		var invalidSubtitleUrl = "invalid://uri";
		mediaElement.SubtitleUrl = invalidSubtitleUrl;
		SubtitleExtensions subtitleExtensions = new();

		// Act & Assert
		await Assert.ThrowsAsync<ArgumentException>(async () => await subtitleExtensions.LoadSubtitles(mediaElement));
	}
}