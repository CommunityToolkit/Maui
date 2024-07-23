using CommunityToolkit.Maui.Core;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;
public class SubtitleParserTests
{
	[Fact]
	public async Task ParseSubtitles_WithNullUrl_ThrowsArgumentNullException()
	{
		// Arrange
		string? url = null;
		
		// Act & Assert
		await Assert.ThrowsAsync<ArgumentNullException>(async () => await SubtitleParser.Content(url));
	}

	[Fact]
	public async Task ParseSubtitles_WithEmptyUrl_ThrowsArgumentException()
	{
		// Arrange
		string url = string.Empty;

		// Act & Assert
		await Assert.ThrowsAsync<ArgumentException>(async () => await SubtitleParser.Content(url));
	}

	[Fact]
	public async Task ParseSubtitles_WithWhiteSpaceUrl_ThrowsArgumentException()
	{
		// Arrange
		string url = "   ";

		// Act & Assert
		await Assert.ThrowsAsync<ArgumentException>(async () => await SubtitleParser.Content(url));
	}

	[Fact]
	public async Task ParseSubtitles_WithInvalidUrl_ThrowsUriFormatException()
	{
		// Arrange
		string url = "not a valid url";

		// Act & Assert
		await Assert.ThrowsAsync<UriFormatException>(async () => await SubtitleParser.Content(url));
	}
}
