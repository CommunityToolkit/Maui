using CommunityToolkit.Maui.Core;
using FluentAssertions;
using Microsoft.Maui.Controls.Shapes;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class AvatarViewImageTests : BaseHandlerTest
{
	public AvatarViewImageTests()
	{
		Assert.IsAssignableFrom<IAvatarView>(new Maui.Views.AvatarView());
	}

	[Fact]
	public void DefaultImageSource()
	{
		var avatarView = new Maui.Views.AvatarView();
		avatarView.ImageSource.Should().BeNull();
	}

	[Fact]
	public void DefaultImageSourceProperties()
	{
		var source = new UriImageSource()
		{
			Uri = new Uri("https://aka.ms/campus.jpg"),
		};
		var avatarView = new Maui.Views.AvatarView
		{
			ImageSource = source
		};
		avatarView.ImageSource.Should().NotBeNull();
		avatarView.Content.Should().BeOfType<Image>();
		if (avatarView.Content is Image avatarImage)
		{
			avatarImage.Should().NotBeNull();
			avatarImage.Aspect.Should().Be(Aspect.AspectFill);
			avatarImage.Source.Should().Be(source);
			avatarImage.Width.Should().Be(-1);
			avatarImage.Height.Should().Be(-1);
			avatarImage.Clip.Should().BeNull();
			avatarImage.BackgroundColor.Should().BeNull();
		}
	}

	[Fact]
	public void ImageSourceBackgroundColor()
	{
		var source = new UriImageSource()
		{
			Uri = new Uri("https://aka.ms/campus.jpg"),
		};
		var avatarView = new Maui.Views.AvatarView
		{
			WidthRequest = 73,
			HeightRequest = 37,
			ImageSource = source,
			BackgroundColor = Colors.Azure,
		};
		avatarView.Layout(new Rect(0, 0, 73, 73));
		avatarView.ImageSource.Should().NotBeNull();
		avatarView.Content.Should().BeOfType<Image>();
		if (avatarView.Content is Image avatarImage)
		{
			avatarImage.BackgroundColor.Should().BeNull();
		}
	}

	[Fact]
	public void ImageSourceChanged()
	{
		var avatarView = new Maui.Views.AvatarView();
		avatarView.ImageSource.Should().BeNull();
		bool signaled = false;
		avatarView.PropertyChanged += (sender, e) =>
		{
			if (e.PropertyName == "ImageSource")
			{
				signaled = true;
			}
		};

		ImageSource source = new UriImageSource()
		{
			Uri = new Uri("https://aka.ms/campus.jpg"),
		};
		avatarView.ImageSource = source;
		avatarView.ImageSource.Should().NotBeNull();
		avatarView.ImageSource.Should().Be(source);
		signaled.Should().BeTrue();
	}

	[Fact]
	public void ImageSourceChangedToNull()
	{
		var avatarView = new Maui.Views.AvatarView();
		avatarView.ImageSource.Should().BeNull();
		ImageSource source = new UriImageSource()
		{
			Uri = new Uri("https://aka.ms/campus.jpg"),
		};
		avatarView.ImageSource = source;
		avatarView.ImageSource.Should().NotBeNull();
		avatarView.ImageSource.Should().Be(source);
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		avatarView.ImageSource = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
		avatarView.ImageSource.Should().BeNull();
	}

	[Fact]
	public void ImageSourceIsNotEmpty()
	{
		var avatarView = new Maui.Views.AvatarView();
		ImageSource source = ImageSource.FromFile("File.png");
		avatarView.ImageSource = source;
		avatarView.ImageSource.IsEmpty.Should().BeFalse();
	}

	[Fact]
	public void ImageSourceParentSize_WhenStrokeShapeNotSet()
	{
		var source = new UriImageSource()
		{
			Uri = new Uri("https://aka.ms/campus.jpg"),
		};
		var avatarView = new Maui.Views.AvatarView
		{
			WidthRequest = 73,
			HeightRequest = 37,
			ImageSource = source
		};
		avatarView.Layout(new Rect(0, 0, 73, 73));
		avatarView.ImageSource.Should().NotBeNull();
		avatarView.Content.Should().BeOfType<Image>();
		if (avatarView.Content is Image avatarImage)
		{
			avatarImage.WidthRequest.Should().Be(73);
			avatarImage.HeightRequest.Should().Be(37);

			avatarImage.Clip.Should().BeNull();
		}
	}

	[Fact]
	public void ImageSourceParentSize_WhenStrokeShapeSet()
	{
		var source = new UriImageSource()
		{
			Uri = new Uri("https://aka.ms/campus.jpg"),
		};
		var avatarView = new Maui.Views.AvatarView
		{
			WidthRequest = 73,
			HeightRequest = 37,
			ImageSource = source,
			StrokeShape = new Rectangle
			{
				Clip = new RectangleGeometry()
			}
		};
		avatarView.Layout(new Rect(0, 0, 73, 73));
		avatarView.ImageSource.Should().NotBeNull();
		avatarView.Content.Should().BeOfType<Image>();
		if (avatarView.Content is Image avatarImage)
		{
			avatarImage.WidthRequest.Should().Be(73);
			avatarImage.HeightRequest.Should().Be(37);

			avatarImage.Clip.Should().BeOfType<RectangleGeometry>();
		}
	}
}