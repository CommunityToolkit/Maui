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

	[Fact]
	public void MediaSourceFromStreamShouldBeStreamMediaSource()
	{
		using var stream = new System.IO.MemoryStream();
		var mediaSource = MediaSource.FromStream(stream);

		mediaSource.Should().BeAssignableTo<StreamMediaSource>();
	}

#pragma warning disable CS8600, CS8604, CS8625 // The tests pass null to parameters that are not nullable on purpose
	[Fact]
	public void FromUriWithNullStringShouldThrowArgumentNullExceptionNamedUri()
	{
		var exception = Assert.Throws<ArgumentNullException>(() => MediaSource.FromUri((string?)null));

		exception.ParamName.Should().Be("uri");
	}

	[Fact]
	public void FromUriWithNullUriShouldThrowArgumentNullException()
	{
		var exception = Assert.Throws<ArgumentNullException>(() => MediaSource.FromUri((Uri?)null));

		exception.ParamName.Should().Be("uri");
	}

	[Fact]
	public void FromUriWithHeadersAndNullUriShouldThrowArgumentNullException()
	{
		var exception = Assert.Throws<ArgumentNullException>(() => MediaSource.FromUri(null, new Dictionary<string, string>()));

		exception.ParamName.Should().Be("uri");
	}

	[Fact]
	public void FromUriWithNullHeadersShouldThrowArgumentNullException()
	{
		var exception = Assert.Throws<ArgumentNullException>(() => MediaSource.FromUri(new Uri("https://example.com/video.mp4"), null));

		exception.ParamName.Should().Be("httpHeaders");
	}

	[Fact]
	public void ImplicitConversionFromNullUriShouldThrowArgumentNullException()
	{
		Uri? uri = null;

		Assert.Throws<ArgumentNullException>(() => (MediaSource)uri);
	}
#pragma warning restore CS8600, CS8604, CS8625
}
