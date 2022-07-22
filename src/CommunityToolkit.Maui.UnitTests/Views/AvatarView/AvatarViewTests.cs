using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Internals;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views.AvatarView;

public class AvatarViewTests : BaseHandlerTest
{
	public AvatarViewTests()
	{
		Assert.IsAssignableFrom<IAvatarView>(new Maui.Views.AvatarView());
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
		var avatarView = new Maui.Views.AvatarView();
		Assert.Equal(Colors.White, avatarView.BorderColor);
	}

	[Fact]
	public void DefaultBorderWidth()
	{
		var avatarView = new Maui.Views.AvatarView();
		Assert.Equal(1, avatarView.BorderWidth);
	}

	[Fact]
	public void DefaultCornerRadius()
	{
		var avatarView = new Maui.Views.AvatarView();
		Assert.Equal(new CornerRadius(24, 24, 24, 24), avatarView.CornerRadius);
	}

	[Fact]
	public void DefaultFontSize()
	{
		var avatarView = new Maui.Views.AvatarView();
		Assert.Equal(0, avatarView.FontSize);
	}

	[Fact]
	public void DefaultHeightRequest()
	{
		var avatarView = new Maui.Views.AvatarView();
		Size request = avatarView.Measure(double.PositiveInfinity, double.PositiveInfinity).Request;
		Assert.Equal(48, request.Height);
	}

	[Fact]
	public void DefaultText()
	{
		var avatarView = new Maui.Views.AvatarView();
		Assert.Equal("?", avatarView.Text);
	}

	[Fact]
	public void DefaultWidthRequest()
	{
		var avatarView = new Maui.Views.AvatarView();
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

		var avatarView = new Maui.Views.AvatarView()
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
		Maui.Views.AvatarView avatar = new()
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
		var avatarView = new Maui.Views.AvatarView()
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
		var avatarView = new Maui.Views.AvatarView()
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
		var avatarView = new Maui.Views.AvatarView()
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
		var avatarView = new Maui.Views.AvatarView()
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
		var avatarView = new Maui.Views.AvatarView();
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
		var avatarView = new Maui.Views.AvatarView()
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
		var avatarView = new Maui.Views.AvatarView()
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
		var avatarView = new Maui.Views.AvatarView()
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