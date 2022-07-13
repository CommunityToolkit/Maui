namespace CommunityToolkit.Maui.UnitTests.Views.AvatarView;

using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Internals;
using Xunit;

public class AvatarViewTests : BaseHandlerTest
{
	[Fact]
	public void ConstructorTest()
	{
		AvatarView avatarView = new()
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

		Assert.Equal(Colors.Beige, avatarView.BorderColor);
		Assert.Equal(2, avatarView.BorderWidth);
		Assert.Equal(new CornerRadius(4, 8, 12, 16), avatarView.CornerRadius);
		Assert.Equal(Colors.Pink, avatarView.TextColor);
		Assert.Equal("GL", avatarView.Text);
		Assert.Equal(TextTransform.Lowercase, avatarView.TextTransform);
		Size request = avatarView.Measure(double.PositiveInfinity, double.PositiveInfinity).Request;
		Assert.Equal(10, request.Width);
		Assert.Equal(20, request.Height);
	}

	[Fact]
	public void DefaultBorderColor()
	{
		AvatarView avatarView = new();
		Assert.Equal(Colors.White, avatarView.BorderColor);
	}

	[Fact]
	public void DefaultBorderWidth()
	{
		AvatarView avatarView = new();
		Assert.Equal(1, avatarView.BorderWidth);
	}

	[Fact]
	public void DefaultCornerRadius()
	{
		AvatarView avatarView = new();
		Assert.Equal(new CornerRadius(24, 24, 24, 24), avatarView.CornerRadius);
	}

	[Fact]
	public void DefaultFontSize()
	{
		AvatarView avatarView = new();
		Assert.Equal(14, avatarView.Font.Size);
	}

	[Fact]
	public void DefaultHeightRequest()
	{
		AvatarView avatarView = new();
		Size request = avatarView.Measure(double.PositiveInfinity, double.PositiveInfinity).Request;
		Assert.Equal(48, request.Height);
	}

	[Fact]
	public void DefaultText()
	{
		AvatarView avatarView = new();
		Assert.Equal("?", avatarView.Text);
	}

	[Fact]
	public void DefaultWidthRequest()
	{
		AvatarView avatarView = new();
		Size request = avatarView.Measure(double.PositiveInfinity, double.PositiveInfinity).Request;
		Assert.Equal(48, request.Width);
	}

	[Theory]
	[InlineData(nameof(IFontElement.FontAttributes), FontAttributes.Bold)]
	[InlineData(nameof(IFontElement.FontAutoScalingEnabled), false)]
	[InlineData(nameof(IFontElement.FontFamily), "Arial")]
	[InlineData(nameof(IFontElement.FontSize), 10)]
	public void FontPropertyTriggersFontProperty(string propertyName, object value)
	{
		var handler = new FontElementHandlerStub();

		AvatarView avatarView = new()
		{
			Handler = handler
		};
		handler.Updates.Clear();

		avatarView.GetType().GetProperty(propertyName)?.SetValue(avatarView, value, null);
		Assert.Equal(2, handler.Updates.Count);
		Assert.Equal(new[] { propertyName, nameof(ITextStyle.Font) }, handler.Updates);
	}

	[Fact]
	public void TestBindingContextPropagation()
	{
		object context = new();
		AvatarView avatar = new()
		{
			BindingContext = context
		};
		FileImageSource source = new();
		avatar.ImageSource = source;
		Assert.Equal(context, source.BindingContext);
	}

	[Fact]
	public void TestBorderColorToBlack()
	{
		AvatarView avatarView = new()
		{
			BorderColor = Colors.Red,
		};
		Assert.Equal(Colors.Red, avatarView.BorderColor);

		bool signaled = false;
		avatarView.PropertyChanged += (sender, e) =>
		{
			if (e.PropertyName == "BorderColor")
			{
				signaled = true;
			}
		};

		avatarView.BorderColor = Colors.Black;
		Assert.Equal(Colors.Black, avatarView.BorderColor);
		Assert.True(signaled);
	}

