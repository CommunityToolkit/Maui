using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Shapes;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views.AvatarView;

public class AvatarViewTests : BaseHandlerTest
{
	public AvatarViewTests()
	{
		Assert.IsAssignableFrom<IAvatarView>(new Maui.Views.AvatarView());
	}

	[Fact]
	public void AvatarViewShouldBeAssignedToIAvatarView()
	{
		new Maui.Views.AvatarView().Should().BeAssignableTo<IAvatarView>();
	}

	[Fact]
	public void BindingContextPropagation()
	{
		object context = new();
		Maui.Views.AvatarView avatar = new()
		{
			BindingContext = context
		};
		FileImageSource source = new();
		avatar.ImageSource = source;
		source.BindingContext.Should().Be(context);
	}

	[Fact]
	public void BorderColorToBlack()
	{
		var avatarView = new Maui.Views.AvatarView()
		{
			BorderColor = Colors.Red,
		};
		avatarView.BorderColor.Should().Be(Colors.Red);

		bool signaled = false;
		avatarView.PropertyChanged += (sender, e) =>
		{
			if (e.PropertyName == "BorderColor")
			{
				signaled = true;
			}
		};

		avatarView.BorderColor = Colors.Black;
		avatarView.BorderColor.Should().Be(Colors.Black);
		signaled.Should().BeTrue();
	}

	[Fact]
	public void BorderWidthToSeven()
	{
		var avatarView = new Maui.Views.AvatarView()
		{
			BorderWidth = 2,
		};
		avatarView.BorderWidth.Should().Be(2);
		bool signaled = false;
		avatarView.PropertyChanged += (sender, e) =>
		{
			if (e.PropertyName == "BorderWidth")
			{
				signaled = true;
			}
		};

		avatarView.BorderWidth = 7;
		avatarView.BorderWidth.Should().Be(7);
		signaled.Should().BeTrue();
	}

	[Fact]
	public void CharacterSpacingProperty()
	{
		var avatarView = new Maui.Views.AvatarView();
		avatarView.CharacterSpacing.Should().Be(0);
		avatarView.CharacterSpacing = 1;
		avatarView.CharacterSpacing.Should().Be(1);
	}

	[Fact]
	public void ConstructorTest()
	{
		var avatarView = new Maui.Views.AvatarView()
		{
			BorderColor = Colors.Beige,
			BorderWidth = 2,
			CornerRadius = new CornerRadius(4, 8, 12, 16),
			TextColor = Colors.Pink,
			Text = "GL",
			TextTransform = TextTransform.Lowercase,
			WidthRequest = 10,
			HeightRequest = 20,
		};

		avatarView.BorderColor.Should().Be(Colors.Beige);
		avatarView.BorderWidth.Should().Be(2);
		avatarView.CornerRadius.Should().Be(new CornerRadius(4, 8, 12, 16));
		avatarView.TextColor.Should().Be(Colors.Pink);
		avatarView.Text.Should().Be("GL");
		avatarView.TextTransform.Should().Be(TextTransform.Lowercase);
		Size request = avatarView.Measure(double.PositiveInfinity, double.PositiveInfinity).Request;
		request.Width.Should().Be(10);
		request.Height.Should().Be(20);
	}

	/// <summary>This test is specifically to test the use ofCornerRadius of type Maui.CornerRadius.</summary>
	[Fact]
	public void CornerRadiusFourCornerRadiusToOneTwoThreeFour()
	{
		var avatarView = new Maui.Views.AvatarView()
		{
			CornerRadius = new CornerRadius(3, 4, 5, 6),
		};
		avatarView.CornerRadius.Should().Be(new CornerRadius(3, 4, 5, 6));
		bool signaled = false;
		avatarView.PropertyChanged += (sender, e) =>
		{
			if (e.PropertyName == "CornerRadius")
			{
				signaled = true;
			}
		};

		avatarView.CornerRadius = new CornerRadius(1, 2, 3, 4);
		avatarView.CornerRadius.Should().Be(new CornerRadius(1, 2, 3, 4));
		signaled.Should().BeTrue();
	}

