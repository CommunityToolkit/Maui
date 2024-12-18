using CommunityToolkit.Maui.Core;
using FluentAssertions;
using Microsoft.Maui.Controls.Shapes;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class AvatarViewImageTests : BaseHandlerTest
{
	public AvatarViewImageTests()
	{
		Assert.IsType<IAvatarView>(new Maui.Views.AvatarView(), exactMatch: false);
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
		const int borderWidth = 5;
		const int widthRequest = 73;
		const int heightRequest = 37;
		const int layoutDiameter = widthRequest;
		var padding = new Thickness(0, 5, 10, 15);

		var avatarView = new Maui.Views.AvatarView
		{
			Padding = padding,
			BorderWidth = borderWidth,
			WidthRequest = widthRequest,
			HeightRequest = heightRequest,
			ImageSource = new UriImageSource
			{
				Uri = new Uri("https://aka.ms/campus.jpg"),
			}
		};
		avatarView.Layout(new Rect(0, 0, layoutDiameter, layoutDiameter));

		avatarView.ImageSource.Should().NotBeNull();
		avatarView.Content.Should().BeOfType<Image>();
		if (avatarView.Content is not Image avatarImage)
		{
			throw new InvalidCastException($"{nameof(avatarView.Content)} must be of type {nameof(Image)}");
		}

		avatarImage.WidthRequest.Should().Be(layoutDiameter - (borderWidth * 2) - padding.Left - padding.Right);
		avatarImage.HeightRequest.Should().Be(layoutDiameter - (borderWidth * 2) - padding.Top - padding.Bottom);

		avatarImage.Clip.Should().BeNull();
	}

	[Fact]
	public void ImageSourceParentSize_WhenStrokeShapeSet()
	{
		const int borderWidth = 5;
		const int widthRequest = 73;
		const int heightRequest = 37;
		const int layoutDiameter = widthRequest;
		var padding = new Thickness(0, 5, 10, 15);

		var avatarView = new Maui.Views.AvatarView
		{
			Padding = padding,
			BorderWidth = borderWidth,
			WidthRequest = widthRequest,
			HeightRequest = heightRequest,
			ImageSource = new UriImageSource()
			{
				Uri = new Uri("https://aka.ms/campus.jpg"),
			},
			StrokeShape = new Rectangle
			{
				Clip = new RectangleGeometry()
			}
		};
		avatarView.Layout(new Rect(0, 0, 73, 73));
		avatarView.ImageSource.Should().NotBeNull();
		avatarView.Content.Should().BeOfType<Image>();

		if (avatarView.Content is not Image avatarImage)
		{
			throw new InvalidCastException($"{nameof(avatarView.Content)} must be of type {nameof(Image)}");
		}

		avatarImage.WidthRequest.Should().Be(layoutDiameter - (borderWidth * 2) - padding.Left - padding.Right);
		avatarImage.HeightRequest.Should().Be(layoutDiameter - (borderWidth * 2) - padding.Top - padding.Bottom);
		avatarImage.Clip.Should().BeOfType<RectangleGeometry>();
	}
}