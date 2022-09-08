using CommunityToolkit.Maui.Core;
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
		((IBorderElement)avatarView).CornerRadius.Should().Be(average);
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
}