	/// <summary>This test is specifically to test the use of CornerRadius of type Int.</summary>
	[Fact]
	public void CornerRadiusSameRadiusToThree()
	{
		var avatarView = new Maui.Views.AvatarView()
		{
			CornerRadius = new CornerRadius(7),
		};
		avatarView.CornerRadius.Should().Be(new CornerRadius(7));
		bool signaled = false;
		avatarView.PropertyChanged += (sender, e) =>
		{
			if (e.PropertyName == "CornerRadius")
			{
				signaled = true;
			}
		};

		avatarView.CornerRadius = new CornerRadius(3);
		avatarView.CornerRadius.Should().Be(new CornerRadius(3));
		signaled.Should().BeTrue();
	}

	[Fact]
	public void DefaultBorderColor()
	{
		var avatarView = new Maui.Views.AvatarView();
		avatarView.BorderColor.Should().Be(AvatarViewDefaults.DefaultBorderColor);
	}

	[Fact]
	public void DefaultBorderWidth()
	{
		var avatarView = new Maui.Views.AvatarView();
		avatarView.BorderWidth.Should().Be(AvatarViewDefaults.DefaultBorderWidth);
	}

	[Fact]
	public void DefaultCornerRadius()
	{
		var avatarView = new Maui.Views.AvatarView();
		avatarView.CornerRadius.Should().Be(new CornerRadius(AvatarViewDefaults.DefaultCornerRadius.TopLeft, AvatarViewDefaults.DefaultCornerRadius.TopRight, AvatarViewDefaults.DefaultCornerRadius.BottomLeft, AvatarViewDefaults.DefaultCornerRadius.BottomRight));
	}

	[Fact]
	public void DefaultFontSize()
	{
		var avatarView = new Maui.Views.AvatarView();
		avatarView.FontSize.Should().Be(0);
	}

	[Fact]
	public void DefaultHeightRequest()
	{
		var avatarView = new Maui.Views.AvatarView();
		Size request = avatarView.Measure(double.PositiveInfinity, double.PositiveInfinity).Request;
		request.Height.Should().Be(AvatarViewDefaults.DefaultHeightRequest);
	}

	[Fact]
	public void DefaultLabelProperties()
	{
		var avatarView = new Maui.Views.AvatarView();
		avatarView.Content.Should().BeOfType<Label>();
		if (avatarView.Content is Label avatarLabel)
		{
			avatarLabel.Should().NotBeNull();
			avatarLabel.HorizontalTextAlignment.Should().Be(TextAlignment.Center);
			avatarLabel.VerticalTextAlignment.Should().Be(TextAlignment.Center);
			avatarLabel.Text.Should().Be(AvatarViewDefaults.DefaultText);
		}
	}

	[Fact]
	public void DefaultProperties()
	{
		var avatarView = new Maui.Views.AvatarView();
		avatarView.IsEnabled.Should().BeTrue();
		avatarView.HorizontalOptions.Should().Be(LayoutOptions.Center);
		avatarView.VerticalOptions.Should().Be(LayoutOptions.Center);
		avatarView.HeightRequest.Should().Be(AvatarViewDefaults.DefaultHeightRequest);
		avatarView.WidthRequest.Should().Be(AvatarViewDefaults.DefaultWidthRequest);
		avatarView.Padding.Should().Be(AvatarViewDefaults.DefaultPadding);
		avatarView.Stroke.Should().Be((SolidColorBrush)AvatarViewDefaults.DefaultBorderColor);
		avatarView.StrokeThickness.Should().Be(AvatarViewDefaults.DefaultBorderWidth);
		avatarView.StrokeShape.Should().BeOfType<RoundRectangle>();
	}

