using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CommunityToolkit.Maui.Core;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

public class VttParserTests : BaseTest
{
    [Fact]
    public void ParseVttFile_ValidFile_ReturnsCorrectCues()
    {
        // Arrange
        var content = @"WEBVTT

00:00:00.000 --> 00:00:05.000
This is the first cue.

00:00:05.000 --> 00:00:10.000
This is the second cue.";

        // Act
		VttParser vttParser = new();
        var cues = vttParser.ParseContent(content);

        // Assert
        Assert.Equal(2, cues.Count);
        Assert.Equal(TimeSpan.Zero, cues[0].StartTime);
        Assert.Equal(TimeSpan.FromSeconds(5), cues[0].EndTime);
        Assert.Equal("This is the first cue.", cues[0].Text);
        Assert.Equal(TimeSpan.FromSeconds(5), cues[1].StartTime);
        Assert.Equal(TimeSpan.FromSeconds(10), cues[1].EndTime);
        Assert.Equal("This is the second cue.", cues[1].Text);
    }

    [Fact]
    public void ParseVttFile_EmptyFile_ReturnsEmptyList()
    {
        // Arrange
        var content = string.Empty;

        // Act
		VttParser vttParser = new();
        var cues = vttParser.ParseContent(content);

        // Assert
        Assert.Empty(cues);
    }

	[Fact]
	public void ParseVttFile_InvalidFormat_ThrowsException()
	{
		// Arrange
		var vttContent = "Invalid format";

		// Act & Assert
		VttParser vttParser = new();
		Assert.Throws<FormatException>(() => vttParser.ParseContent(vttContent));
	}

	[Fact]
    public void ParseVttFile_InvalidTimestamps_ThrowsException()
    {
        // Arrange
        var content = @"WEBVTT

00:00:00.000 --> 00:00:05.000
This is the first cue.

00:00:10.000 --> 00:00:05.000
This is the second cue.";

        // Act & Assert
		VttParser vttParser = new();
        Assert.Throws<FormatException>(() => vttParser.ParseContent(content));
    }
}