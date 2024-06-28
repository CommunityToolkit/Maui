using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;
public class FontExtensionsTests : BaseTest
{
	[Fact]
	public void FontFamily_TTF_Android_ReturnsExpectedResult()
	{
		// Arrange
		MediaElement mediaElement = new()
		{
			SubtitleFont = "Font.ttf#Font"
		};

		// Act
		var result = new Core.FontExtensions.FontFamily(mediaElement.SubtitleFont).Android;

		// Assert
		Assert.Equal("Font.ttf", result);
	}

	[Fact]
	public void FontFamily_TTF_WindowsFont_ReturnsExpectedResult()
	{
		// Arrange
		MediaElement mediaElement = new()
		{
			SubtitleFont = "Font.ttf#Font"
		};

		// Act
		var result = new Core.FontExtensions.FontFamily(mediaElement.SubtitleFont).WindowsFont;

		// Assert
		Assert.Equal("ms-appx:///Font.ttf#Font", result);
	}

	[Fact]
	public void FontFamily_TTF_MacIOS_ReturnsExpectedResult()
	{
		// Arrange
		MediaElement mediaElement = new()
		{
			SubtitleFont = "Font.ttf#Font"
		};

		// Act
		var result = new Core.FontExtensions.FontFamily(mediaElement.SubtitleFont).MacIOS;

		// Assert
		Assert.Equal("Font", result);
	}

	[Fact]
	public void FontFamily_OTF_Android_ReturnsExpectedResult()
	{
		// Arrange
		MediaElement mediaElement = new()
		{
			SubtitleFont = "Font.otf#Font"
		};

		// Act
		var result = new Core.FontExtensions.FontFamily(mediaElement.SubtitleFont).Android;

		// Assert
		Assert.Equal("Font.otf", result);
	}

	[Fact]
	public void FontFamily_OTF_WindowsFont_ReturnsExpectedResult()
	{
		// Arrange
		MediaElement mediaElement = new()
		{
			SubtitleFont = "Font.otf#Font"
		};

		// Act
		var result = new Core.FontExtensions.FontFamily(mediaElement.SubtitleFont).WindowsFont;

		// Assert
		Assert.Equal("ms-appx:///Font.otf#Font", result);
	}

	[Fact]
	public void FontFamily_OTF_MacIOS_ReturnsExpectedResult()
	{
		// Arrange
		MediaElement mediaElement = new()
		{
			SubtitleFont = "Font.otf#Font"
		};

		// Act
		var result = new Core.FontExtensions.FontFamily(mediaElement.SubtitleFont).MacIOS;

		// Assert
		Assert.Equal("Font", result);
	}

	[Fact]
	public void FontFamily_TTF_Android_InvalidInput_ReturnsEmptyString()
	{
		// Arrange
		MediaElement mediaElement = new()
		{
			SubtitleFont = "Invalid input"
		};
		var result = new Core.FontExtensions.FontFamily(mediaElement.SubtitleFont).Android;

		// Act & Assert
		Assert.Equal(string.Empty, result);
	}

	[Fact]
	public void FontFamily_TTF_WindowsFont_InvalidInput_ReturnsEmptyString()
	{
		// Arrange
		MediaElement mediaElement = new()
		{
			SubtitleFont = "Invalid input"
		};
		var result = new Core.FontExtensions.FontFamily(mediaElement.SubtitleFont).WindowsFont;

		// Act & Assert
		Assert.Equal(string.Empty, result);
	}

	[Fact]
	public void FontFamily_TTF_MacIOS_InvalidInput_ReturnsEmptyString()
	{
		// Arrange
		MediaElement mediaElement = new()
		{
			SubtitleFont = "Invalid input"
		};
		var result = new Core.FontExtensions.FontFamily(mediaElement.SubtitleFont).MacIOS;

		// Act & Assert
		Assert.Equal(string.Empty, result);
	}
}
