using System.Globalization;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Views;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class MediaSourceConverterTests : BaseTest
{
	[Fact]
	public void MediaSourceConverterShouldConvertFromStringType()
	{
		var mediaSourceConverter = new MediaSourceConverter();
		var mediaSourceCanConvert = mediaSourceConverter.CanConvertFrom(null, typeof(string));

		mediaSourceCanConvert.Should().BeTrue();
	}

	[Fact]
	public void MediaSourceConverterShouldConvertToStringType()
	{
		var mediaSourceConverter = new MediaSourceConverter();
		var mediaSourceCanConvert = mediaSourceConverter.CanConvertTo(null, typeof(string));

		mediaSourceCanConvert.Should().BeTrue();
	}

	[Fact]
	public void MediaSourceConverterWithEmbedPrefixShouldBeResourceMediaSource()
	{
		var mediaSourceConverter = new MediaSourceConverter();
		var mediaSource = mediaSourceConverter.ConvertFrom(null, CultureInfo.InvariantCulture, "embed://file.mp4");

		mediaSource.Should().BeAssignableTo<ResourceMediaSource>();
	}

	[Fact]
	public void MediaSourceConverterWithFileSystemPrefixShouldBeResourceMediaSource()
	{
		var mediaSourceConverter = new MediaSourceConverter();
		var mediaSource = mediaSourceConverter.ConvertFrom(null, CultureInfo.InvariantCulture, "filesystem://file.mp4");

		mediaSource.Should().BeAssignableTo<FileMediaSource>();
	}
}