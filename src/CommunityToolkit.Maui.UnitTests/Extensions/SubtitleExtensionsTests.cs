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
		CancellationToken token = new();

		// Act
		SubtitleExtensions subtitleExtensions = new();

		// Assert
		Assert.NotNull(subtitleExtensions.LoadSubtitles(mediaElement, token));
	}

	[Fact]
	public async Task LoadSubtitles_InvalidSubtitleExtensions_ThrowsNullReferenceExceptionAsync()
	{
		// Arrange
		IMediaElement mediaElement = new MediaElement();
		CancellationToken token = new();

		// Act
		SubtitleExtensions subtitleExtensions = null!;

		// Assert
		await Assert.ThrowsAsync<NullReferenceException>(async () => await subtitleExtensions.LoadSubtitles(mediaElement, token));
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
		CancellationToken token = new();

		// Act & Assert
		await Assert.ThrowsAsync<ArgumentException>(async () => await subtitleExtensions.LoadSubtitles(mediaElement, token));
	}
}