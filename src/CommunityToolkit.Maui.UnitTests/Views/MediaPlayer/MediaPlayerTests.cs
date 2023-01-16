using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.MediaPlayer;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views.MediaPlayer;

public class MediaPlayerTests : BaseHandlerTest
{
	public MediaPlayerTests()
	{
		Assert.IsAssignableFrom<IMediaPlayer>(new Maui.Views.MediaPlayer());
	}

	[Fact]
	public void BindingContextPropagation()
	{
		object context = new();
		Maui.Views.MediaPlayer mediaPlayer = new();
		FileMediaSource mediaSource = new();

		mediaPlayer.Source = mediaSource;

		mediaPlayer.BindingContext = context;

		mediaSource.BindingContext.Should().Be(context);
	}

	[Fact]
	public void MediaPlayerShouldBeAssignedToIMediaPlayer()
	{
		new Maui.Views.MediaPlayer().Should().BeAssignableTo<IMediaPlayer>();
	}

	[Fact]
	public void MediaPlayerVolumeShouldNotBeMoreThan1()
	{
		Maui.Views.MediaPlayer mediaPlayer = new();

		mediaPlayer.Invoking(player =>
		{
			mediaPlayer.Volume = 2d;
		}).Should().Throw<ArgumentException>();
	}

	[Fact]
	public void MediaPlayerVolumeShouldNotBeLessThan0()
	{
		Maui.Views.MediaPlayer mediaPlayer = new();

		mediaPlayer.Invoking(player =>
		{
			mediaPlayer.Volume = -2d;
		}).Should().Throw<ArgumentException>();
	}
}
