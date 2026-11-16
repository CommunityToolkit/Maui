using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Microsoft.Maui.Controls.Shapes;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class AvatarViewTests : BaseViewTest
{
	static readonly BindableProperty[] propertiesWithAvatarViewDefaults =
	[
		VisualElement.WidthRequestProperty,
		VisualElement.HeightRequestProperty,
		Border.PaddingProperty,
		Border.StrokeProperty,
		Border.StrokeThicknessProperty,
		Border.StrokeShapeProperty,
		View.HorizontalOptionsProperty,
		View.VerticalOptionsProperty,
	];

	public AvatarViewTests()
	{
		Assert.IsType<IAvatarView>(new Maui.Views.AvatarView(), exactMatch: false);
	}

	[Fact]
	public void MultipleAvatarViews_BindablePropertyAttributedProperties_WithPartialPropertyInializers_UseCorrectDefaultValue()
	{
		// Arrange
		var avatarView1 = new Maui.Views.AvatarView();
		var avatarView2 = new Maui.Views.AvatarView();

		// Act Assert
		Assert.Equal(AvatarViewDefaults.BorderColor, avatarView1.BorderColor);
		Assert.Equal(AvatarViewDefaults.BorderWidth, avatarView1.BorderWidth);
		Assert.Equal(AvatarViewDefaults.CornerRadius, avatarView1.CornerRadius);
		Assert.Equal(AvatarViewDefaults.Text, avatarView1.Text);

		Assert.Equal(AvatarViewDefaults.BorderColor, avatarView2.BorderColor);
		Assert.Equal(AvatarViewDefaults.BorderWidth, avatarView2.BorderWidth);
		Assert.Equal(AvatarViewDefaults.CornerRadius, avatarView2.CornerRadius);
		Assert.Equal(AvatarViewDefaults.Text, avatarView2.Text);
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

		CreateViewHandler<MockAvatarViewHandler>(avatarView);

		avatarView.BorderColor.Should().Be(Colors.Beige);
		avatarView.BorderWidth.Should().Be(2);
		avatarView.CornerRadius.Should().Be(new CornerRadius(4, 8, 12, 16));
		avatarView.TextColor.Should().Be(Colors.Pink);
		avatarView.Text.Should().Be("GL");
		avatarView.TextTransform.Should().Be(TextTransform.Lowercase);
		Size request = avatarView.Measure(double.PositiveInfinity, double.PositiveInfinity);
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
		avatarView.BorderColor.Should().Be(AvatarViewDefaults.BorderColor);
	}

	[Fact]
	public void DefaultBorderWidth()
	{
		var avatarView = new Maui.Views.AvatarView();
		avatarView.BorderWidth.Should().Be(AvatarViewDefaults.BorderWidth);
	}

	[Fact]
	public void DefaultCornerRadius()
	{
		var avatarView = new Maui.Views.AvatarView();
		avatarView.CornerRadius.Should().Be(new CornerRadius(AvatarViewDefaults.CornerRadius.TopLeft, AvatarViewDefaults.CornerRadius.TopRight, AvatarViewDefaults.CornerRadius.BottomLeft, AvatarViewDefaults.CornerRadius.BottomRight));
	}

	[Fact]
	public void DefaultFontSize()
	{
		var avatarView = new Maui.Views.AvatarView();
		avatarView.FontSize.Should().Be(-1);
	}

	[Fact]
	public void DefaultHeightRequest()
	{
		var avatarView = new Maui.Views.AvatarView();
		CreateViewHandler<MockAvatarViewHandler>(avatarView);

		Size request = avatarView.Measure(double.PositiveInfinity, double.PositiveInfinity);
		request.Height.Should().Be(AvatarViewDefaults.HeightRequest);
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
			avatarLabel.Text.Should().Be(AvatarViewDefaults.Text);
		}
	}

	[Fact]
	public void DefaultProperties()
	{
		var avatarView = new Maui.Views.AvatarView();
		avatarView.IsEnabled.Should().BeTrue();
		avatarView.HorizontalOptions.Should().Be(LayoutOptions.Center);
		avatarView.VerticalOptions.Should().Be(LayoutOptions.Center);
		avatarView.HeightRequest.Should().Be(AvatarViewDefaults.HeightRequest);
		avatarView.WidthRequest.Should().Be(AvatarViewDefaults.WidthRequest);
		avatarView.Padding.Should().Be(AvatarViewDefaults.Padding);
		avatarView.Stroke.Should().Be((SolidColorBrush)AvatarViewDefaults.BorderColor);
		avatarView.StrokeThickness.Should().Be(AvatarViewDefaults.BorderWidth);
		avatarView.StrokeShape.Should().BeOfType<RoundRectangle>();
	}

	[Fact]
	public void DefaultWidthRequest()
	{
		var avatarView = new Maui.Views.AvatarView();
		CreateViewHandler<MockAvatarViewHandler>(avatarView);

		Size request = avatarView.Measure(double.PositiveInfinity, double.PositiveInfinity);
		request.Width.Should().Be(AvatarViewDefaults.WidthRequest);
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

	[Fact]
	public void EnsureDefaults()
	{
		var avatarView = new Maui.Views.AvatarView();
		Assert.Equal(AvatarViewDefaults.BorderColor, avatarView.BorderColor);
		Assert.Equal(AvatarViewDefaults.BorderWidth, avatarView.BorderWidth);
		Assert.Equal(AvatarViewDefaults.CornerRadius, avatarView.CornerRadius);
		Assert.Equal(AvatarViewDefaults.Text, avatarView.Text);
		Assert.Equal(AvatarViewDefaults.Padding, avatarView.Padding);
		Assert.Equal(AvatarViewDefaults.HeightRequest, avatarView.HeightRequest);
		Assert.Equal(AvatarViewDefaults.WidthRequest, avatarView.WidthRequest);
	}

	[Fact]
	public void ExplicitStyleReplacesTheAvatarViewDefaults()
	{
		var strokeShape = new Rectangle();
		var avatarView = new Maui.Views.AvatarView
		{
			Style = CreateStyleForTheAvatarViewDefaults(strokeShape),
		};

		AssertStyleValues(avatarView, strokeShape);
	}

	[Fact]
	public void ImplicitStyleReplacesTheAvatarViewDefaults()
	{
		var strokeShape = new Rectangle();
		var layout = new VerticalStackLayout();
		layout.Resources.Add(CreateStyleForTheAvatarViewDefaults(strokeShape));
		var avatarView = new Maui.Views.AvatarView();

		layout.Children.Add(avatarView);

		AssertStyleValues(avatarView, strokeShape);
	}

	[Fact]
	public void StyleSizesTheAvatarView()
	{
		var avatarView = new Maui.Views.AvatarView
		{
			Style = new Style(typeof(Maui.Views.AvatarView))
			{
				Setters =
				{
					new Setter { Property = VisualElement.WidthRequestProperty, Value = 64d },
					new Setter { Property = VisualElement.HeightRequestProperty, Value = 64d },
				},
			},
		};
		CreateViewHandler<MockAvatarViewHandler>(avatarView);

		Size request = avatarView.Measure(double.PositiveInfinity, double.PositiveInfinity);

		request.Width.Should().Be(64);
		request.Height.Should().Be(64);
	}

	[Fact]
	public void AvatarViewDefaultsComeBackWhenTheStyleIsRemoved()
	{
		var avatarView = new Maui.Views.AvatarView
		{
			Style = CreateStyleForTheAvatarViewDefaults(new Rectangle()),
		};

		avatarView.Style = null;

		AssertAvatarViewDefaults(avatarView);
	}

	[Fact]
	public void AvatarViewDefaultsComeBackWhenTheLocalValuesAreCleared()
	{
		var avatarView = new Maui.Views.AvatarView
		{
			WidthRequest = 30,
			HeightRequest = 31,
			Padding = new Thickness(4),
			Stroke = Colors.Red,
			StrokeThickness = 5,
			StrokeShape = new Ellipse(),
			HorizontalOptions = LayoutOptions.Start,
			VerticalOptions = LayoutOptions.End,
		};

		foreach (var property in propertiesWithAvatarViewDefaults)
		{
			avatarView.ClearValue(property);
		}

		AssertAvatarViewDefaults(avatarView);
	}

	[Fact]
	public void ClearingTheAvatarViewDefaultsKeepsThem()
	{
		var avatarView = new Maui.Views.AvatarView();

		foreach (var property in propertiesWithAvatarViewDefaults)
		{
			avatarView.ClearValue(property);
		}

		AssertAvatarViewDefaults(avatarView);
	}

	[Fact]
	public void AvatarViewDefaultsComeBackEachTimeAVisualStateNoLongerApplies()
	{
		var avatarView = new Maui.Views.AvatarView();
		var pointerOverShape = new Ellipse();
		var pointerOver = new VisualState { Name = "PointerOver" };
		pointerOver.Setters.Add(new Setter { Property = VisualElement.WidthRequestProperty, Value = 56d });
		pointerOver.Setters.Add(new Setter { Property = Border.StrokeProperty, Value = new SolidColorBrush(Colors.Red) });
		pointerOver.Setters.Add(new Setter { Property = Border.StrokeShapeProperty, Value = pointerOverShape });
		var commonStates = new VisualStateGroup { Name = "CommonStates" };
		commonStates.States.Add(new VisualState { Name = "Normal" });
		commonStates.States.Add(pointerOver);
		VisualStateManager.SetVisualStateGroups(avatarView, new VisualStateGroupList { commonStates });

		for (var i = 0; i < 3; i++)
		{
			VisualStateManager.GoToState(avatarView, "PointerOver").Should().BeTrue();
			avatarView.WidthRequest.Should().Be(56);
			avatarView.Stroke.Should().Be(new SolidColorBrush(Colors.Red));
			avatarView.StrokeShape.Should().BeSameAs(pointerOverShape);
			AssertTheStrokeShapeIsTheOnlyShapeAmongTheLogicalChildren(avatarView);

			VisualStateManager.GoToState(avatarView, "Normal").Should().BeTrue();
			AssertAvatarViewDefaults(avatarView);
		}
	}

	static Style CreateStyleForTheAvatarViewDefaults(Shape strokeShape) => new(typeof(Maui.Views.AvatarView))
	{
		Setters =
		{
			new Setter { Property = VisualElement.WidthRequestProperty, Value = 64d },
			new Setter { Property = VisualElement.HeightRequestProperty, Value = 65d },
			new Setter { Property = Border.PaddingProperty, Value = new Thickness(8) },
			new Setter { Property = Border.StrokeProperty, Value = new SolidColorBrush(Colors.Red) },
			new Setter { Property = Border.StrokeThicknessProperty, Value = 7d },
			new Setter { Property = Border.StrokeShapeProperty, Value = strokeShape },
			new Setter { Property = View.HorizontalOptionsProperty, Value = LayoutOptions.Start },
			new Setter { Property = View.VerticalOptionsProperty, Value = LayoutOptions.End },
			new Setter { Property = VisualElement.IsEnabledProperty, Value = false },
		},
	};

	static void AssertStyleValues(Maui.Views.AvatarView avatarView, Shape strokeShape)
	{
		avatarView.WidthRequest.Should().Be(64);
		avatarView.HeightRequest.Should().Be(65);
		avatarView.Padding.Should().Be(new Thickness(8));
		avatarView.Stroke.Should().Be(new SolidColorBrush(Colors.Red));
		avatarView.StrokeThickness.Should().Be(7);
		avatarView.StrokeShape.Should().BeSameAs(strokeShape);
		avatarView.HorizontalOptions.Should().Be(LayoutOptions.Start);
		avatarView.VerticalOptions.Should().Be(LayoutOptions.End);
		avatarView.IsEnabled.Should().BeFalse();
		AssertTheStrokeShapeIsTheOnlyShapeAmongTheLogicalChildren(avatarView);
	}

	static void AssertAvatarViewDefaults(Maui.Views.AvatarView avatarView)
	{
		avatarView.WidthRequest.Should().Be(AvatarViewDefaults.WidthRequest);
		avatarView.HeightRequest.Should().Be(AvatarViewDefaults.HeightRequest);
		avatarView.Padding.Should().Be(AvatarViewDefaults.Padding);
		avatarView.Stroke.Should().Be((SolidColorBrush)AvatarViewDefaults.BorderColor);
		avatarView.StrokeThickness.Should().Be(AvatarViewDefaults.BorderWidth);
		avatarView.StrokeShape.Should().BeOfType<RoundRectangle>().Which.CornerRadius.Should().Be(AvatarViewDefaults.CornerRadius);
		avatarView.HorizontalOptions.Should().Be(LayoutOptions.Center);
		avatarView.VerticalOptions.Should().Be(LayoutOptions.Center);
		AssertTheStrokeShapeIsTheOnlyShapeAmongTheLogicalChildren(avatarView);
	}

	static void AssertTheStrokeShapeIsTheOnlyShapeAmongTheLogicalChildren(Maui.Views.AvatarView avatarView)
	{
		var shapes = ((IElementController)avatarView).LogicalChildren.OfType<Shape>().ToList();

		shapes.Should().ContainSingle().Which.Should().BeSameAs(avatarView.StrokeShape);
		shapes[0].Parent.Should().BeSameAs(avatarView);
	}
}