	[Fact]
	public void DefaultWidthRequest()
	{
		var avatarView = new Maui.Views.AvatarView();
		Size request = avatarView.Measure(double.PositiveInfinity, double.PositiveInfinity).Request;
		request.Width.Should().Be(AvatarViewDefaults.DefaultWidthRequest);
	}

	[Fact]
	public void FontAttributesPropertyChanged()
	{
		var avatarView = new Maui.Views.AvatarView();
		avatarView.FontAttributes.Should().Be(FontAttributes.None);
		avatarView.FontAttributes = FontAttributes.Bold;
		avatarView.FontAttributes.Should().Be(FontAttributes.Bold);
	}

	[Fact]
	public void FontAutoScalingEnabledPropertyChanged()
	{
		var avatarView = new Maui.Views.AvatarView();
		avatarView.FontAutoScalingEnabled.Should().BeTrue();
		avatarView.FontAutoScalingEnabled = false;
		avatarView.FontAutoScalingEnabled.Should().BeFalse();
	}

	[Fact]
	public void FontFamilyPropertyChanged()
	{
		var avatarView = new Maui.Views.AvatarView();
		avatarView.FontFamily.Should().BeNull();
		avatarView.FontFamily = "Arial";
		avatarView.FontFamily.Should().Be("Arial");
	}

	[Theory]
	[InlineData(nameof(IFontElement.FontAttributes), FontAttributes.Bold)]
	[InlineData(nameof(IFontElement.FontAutoScalingEnabled), false)]
	[InlineData(nameof(IFontElement.FontFamily), "Arial")]
	[InlineData(nameof(IFontElement.FontSize), 10)]
	public void FontPropertyTriggersFontProperty(string propertyName, object value)
	{
		var handler = new FontElementHandlerStub();
		var avatarView = new Maui.Views.AvatarView()
		{
			Handler = handler
		};
		handler.Updates.Clear();
		avatarView.GetType().GetProperty(propertyName)?.SetValue(avatarView, value, null);
		handler.Updates.Should().HaveCount(2);
		Assert.Equal(new[] { propertyName, nameof(ITextStyle.Font) }, handler.Updates);
	}

	[Fact]
	public void TextColorToYellow()
	{
		var avatarView = new Maui.Views.AvatarView()
		{
			TextColor = Colors.Coral,
		};
		avatarView.TextColor.Should().Be(Colors.Coral);
		bool signaled = false;
		avatarView.PropertyChanged += (sender, e) =>
		{
			if (e.PropertyName == "TextColor")
			{
				signaled = true;
			}
		};

		avatarView.TextColor = Colors.Yellow;
		avatarView.TextColor.Should().Be(Colors.Yellow);
		signaled.Should().BeTrue();
	}

	[Fact]
	public void TextSetToZz()
	{
		var avatarView = new Maui.Views.AvatarView()
		{
			Text = "GL"
		};
		avatarView.Text.Should().Be("GL");

		bool signaled = false;
		avatarView.PropertyChanged += (sender, e) =>
		{
			if (e.PropertyName == "Text")
			{
				signaled = true;
			}
		};

		avatarView.Text = "ZZ";
		avatarView.Text.Should().Be("ZZ");
		signaled.Should().BeTrue();
	}

	[Fact]
	public void TextTranformToLowerCase()
	{
		var avatarView = new Maui.Views.AvatarView()
		{
			TextTransform = TextTransform.Uppercase,
		};
		avatarView.TextTransform.Should().Be(TextTransform.Uppercase);
		bool signaled = false;
		avatarView.PropertyChanged += (sender, e) =>
		{
			if (e.PropertyName == "TextTransform")
			{
				signaled = true;
			}
		};

		avatarView.TextTransform = TextTransform.Lowercase;
		avatarView.TextTransform.Should().Be(TextTransform.Lowercase);
		signaled.Should().BeTrue();
	}
}