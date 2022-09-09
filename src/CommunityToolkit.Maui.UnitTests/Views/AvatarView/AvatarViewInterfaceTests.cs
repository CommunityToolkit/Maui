using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views.AvatarView;

public class AvatarViewInterfaceTests : BaseHandlerTest
{
	public AvatarViewInterfaceTests()
	{
		Assert.IsAssignableFrom<IAvatarView>(new Maui.Views.AvatarView());
	}

	[Fact]
	public void IBorderElementBorderColorDefaultValue()
	{
		var avatarView = new Maui.Views.AvatarView();
		((IBorderElement)avatarView).BorderColorDefaultValue.Should().Be(AvatarViewDefaults.DefaultBorderColor);
	}

	[Fact]
	public void IBorderElementBorderWidthDefaultValue()
	{
		var avatarView = new Maui.Views.AvatarView();
		((IBorderElement)avatarView).BorderWidthDefaultValue.Should().Be(AvatarViewDefaults.DefaultBorderWidth);
	}

	[Fact]
	public void IBorderElementCornerRadius()
	{
		var avatarView = new Maui.Views.AvatarView();
		int average = (int)(new[] { AvatarViewDefaults.DefaultCornerRadius.TopLeft, AvatarViewDefaults.DefaultCornerRadius.TopRight, AvatarViewDefaults.DefaultCornerRadius.BottomLeft, AvatarViewDefaults.DefaultCornerRadius.BottomRight }).Average();
		((IBorderElement)avatarView).CornerRadius.Should().Be(average);
		CornerRadius cornerRadius = new(3, 7, 37, 73);
		average = (int)(new[] { cornerRadius.TopLeft, cornerRadius.TopRight, cornerRadius.BottomLeft, cornerRadius.BottomRight }).Average();
		avatarView.CornerRadius = cornerRadius;
		((IBorderElement)avatarView).CornerRadius.Should().Be(average);
	}

