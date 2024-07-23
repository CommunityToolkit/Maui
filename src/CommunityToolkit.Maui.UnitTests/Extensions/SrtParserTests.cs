using CommunityToolkit.Maui.Core;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

public class SrtParserTests : BaseTest
{
	[Fact]
    public void ParseSrtFile_ValidInput_ReturnsExpectedResult()
    {
        // Arrange
        var srtContent = @"1
00:00:10,000 --> 00:00:13,000
This is the first subtitle.

2
00:00:15,000 --> 00:00:18,000
This is the second subtitle.";

        // Act
		SrtParser srtParser = new();
        var cues = srtParser.ParseContent(srtContent);

		// Assert
		Assert.Equal(TimeSpan.FromSeconds(10), cues.Cues[0].StartTime);
		Assert.Equal(TimeSpan.FromSeconds(13), cues.Cues[0].EndTime);
		Assert.Equal("This is the first subtitle.", cues.Cues[0].RawText);
		Assert.Equal(TimeSpan.FromSeconds(15), cues.Cues[1].StartTime);
		Assert.Equal(TimeSpan.FromSeconds(18), cues.Cues[1].EndTime);
		Assert.Equal("This is the second subtitle.", cues.Cues[1].RawText);
    }

    [Fact]
	public void ParseSrtFile_EmptyInput_ReturnsEmptyList()
	{
		// Arrange
		var srtContent = string.Empty;

		// Act
		var parser = new SrtParser();
		var result = parser.ParseContent(srtContent);

		// Assert
		Assert.Empty(result.Cues);
	}

	[Fact]
	public void ParseSrtFile_InvalidFormat_ThrowsException()
	{
		// Arrange
		var srtContent = "Invalid format";

		// Act & Assert
		var parser = new SrtParser();
		Assert.Throws<FormatException>(() => parser.ParseContent(srtContent));
	}

	[Fact]
	public void ParseSrtFile_InvalidTimestamps_ThrowsException()
	{
		// Arrange
		var content = @"1
00:00:00,000 --> 00:00:05,000
This is the first subtitle.

2
00:00:10,000 --> 00:00:05,000
This is the second subtitle.";

		// Act & Assert
		var parser = new SrtParser();
		Assert.Throws<FormatException>(() => parser.ParseContent(content));
	}
}