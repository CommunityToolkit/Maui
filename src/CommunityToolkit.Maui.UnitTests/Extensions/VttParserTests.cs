using System.Text.RegularExpressions;
using CommunityToolkit.Maui.Core;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

public class VttParserTests : BaseTest
{
	[Fact]
	public void RegexImportTest()
	{
		// This test verifies that the Regex namespace is available
		// and can be used without throwing exceptions
		Assert.NotNull(typeof(Regex));
	}

	[Fact]
	public void RegexMatchTest()
	{
		// Test a simple regex pattern to ensure Regex functionality
		string pattern = @"\d+";
		string input = "Test 123";
		var match = Regex.Match(input, pattern);
		Assert.True(match.Success);
		Assert.Equal("123", match.Value);
	}

	[Fact]
	public void RegexReplaceTest()
	{
		// Test Regex.Replace functionality
		string pattern = @"\s+";
		string input = "Hello   World";
		string result = Regex.Replace(input, pattern, " ");
		Assert.Equal("Hello World", result);
	}

	[Fact]
	public void RegexSplitTest()
	{
		// Test Regex.Split functionality
		string pattern = @",\s*";
		string input = "apple, banana,cherry, date";
		string[] result = Regex.Split(input, pattern);
		Assert.Equal(4, result.Length);
		Assert.Equal("apple", result[0]);
		Assert.Equal("date", result[3]);
	}

	readonly VttParser parser = new();

	[Fact]
	public void ParseContent_ValidVTTFile_ReturnsCorrectSubtitleDocument()
	{
		var content = @"WEBVTT

00:00:01.000 --> 00:00:04.000
This is the first subtitle

00:00:05.000 --> 00:00:08.000
This is the second subtitle";

		var result = parser.ParseContent(content);

		Assert.Equal("WEBVTT", result.Header);
		Assert.Equal(2, result.Cues.Count);
		Assert.Equal(TimeSpan.FromSeconds(1), result.Cues[0].StartTime);
		Assert.Equal(TimeSpan.FromSeconds(4), result.Cues[0].EndTime);
		Assert.Equal("This is the first subtitle", result.Cues[0].RawText);
	}

	[Fact]
	public void ParseContent_VTTFileWithCueSettings_ParsesCueSettingsCorrectly()
	{
		var content = @"WEBVTT

00:00:01.000 --> 00:00:04.000 vertical:rl line:0 position:50% align:start
This is a subtitle with cue settings";

		var result = parser.ParseContent(content);

		Assert.Single(result.Cues);
		var cue = result.Cues[0];
		Assert.Equal("rl", cue.Vertical);
		Assert.Equal("0", cue.Line);
		Assert.Equal("50%", cue.Position);
		Assert.Equal("start", cue.Align);
	}

	[Fact]
	public void ParseContent_VTTFileWithStyleAndMetadataCue_ParsesCorrectly()
	{
		var content = @"WEBVTT

NOTE This is a comment

STYLE
::cue {
  background-color: yellow;
  color: black;
}

00:00:01.000 --> 00:00:04.000
This is a regular subtitle

MetadataCue
This is metadata content";

		var result = parser.ParseContent(content);

		Assert.Equal(2, result.Cues.Count);
		Assert.IsType<SubtitleCue>(result.Cues[0]);
		Assert.IsType<SubtitleMetadataCue>(result.Cues[1]);

		Assert.NotNull(result.StyleBlock);
		Assert.Contains("background-color: yellow;", result.StyleBlock);
		Assert.Contains("color: black;", result.StyleBlock);

		var metadataCue = result.Cues[1] as SubtitleMetadataCue;
		Assert.NotNull(metadataCue);
		Assert.Equal("MetadataCue", metadataCue.Id);
		Assert.Equal("This is metadata content", metadataCue.Data);
	}

	[Fact]
	public void ParseContent_VTTFileWithFormattedText_ParsesCueTextCorrectly()
	{
		var content = @"WEBVTT

00:00:01.000 --> 00:00:04.000
This is <b>bold</b> and <i>italic</i> text";

		var result = parser.ParseContent(content);

		Assert.Single(result.Cues);
		var cue = result.Cues[0];
		Assert.NotNull(cue);
		var parsedText = cue.ParsedCueText;
		Assert.NotNull(parsedText);
		Assert.Equal("root", parsedText.NodeType);
		Assert.Equal(5, parsedText.Children.Count);

		Assert.Equal("text", parsedText.Children[0].NodeType);
		Assert.Equal("This is ", parsedText.Children[0].TextContent);

		Assert.Equal("b", parsedText.Children[1].NodeType);
		Assert.Single(parsedText.Children[1].Children);
		Assert.Equal("bold", parsedText.Children[1].Children[0].TextContent);

		Assert.Equal("text", parsedText.Children[2].NodeType);
		Assert.Equal(" and ", parsedText.Children[2].TextContent);

		Assert.Equal("i", parsedText.Children[3].NodeType);
		Assert.Single(parsedText.Children[3].Children);
		Assert.Equal("italic", parsedText.Children[3].Children[0].TextContent);

		Assert.Equal("text", parsedText.Children[4].NodeType);
		Assert.Equal(" text", parsedText.Children[4].TextContent);
	}

	[Fact]
	public void ParseContent_InvalidVTTFile_ThrowsFormatException()
	{
		var content = "This is not a valid VTT file";

		Assert.Throws<FormatException>(() => parser.ParseContent(content));
	}
}