	[Fact]
	public void IBorderElementCornerRadiusDefaultValue()
	{
		var avatarView = new Maui.Views.AvatarView();
		int average = (int)(new[] { AvatarViewDefaults.DefaultCornerRadius.TopLeft, AvatarViewDefaults.DefaultCornerRadius.TopRight, AvatarViewDefaults.DefaultCornerRadius.BottomLeft, AvatarViewDefaults.DefaultCornerRadius.BottomRight }).Average();
		((IBorderElement)avatarView).CornerRadiusDefaultValue.Should().Be(average);
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
	public void IImageElementIsAnimationPlaying()
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
			((IImageElement)avatarView).IsAnimationPlaying.Should().BeFalse();
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
	public void IImageElementIsLoading()
	{
		var avatarView = new Maui.Views.AvatarView();
		((IImageElement)avatarView).IsLoading.Should().BeFalse();
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
	public void IImageElementSource()
	{
		var avatarView = new Maui.Views.AvatarView();
		((IImageElement)avatarView).Source.Should().BeNull();
		avatarView.ImageSource = new UriImageSource()
		{
			Uri = new Uri("https://aka.ms/campus.jpg"),
		};
		((IImageElement)avatarView).Source.Should().NotBeNull();
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

	[Fact]
	public void IsBackgroundColorSet()
	{
		var avatarView = new Maui.Views.AvatarView();
		((IBorderElement)avatarView).IsBackgroundColorSet().Should().BeFalse();
		avatarView.BackgroundColor = Colors.Azure;
		((IBorderElement)avatarView).IsBackgroundColorSet().Should().BeTrue();
	}

	[Fact]
	public void IsBackgroundSet()
	{
		var avatarView = new Maui.Views.AvatarView();
		((IBorderElement)avatarView).IsBackgroundSet().Should().BeFalse();
		avatarView.Background = Colors.Azure;
		((IBorderElement)avatarView).IsBackgroundSet().Should().BeTrue();
	}

	[Fact]
	public void IsBorderColorSet()
	{
		var avatarView = new Maui.Views.AvatarView();
		((IBorderElement)avatarView).IsBorderColorSet().Should().BeFalse();
		avatarView.BorderColor = Colors.Azure;
		((IBorderElement)avatarView).IsBorderColorSet().Should().BeTrue();
	}

	[Fact]
	public void IsBorderWidthSet()
	{
		var avatarView = new Maui.Views.AvatarView();
		((IBorderElement)avatarView).IsBorderWidthSet().Should().BeFalse();
		avatarView.BorderWidth = 2;
		((IBorderElement)avatarView).IsBorderWidthSet().Should().BeTrue();
	}

	[Fact]
	public void IsCornerRadiusSet()
	{
		var avatarView = new Maui.Views.AvatarView();
		((IBorderElement)avatarView).IsCornerRadiusSet().Should().BeFalse();
		avatarView.CornerRadius = 3;
		((IBorderElement)avatarView).IsCornerRadiusSet().Should().BeTrue();
	}

	[Fact]
	public void IBorderElementOnBorderColorPropertyChanged()
	{
		var avatarView = new Maui.Views.AvatarView();
		avatarView.Stroke.Should().Be((SolidColorBrush)AvatarViewDefaults.DefaultBorderColor);
		((IBorderElement)avatarView).OnBorderColorPropertyChanged(Colors.AliceBlue, Colors.Azure);
		avatarView.Stroke.Should().Be((SolidColorBrush)Colors.Azure);
	}

	[Fact]
	public void ITextAlignmentElementOnHorizontalTextAlignmentPropertyChanged()
	{
		// For code coverage
		var avatarView = new Maui.Views.AvatarView();
		avatarView.Content.Should().BeOfType<Label>();
		if (avatarView.Content is Label avatarLabel)
		{
			avatarLabel.Should().NotBeNull();
			avatarLabel.HorizontalTextAlignment.Should().Be(TextAlignment.Center);
			((ITextAlignmentElement)avatarView).OnHorizontalTextAlignmentPropertyChanged(TextAlignment.Center, TextAlignment.End);
		}
	}

	[Fact]
	public void IImageElementOnImageSourceSourceChanged()
	{
		// For code coverage
		var avatarView = new Maui.Views.AvatarView();
		((IImageElement)avatarView).Source.Should().BeNull();
		avatarView.ImageSource = new UriImageSource()
		{
			Uri = new Uri("https://aka.ms/campus.jpg"),
		};

		((IImageElement)avatarView).OnImageSourceSourceChanged(this, EventArgs.Empty);
	}

	[Fact]
	public void IImageElementRaiseImageSourcePropertyChanged()
	{
		// For code coverage
		var avatarView = new Maui.Views.AvatarView();
		((IImageElement)avatarView).Source.Should().BeNull();
		avatarView.ImageSource = new UriImageSource()
		{
			Uri = new Uri("https://aka.ms/campus.jpg"),
		};

		((IImageElement)avatarView).RaiseImageSourcePropertyChanged();
	}

	[Fact]
	public void ITextElementUpdateFormsText()
	{
		// For code coverage
		var avatarView = new Maui.Views.AvatarView();
		((ITextElement)avatarView).UpdateFormsText("Original Text", TextTransform.Uppercase);
		avatarView.Text.Should().Be("?");
	}

	[Fact]
	public void IImageSourcePartUpdateIsLoading()
	{
		// For code coverage
		var handler = new FontElementHandlerStub();
		var avatarView = new Maui.Views.AvatarView()
		{
			Handler = handler
		};
		handler.Updates.Clear();
		((IImageSourcePart)avatarView).UpdateIsLoading(true);
		((IImageSourcePart)avatarView).UpdateIsLoading(false);
		avatarView.Text.Should().Be("?");
	}

	[Fact]
	public void ILineHeightElementOnLineHeightChanged()
	{
		// For code coverage
		var avatarView = new Maui.Views.AvatarView();
		((Microsoft.Maui.Controls.Internals.ILineHeightElement)avatarView).OnLineHeightChanged(0.0, 3.7);
		avatarView.Text.Should().Be("?");
	}
}