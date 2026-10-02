using CommunityToolkit.Maui.Core;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class AvatarViewInterfaceTests : BaseViewTest
{
	public AvatarViewInterfaceTests()
	{
		Assert.IsType<IAvatarView>(new Maui.Views.AvatarView(), exactMatch: false);
	}

	[Fact]
	public void ITextAlignmentHorizontalTextAlignment()
	{
		var avatarView = new Maui.Views.AvatarView();
		avatarView.Content.Should().BeOfType<Label>();
		if (avatarView.Content is Label avatarLabel)
		{
			avatarLabel.Should().NotBeNull();
			((ITextAlignment)avatarView).HorizontalTextAlignment.Should().Be(avatarLabel.HorizontalTextAlignment);
			((ITextAlignment)avatarView).HorizontalTextAlignment.Should().Be(TextAlignment.Center);
		}
	}

	[Fact]
	public void IImageSourcePartIsAnimationPlaying()
	{
		var avatarView = new Maui.Views.AvatarView
		{
			ImageSource = new UriImageSource()
			{
				Uri = new Uri("https://aka.ms/campus.jpg"),
			}
		};
		avatarView.Content.Should().BeOfType<Image>();
		if (avatarView.Content is Image avatarImage)
		{
			avatarImage.Should().NotBeNull();
			((IImageSourcePart)avatarView).IsAnimationPlaying.Should().BeFalse();
		}
	}

	[Fact]
	public void IImageSourceIsEmpty()
	{
		var avatarView = new Maui.Views.AvatarView();
		((IImageSource)avatarView).IsEmpty.Should().BeTrue();
		avatarView.ImageSource = new UriImageSource()
		{
			Uri = new Uri("https://aka.ms/campus.jpg"),
		};
		((IImageSource)avatarView).IsEmpty.Should().BeFalse();
	}

	[Fact]
	public void IImageIsOpaque()
	{
		var avatarView = new Maui.Views.AvatarView();
		((Microsoft.Maui.IImage)avatarView).IsOpaque.Should().BeFalse();
	}

	[Fact]
	public void ILabelLineHeight()
	{
		var avatarView = new Maui.Views.AvatarView();
		avatarView.Content.Should().BeOfType<Label>();
		if (avatarView.Content is Label avatarLabel)
		{
			avatarLabel.Should().NotBeNull();
			((ILabel)avatarView).LineHeight.Should().Be(-1);
			avatarLabel.LineHeight = 7;
			((ILabel)avatarView).LineHeight.Should().Be(7);
			avatarLabel.LineHeight.Should().Be(7);
		}
	}

	[Fact]
	public void IImageSourcePartSource()
	{
		var avatarView = new Maui.Views.AvatarView();
		((IImageSourcePart)avatarView).Source.Should().BeNull();
		avatarView.ImageSource = new UriImageSource()
		{
			Uri = new Uri("https://aka.ms/campus.jpg"),
		};
		((IImageSourcePart)avatarView).Source.Should().NotBeNull();
	}

	[Fact]
	public void ITextAlignmentVerticalTextAlignment()
	{
		var avatarView = new Maui.Views.AvatarView();
		avatarView.Content.Should().BeOfType<Label>();
		if (avatarView.Content is Label avatarLabel)
		{
			avatarLabel.Should().NotBeNull();
			((ITextAlignment)avatarView).VerticalTextAlignment.Should().Be(avatarLabel.VerticalTextAlignment);
			((ITextAlignment)avatarView).VerticalTextAlignment.Should().Be(TextAlignment.Center);
		}
	}

	[Fact]
	public void IImageAspect()
	{
		var avatarView = new Maui.Views.AvatarView();
		((Microsoft.Maui.IImage)avatarView).Aspect.Should().Be(Aspect.AspectFill);
	}

	[Fact]
	public void ILabelTextDecorations()
	{
		var avatarView = new Maui.Views.AvatarView();
		((ILabel)avatarView).TextDecorations.Should().Be(TextDecorations.None);
	}

}