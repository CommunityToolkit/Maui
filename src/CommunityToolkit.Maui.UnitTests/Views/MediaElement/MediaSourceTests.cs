using CommunityToolkit.Maui.Views;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class MediaSourceTests : BaseTest
{
	[Fact]
	public void MediaSourceFromResourceShouldBeUriMediaSource()
	{
		var mediaSource = MediaSource.FromUri("https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4");

		mediaSource.Should().BeAssignableTo<UriMediaSource>();
	}

	[Fact]
	public void MediaSourceInvalidUriShouldThrowUriFormatException()
	{
		this.Invoking(player =>
		{
			var mediaSource = MediaSource.FromUri("invaliduri");
		}).Should().Throw<UriFormatException>();
	}

	[Fact]
	public void MediaSourceFromResourceShouldBeResourceMediaSource()
	{
		var mediaSource = MediaSource.FromResource("file.mp4");

		mediaSource.Should().BeAssignableTo<ResourceMediaSource>();
	}

	[Fact]
	public void MediaSourceFromFileShouldBeFileMediaSource()
	{
		var mediaSource = MediaSource.FromFile("file.mp4");

		mediaSource.Should().BeAssignableTo<FileMediaSource>();
	}
}