	[Fact]
	public void TestBorderWidthToSeven()
	{
		AvatarView avatarView = new()
		{
			BorderWidth = 2,
		};
		Assert.Equal(2, avatarView.BorderWidth);

		bool signaled = false;
		avatarView.PropertyChanged += (sender, e) =>
		{
			if (e.PropertyName == "BorderWidth")
			{
				signaled = true;
			}
		};

		avatarView.BorderWidth = 7;
		Assert.Equal(7, avatarView.BorderWidth);
		Assert.True(signaled);
	}

	/// <summary>This test is specifically to test the use ofCornerRadius of type Maui.CornerRadius.</summary>
	[Fact]
	public void TestCornerRadiusFourCornerRadiusToOneTwoThreeFour()
	{
		AvatarView avatarView = new()
		{
			CornerRadius = new CornerRadius(3, 4, 5, 6),
		};
		Assert.Equal(new CornerRadius(3, 4, 5, 6), avatarView.CornerRadius);

		bool signaled = false;
		avatarView.PropertyChanged += (sender, e) =>
		{
			if (e.PropertyName == "CornerRadius")
			{
				signaled = true;
			}
		};

		avatarView.CornerRadius = new CornerRadius(1, 2, 3, 4);
		Assert.Equal(new CornerRadius(1, 2, 3, 4), avatarView.CornerRadius);
		Assert.True(signaled);
	}

	/// <summary>This test is specifically to test the use of CornerRadius of type Int.</summary>
	[Fact]
	public void TestCornerRadiusSameRadiusToThree()
	{
		AvatarView avatarView = new()
		{
			CornerRadius = new CornerRadius(7),
		};
		Assert.Equal(new CornerRadius(7), avatarView.CornerRadius);

		bool signaled = false;
		avatarView.PropertyChanged += (sender, e) =>
		{
			if (e.PropertyName == "CornerRadius")
			{
				signaled = true;
			}
		};

		avatarView.CornerRadius = new CornerRadius(3);
		Assert.Equal(new CornerRadius(3), avatarView.CornerRadius);
		Assert.True(signaled);
	}

	[Fact]
	public void TestImageSource()
	{
		AvatarView avatarView = new();
		Assert.Null(avatarView.ImageSource);

		bool signaled = false;
		avatarView.PropertyChanged += (sender, e) =>
		{
			if (e.PropertyName == "ImageSource")
			{
				signaled = true;
			}
		};

		ImageSource source = ImageSource.FromFile("File.png");
		avatarView.ImageSource = source;

		Assert.Equal(source, avatarView.ImageSource);
		Assert.True(signaled);
	}

	[Fact]
	public void TestTextColorToYellow()
	{
		AvatarView avatarView = new()
		{
			TextColor = Microsoft.Maui.Graphics.Colors.Coral,
		};
		Assert.Equal(Microsoft.Maui.Graphics.Colors.Coral, avatarView.TextColor);

		bool signaled = false;
		avatarView.PropertyChanged += (sender, e) =>
		{
			if (e.PropertyName == "TextColor")
			{
				signaled = true;
			}
		};

		avatarView.TextColor = Microsoft.Maui.Graphics.Colors.Yellow;
		Assert.Equal(Microsoft.Maui.Graphics.Colors.Yellow, avatarView.TextColor);
		Assert.True(signaled);
	}

	[Fact]
	public void TestTextSetToZz()
	{
		AvatarView avatarView = new()
		{
			Text = "GL"
		};
		Assert.Equal("GL", avatarView.Text);

		bool signaled = false;
		avatarView.PropertyChanged += (sender, e) =>
		{
			if (e.PropertyName == "Text")
			{
				signaled = true;
			}
		};

		avatarView.Text = "ZZ";
		Assert.Equal("ZZ", avatarView.Text);
		Assert.True(signaled);
	}

	[Fact]
	public void TestTextTranformToLowerCase()
	{
		AvatarView avatarView = new()
		{
			TextTransform = TextTransform.Uppercase,
		};
		Assert.Equal(TextTransform.Uppercase, avatarView.TextTransform);

		bool signaled = false;
		avatarView.PropertyChanged += (sender, e) =>
		{
			if (e.PropertyName == "TextTransform")
			{
				signaled = true;
			}
		};

		avatarView.TextTransform = TextTransform.Lowercase;
		Assert.Equal(TextTransform.Lowercase, avatarView.TextTransform);
		Assert.True(signaled);
	}
}