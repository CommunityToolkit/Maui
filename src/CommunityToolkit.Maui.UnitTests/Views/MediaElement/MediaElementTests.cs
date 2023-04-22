using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class MediaElementTests : BaseHandlerTest
{
	public MediaElementTests()
	{
		Assert.IsAssignableFrom<IMediaElement>(new Maui.Views.MediaElement());
	}

	[Fact]
	public void BindingContextPropagation()
	{
		object context = new();
		Maui.Views.MediaElement mediaElement = new();
		FileMediaSource mediaSource = new();

		mediaElement.Source = mediaSource;

		mediaElement.BindingContext = context;

		mediaSource.BindingContext.Should().Be(context);
	}

	[Fact]
	public void MediaElementShouldBeAssignedToIMediaElement()
	{
		new Maui.Views.MediaElement().Should().BeAssignableTo<IMediaElement>();
	}

	[Fact]
	public void MediaElementVolumeShouldNotBeMoreThan1()
	{
		Maui.Views.MediaElement mediaElement = new();

		mediaElement.Invoking(player =>
		{
			mediaElement.Volume = 2d;
		}).Should().Throw<ArgumentException>();
	}

	[Fact]
	public void MediaElementVolumeShouldNotBeLessThan0()
	{
		Maui.Views.MediaElement mediaElement = new();

		mediaElement.Invoking(player =>
		{
			mediaElement.Volume = -2d;
		}).Should().Throw<ArgumentException>();
	}
}