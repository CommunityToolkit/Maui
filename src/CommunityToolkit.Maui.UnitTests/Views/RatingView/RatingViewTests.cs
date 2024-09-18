// Ignore Spelling: color, colors

using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using FluentAssertions;
using Microsoft.Maui.Controls.Shapes;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class RatingViewTests : BaseHandlerTest
{
	[Fact]
	public void Defaults_BindingContext()
	{
		RetingViewModel vm = new();
		RatingView ratingViewWithBinding = new();
		_ = ratingViewWithBinding.BindingContext.Should().BeNull();
		ratingViewWithBinding.BindingContext = vm;
		_ = ratingViewWithBinding.BindingContext.Should().Be(vm);
	}

	[Fact]
	public void Defaults_ItemDefaultsApplied()
	{
		RatingView ratingView = new();
		Border firstItem = (Border)ratingView.Control!.Children[0];

		_ = firstItem.Should().BeOfType<Border>();
		_ = firstItem.BackgroundColor.Should().Be(Colors.Transparent);
		_ = firstItem.Margin.Should().Be(Thickness.Zero);
		_ = firstItem.Padding.Should().Be(RatingViewDefaults.ItemPadding);
		_ = firstItem.Stroke.Should().Be(new SolidColorBrush(Colors.Transparent));
		_ = firstItem.StrokeThickness.Should().Be(0);
		_ = firstItem.Style.Should().BeNull();
	}

	[Fact]
	public void Defaults_ShapeDefaultsApplied()
	{
		RatingView ratingView = new();
		Microsoft.Maui.Controls.Shapes.Path firstItemShape = (Microsoft.Maui.Controls.Shapes.Path)((Border)ratingView.Control!.Children[0]).Content!.GetVisualTreeDescendants()[0];

		Assert.NotNull(firstItemShape);
		_ = Assert.IsType<Microsoft.Maui.Controls.Shapes.Path>(firstItemShape);
		Assert.Equal(Stretch.Uniform, firstItemShape.Aspect);
		Assert.Equal(RatingViewDefaults.ItemShapeSize, firstItemShape.HeightRequest);
		Assert.Equal(RatingViewDefaults.ShapeBorderColor, firstItemShape.Stroke);
		Assert.Equal(PenLineCap.Round, firstItemShape.StrokeLineCap);
		Assert.Equal(PenLineJoin.Round, firstItemShape.StrokeLineJoin);
		Assert.Equal(RatingViewDefaults.ShapeBorderThickness, firstItemShape.StrokeThickness);
		Assert.Equal(RatingViewDefaults.ItemShapeSize, firstItemShape.WidthRequest);
	}

	[Fact]
	public void Defaults_ShouldHaveCorrectDefaultProperties()
	{
		RatingView ratingView = new();

		Assert.Equal(RatingViewDefaults.DefaultRating, ratingView.Rating);
		Assert.Equal(RatingViewDefaults.EmptyBackgroundColor, ratingView.EmptyBackgroundColor);
		Assert.Equal(RatingViewDefaults.FilledBackgroundColor, ratingView.FilledBackgroundColor);
		Assert.Equal(RatingViewDefaults.IsEnabled, ratingView.IsEnabled);
		Assert.Equal(RatingViewDefaults.ItemPadding, ratingView.ItemPadding);
		Assert.Equal(RatingViewDefaults.ItemShapeSize, ratingView.ItemShapeSize);
		Assert.Equal(RatingViewDefaults.MaximumRating, ratingView.MaximumRating);
		Assert.Equal(RatingViewDefaults.Shape, ratingView.Shape);
		Assert.Equal(RatingViewDefaults.ShapeBorderColor, ratingView.ShapeBorderColor);
		Assert.Equal(RatingViewDefaults.ShapeBorderThickness, ratingView.ShapeBorderThickness);
		Assert.Equal(RatingViewDefaults.Spacing, ratingView.Spacing);
		Assert.Equal(RatingFillElement.Shape, ratingView.RatingFill);
		Assert.Null(ratingView.CustomShape);
	}

	[Fact]
	public void Events_Border_TapGestureRecognizer_SingleRating_Toggled()
	{
		RatingView ratingView = new()
		{
			MaximumRating = 1
		};
		Border child = (Border)ratingView.Control!.Children[0];
		TapGestureRecognizer tgr = (TapGestureRecognizer)child.GestureRecognizers[0];
		tgr.SendTapped(child);
		_ = ratingView.Rating.Should().Be(1);
		tgr.SendTapped(child);
		_ = ratingView.Rating.Should().Be(0);
	}

	[Fact]
	public void Events_Border_TapGestureRecognizer_Tapped()
	{
		int handlerTappedCount = 0;
		RatingView ratingView = new();
		_ = ratingView.Rating.Should().Be(handlerTappedCount);
		Border child = (Border)ratingView.Control!.Children[0];
		TapGestureRecognizer tgr = (TapGestureRecognizer)child.GestureRecognizers[0];
		tgr.SendTapped(child);
		handlerTappedCount++;
		_ = ratingView.Rating.Should().Be(handlerTappedCount);
	}

	[Fact]
	public void Events_RatingChanged_ShouldBeRaised_MaximumRatingPropertyChanged_LEnabled()
	{
		const double currentRating = 5;
		const double maximumRating = 4;
		RatingView ratingView = new()
		{
			Rating = currentRating
		};

		bool signaled = false;
		ratingView.RatingChanged += (sender, e) => signaled = true;
		ratingView.MaximumRating = 4;
		_ = ratingView.Rating.Should().Be(maximumRating);
		_ = signaled.Should().BeTrue();
	}

	[Fact]
	public void Events_RatingChanged_ShouldBeRaised_RatingPropertyChanged_Enabled()
	{
		const double currentRating = 3.5;
		RatingView ratingView = new();
		_ = ratingView.Rating.Should().Be(RatingViewDefaults.DefaultRating);
		bool signaled = false;
		ratingView.RatingChanged += (sender, e) => signaled = true;
		ratingView.Rating = currentRating;
		_ = ratingView.Rating.Should().Be(currentRating);
		_ = signaled.Should().BeTrue();
	}

	[Fact]
	public void Events_RatingChanged_ShouldNotBeRaised_MaximumRatingPropertyChanged_HEnabled()
	{
		const double currentRating = 5;
		const int maximumRating = 7;
		RatingView ratingView = new()
		{
			Rating = currentRating
		};

		bool signaled = false;
		ratingView.RatingChanged += (sender, e) => signaled = true;
		ratingView.MaximumRating = maximumRating;
		_ = ratingView.Rating.Should().Be(currentRating);
		_ = signaled.Should().BeFalse();
	}

	[Fact]
	public void Events_RatingChanged_ShouldNotBeRaised_MaximumRatingPropertyChanged_HNotEnabled()
	{
		const double currentRating = 5;
		const int maximumRating = 7;
		RatingView ratingView = new()
		{
			Rating = currentRating,
			IsEnabled = false,
		};

		bool signaled = false;
		ratingView.RatingChanged += (sender, e) => signaled = true;
		ratingView.MaximumRating = maximumRating;
		_ = ratingView.Rating.Should().Be(currentRating);
		_ = signaled.Should().BeFalse();
	}

	[Fact]
	public void Events_RatingChanged_ShouldNotBeRaised_MaximumRatingPropertyChanged_LNotEnabled()
	{
		const double currentRating = 5;
		const int maximumRating = 4;
		RatingView ratingView = new()
		{
			Rating = currentRating,
			IsEnabled = false,
		};

		bool signaled = false;
		ratingView.RatingChanged += (sender, e) => signaled = true;
		ratingView.MaximumRating = maximumRating;
		_ = ratingView.Rating.Should().Be(maximumRating);
		_ = signaled.Should().BeFalse();
	}

	[Fact]
	public void Events_RatingChanged_ShouldNotBeRaised_RatingPropertyChanged_NotEnabled()
	{
		const double currentRating = 3.5;
		RatingView ratingView = new()
		{
			IsEnabled = false
		};

		bool signaled = false;
		ratingView.RatingChanged += (sender, e) => signaled = true;
		ratingView.Rating = currentRating;
		_ = ratingView.Rating.Should().Be(currentRating);
		_ = signaled.Should().BeFalse();
	}

	[Fact]
	public void Events_ShouldBeRaised_MaximumRatingChanged_BelowRating()
	{
		List<RatingChangedEventArgs> receivedEvents = [];
		const double expectedRating = 2;
		RatingView ratingView = new()
		{
			MaximumRating = 3,
			Rating = 3
		};
		ratingView.RatingChanged += (sender, e) => receivedEvents.Add(e);
		ratingView.MaximumRating = (byte)expectedRating;
		_ = receivedEvents.Should().HaveCount(1);
		_ = receivedEvents[0].Rating.Should().Be(expectedRating);
	}

	[Fact]
	public void Events_ShouldBeRaised_RatingChangedEvent()
	{
		List<RatingChangedEventArgs> receivedEvents = [];
		const double expectedRating = 2.0;
		RatingView ratingView = new();
		ratingView.RatingChanged += (sender, e) => receivedEvents.Add(e);
		ratingView.Rating = expectedRating;
		_ = Assert.Single(receivedEvents);
		_ = receivedEvents[0].Rating.Should().Be(expectedRating);
	}

	[Fact]
	public void Events_ShouldNotBeRaised_MaximumRatingChanged_AboveRating()
	{
		List<RatingChangedEventArgs> receivedEvents = [];
		RatingView ratingView = new()
		{
			MaximumRating = 3,
			Rating = 3
		};
		ratingView.RatingChanged += (sender, e) => receivedEvents.Add(e);
		ratingView.MaximumRating = 4;
		_ = receivedEvents.Should().HaveCount(0);
	}

	[Fact]
	public void Properties_Change_CustomShape()
	{
		const string customShape = "M 12 0C5.388 0 0 5.388 0 12s5.388 12 12 12 12-5.38 12-12c0-6.612-5.38-12-12-12z";
		RatingView ratingView = new();
		_ = ratingView.CustomShape.Should().BeNullOrEmpty();
		ratingView.CustomShape = customShape;
		_ = ratingView.CustomShape.Should().Be(customShape);
	}

	[Theory]
	[InlineData("")]
	[InlineData(null)]
	public void Properties_Change_CustomShape_Null(string? customShapes)
	{
		const string customShape = "M 12 0C5.388 0 0 5.388 0 12s5.388 12 12 12 12-5.38 12-12c0-6.612-5.38-12-12-12z";
		RatingView ratingView = new()
		{
			Shape = RatingViewShape.Custom,
			CustomShape = customShape,
		};
		_ = ratingView.Shape.Should().Be(RatingViewShape.Custom);
		_ = ratingView.CustomShape.Should().Be(customShape);
		ratingView.CustomShape = customShapes!;
		_ = ratingView.Shape.Should().Be(RatingViewShape.Star);
	}

	[Fact]
	public void Properties_Change_CustomShape_ShapeCustom()
	{
		const string customShape = "M 12 0C5.388 0 0 5.388 0 12s5.388 12 12 12 12-5.38 12-12c0-6.612-5.38-12-12-12z";
		RatingView ratingView = new()
		{
			Shape = RatingViewShape.Custom
		};
		_ = ratingView.Shape.Should().Be(RatingViewShape.Custom);
		ratingView.CustomShape = customShape;
		_ = ratingView.CustomShape.Should().Be(customShape);
		_ = ratingView.Shape.Should().Be(RatingViewShape.Custom);
	}

	[Fact]
	public void Properties_Change_CustomShape_ShapeNotCustom()
	{
		const string customShape = "M 12 0C5.388 0 0 5.388 0 12s5.388 12 12 12 12-5.38 12-12c0-6.612-5.38-12-12-12z";
		RatingView ratingView = new()
		{
			Shape = RatingViewShape.Heart
		};
		_ = ratingView.Shape.Should().Be(RatingViewShape.Heart);
		ratingView.CustomShape = customShape;
		_ = ratingView.CustomShape.Should().Be(customShape);
		_ = ratingView.Shape.Should().Be(RatingViewShape.Heart);
	}

	[Fact]
	public void Properties_Change_EmptyBackgroundColor_Item()
	{
		const double rating = 1.5;
		const byte maximumRating = 7;
		Color emptyBackgroundColor = Colors.Snow;
		RatingView ratingView = new()
		{
			MaximumRating = maximumRating,
			Rating = rating,
			RatingFill = RatingFillElement.Item
		};
		_ = ratingView.EmptyBackgroundColor.Should().NotBe(emptyBackgroundColor);
		ratingView.EmptyBackgroundColor = emptyBackgroundColor;
		_ = ratingView.EmptyBackgroundColor.Should().Be(emptyBackgroundColor);
		Microsoft.Maui.Controls.Shapes.Path emptyRatingItem = (Microsoft.Maui.Controls.Shapes.Path)((Border)ratingView.Control!.Children[maximumRating - 1]).Content!.GetVisualTreeDescendants()[0];
		_ = emptyRatingItem.Fill.Should().Be(new SolidColorBrush(emptyBackgroundColor));
	}

	[Fact]
	public void Properties_Change_EmptyBackgroundColor_Shape()
	{
		const double rating = 1.5;
		const byte maximumRating = 7;
		Color emptyBackgroundColor = Colors.Snow;
		RatingView ratingView = new()
		{
			MaximumRating = maximumRating,
			Rating = rating
		};
		_ = ratingView.EmptyBackgroundColor.Should().NotBe(emptyBackgroundColor);
		ratingView.EmptyBackgroundColor = emptyBackgroundColor;
		_ = ratingView.EmptyBackgroundColor.Should().Be(emptyBackgroundColor);
		Microsoft.Maui.Controls.Shapes.Path emptyRatingItem = (Microsoft.Maui.Controls.Shapes.Path)((Border)ratingView.Control!.Children[maximumRating - 1]).Content!.GetVisualTreeDescendants()[0];
		_ = emptyRatingItem.Fill.Should().Be(new SolidColorBrush(emptyBackgroundColor));
	}

	[Fact]
	public void Properties_Change_FilledBackgroundColor_Item()
	{
		const double rating = 1.5;
		const byte maximumRating = 7;
		Color filledBackgroundColor = Colors.Snow;
		Color emptyBackgroundColor = Colors.Firebrick;
		Color backgroundColor = Colors.DarkGreen;
		RatingView ratingView = new()
		{
			MaximumRating = maximumRating,
			Rating = rating,
			RatingFill = RatingFillElement.Item
		};
		_ = ratingView.FilledBackgroundColor.Should().NotBe(filledBackgroundColor);
		_ = ratingView.BackgroundColor.Should().BeNull();
		ratingView.BackgroundColor = backgroundColor;
		ratingView.EmptyBackgroundColor = emptyBackgroundColor;
		ratingView.FilledBackgroundColor = filledBackgroundColor;
		_ = ratingView.FilledBackgroundColor.Should().Be(filledBackgroundColor);
		Microsoft.Maui.Controls.Shapes.Path filledRatingShape = (Microsoft.Maui.Controls.Shapes.Path)((Border)ratingView.Control!.Children[(int)Math.Floor(rating)]).Content!.GetVisualTreeDescendants()[0];
		_ = filledRatingShape.Fill.Should().Be(new SolidColorBrush(emptyBackgroundColor));
		Border filledRatingItem = (Border)ratingView.Control!.Children[(int)Math.Floor(rating)];
		_ = filledRatingItem.BackgroundColor.Should().Be(Colors.Transparent);
		filledRatingItem = (Border)ratingView.Control!.Children[maximumRating - 1]; // Check the last one, as this is where we expect the background colour to be set
		_ = filledRatingItem.BackgroundColor.Should().Be(backgroundColor);
	}

	[Fact]
	public void Properties_Change_FilledBackgroundColor_Shape()
	{
		const double rating = 1.5;
		const byte maximumRating = 7;
		Color filledBackgroundColor = Colors.Snow;
		RatingView ratingView = new()
		{
			MaximumRating = maximumRating,
			Rating = rating
		};
		_ = ratingView.FilledBackgroundColor.Should().NotBe(filledBackgroundColor);
		ratingView.FilledBackgroundColor = filledBackgroundColor;
		_ = ratingView.FilledBackgroundColor.Should().Be(filledBackgroundColor);
		Microsoft.Maui.Controls.Shapes.Path filledRatingItem = (Microsoft.Maui.Controls.Shapes.Path)((Border)ratingView.Control!.Children[0]).Content!.GetVisualTreeDescendants()[0];
		_ = filledRatingItem.Fill.Should().Be(new SolidColorBrush(filledBackgroundColor));
	}

	[Fact]
	public void Properties_Change_IsEnabled()
	{
		RatingView ratingView = new();
		_ = ratingView.IsEnabled.Should().BeTrue();
		ratingView.IsEnabled = false;
		_ = ratingView.IsEnabled.Should().BeFalse();
	}

	[Fact]
	public void Properties_Change_IsEnabled_GestureRecognizers()
	{
		RatingView ratingView = new();
		_ = ratingView.IsEnabled.Should().BeTrue();
		for (int i = 0; i < ratingView.Control?.Children.Count; i++)
		{
			Border child = (Border)ratingView.Control.Children[i];
			_ = child.GestureRecognizers.Count.Should().Be(1);
		}
		ratingView.IsEnabled = false;
		_ = ratingView.IsEnabled.Should().BeFalse();
		for (int i = 0; i < ratingView.Control?.Children.Count; i++)
		{
			Border child = (Border)ratingView.Control.Children[i];
			_ = child.GestureRecognizers.Count.Should().Be(0);
		}
	}

	[Fact]
	public void Properties_Change_ItemPadding()
	{
		Thickness itemPadding = new(1, 2, 3, 4);
		RatingView ratingView = new();
		_ = ratingView.ItemPadding.Should().NotBe(itemPadding);
		ratingView.ItemPadding = itemPadding;
		_ = ratingView.ItemPadding.Should().Be(itemPadding);
		Border firstItem = (Border)ratingView.Control!.Children[0];
		_ = firstItem.Padding.Should().Be(itemPadding);
	}

	[Fact]
	public void Properties_Change_MaximumRating()
	{
		const byte maximumRating = 7;
		RatingView ratingView = new();
		_ = ratingView.MaximumRating.Should().NotBe(maximumRating);
		ratingView.MaximumRating = maximumRating;
		_ = ratingView.MaximumRating.Should().Be(maximumRating);
		_ = ratingView.Control!.Children.Should().HaveCount(maximumRating);
	}

	[Fact]
	public void Properties_Change_Rating()
	{
		const double rating = 7.3;
		RatingView ratingView = new();
		_ = ratingView.Rating.Should().NotBe(rating);
		ratingView.Rating = rating;
		_ = ratingView.Rating.Should().Be(rating);
	}

	[Fact]
	public void Properties_Change_RatingFill()
	{
		const RatingFillElement ratingFill = RatingFillElement.Item;
		RatingView ratingView = new();
		_ = ratingView.RatingFill.Should().NotBe(ratingFill);
		ratingView.RatingFill = ratingFill;
		_ = ratingView.RatingFill.Should().Be(ratingFill);
	}

	[Theory]
	[InlineData(RatingViewShape.Heart)]
	[InlineData(RatingViewShape.Circle)]
	[InlineData(RatingViewShape.Like)]
	[InlineData(RatingViewShape.Dislike)]
	[InlineData(RatingViewShape.Custom)]
	public void Properties_Change_Shape(RatingViewShape expectedShape)
	{
		RatingView ratingView = new();
		_ = ratingView.Shape.Should().NotBe(expectedShape);
		ratingView.Shape = expectedShape;
		_ = ratingView.Shape.Should().Be(expectedShape);
	}

	[Fact]
	public void Properties_Change_ShapeBorderColor()
	{
		Color shapeBorderColor = Colors.Snow;
		Brush brush = new SolidColorBrush(shapeBorderColor);
		const double rating = 1.5;
		const byte maximumRating = 7;
		RatingView ratingView = new()
		{
			MaximumRating = maximumRating,
			Rating = rating
		};
		_ = ratingView.ShapeBorderColor.Should().NotBe(shapeBorderColor);
		ratingView.ShapeBorderColor = shapeBorderColor;
		_ = ratingView.ShapeBorderColor.Should().Be(shapeBorderColor);
		Microsoft.Maui.Controls.Shapes.Path firstFilledRatingItem = (Microsoft.Maui.Controls.Shapes.Path)((Border)ratingView.Control!.Children[0]).Content!.GetVisualTreeDescendants()[0];
		_ = firstFilledRatingItem.Stroke.Should().Be(brush);
	}

	[Fact]
	public void Properties_Change_ShapeBorderThickness()
	{
		const double shapeBorderThickness = 7.3;
		RatingView ratingView = new();
		_ = ratingView.ShapeBorderThickness.Should().NotBe(shapeBorderThickness);
		ratingView.ShapeBorderThickness = shapeBorderThickness;
		_ = ratingView.ShapeBorderThickness.Should().Be(shapeBorderThickness);
		Microsoft.Maui.Controls.Shapes.Path firstRatingItem = (Microsoft.Maui.Controls.Shapes.Path)((Border)ratingView.Control!.Children[0]).Content!.GetVisualTreeDescendants()[0];
		_ = firstRatingItem.StrokeThickness.Should().Be(shapeBorderThickness);
	}

	[Fact]
	public void Properties_Change_Size()
	{
		const int itemShapeSize = 73;
		RatingView ratingView = new();
		_ = ratingView.ItemShapeSize.Should().NotBe(itemShapeSize);
		ratingView.ItemShapeSize = itemShapeSize;
		_ = ratingView.ItemShapeSize.Should().Be(itemShapeSize);
		Microsoft.Maui.Controls.Shapes.Path firstRatingItem = (Microsoft.Maui.Controls.Shapes.Path)((Border)ratingView.Control!.Children[0]).Content!.GetVisualTreeDescendants()[0];
		_ = firstRatingItem.WidthRequest.Should().Be(itemShapeSize);
		_ = firstRatingItem.HeightRequest.Should().Be(itemShapeSize);
	}

	[Fact]
	public void Properties_Change_Spacing()
	{
		const int spacing = 73;
		RatingView ratingView = new();
		_ = ratingView.Spacing.Should().NotBe(spacing);
		ratingView.Spacing = spacing;
		_ = ratingView.Spacing.Should().Be(spacing);
		HorizontalStackLayout? control = ratingView.Control;
		_ = control.Should().NotBeNull();
		_ = control!.Spacing.Should().Be(spacing);
	}

	[Fact]
	public void Properties_MaximumRating_KeptInRange()
	{
		RatingView ratingView = new();
		const byte minMaximumRating = 0;
		const byte maxMaximumRating = RatingViewDefaults.MaximumRatings + 1;
		ratingView.MaximumRating = minMaximumRating;
		_ = ratingView.MaximumRating.Should().Be(1);
		ratingView.MaximumRating = maxMaximumRating;
		_ = ratingView.MaximumRating.Should().Be(RatingViewDefaults.MaximumRatings);
	}

	[Fact]
	public void Properties_MaximumRating_NumberOfChildrenHigher()
	{
		const byte minMaximumRating = 7;
		RatingView ratingView = new();
		_ = ratingView.Control!.Count.Should().Be(RatingViewDefaults.MaximumRating);
		ratingView.MaximumRating = minMaximumRating;
		_ = ratingView.Control!.Count.Should().Be(minMaximumRating);
	}

	[Fact]
	public void Properties_MaximumRating_NumberOfChildrenLower()
	{
		const byte minMaximumRating = 3;
		RatingView ratingView = new();
		_ = ratingView.Control!.Count.Should().Be(RatingViewDefaults.MaximumRating);
		ratingView.MaximumRating = minMaximumRating;
		_ = ratingView.Control!.Count.Should().Be(minMaximumRating);
	}

	[Fact]
	public void Properties_MaximumRating_Validator()
	{
		RatingView ratingView = new();
		_ = RatingView.MaximumRatingProperty.ValidateValue(ratingView, (byte)0).Should().BeFalse();
		_ = RatingView.MaximumRatingProperty.ValidateValue(ratingView, (byte)(RatingViewDefaults.MaximumRating + 1)).Should().BeFalse();
		_ = RatingView.MaximumRatingProperty.ValidateValue(ratingView, (byte)1).Should().BeTrue();
	}

	[Fact]
	public void ViewStructure_Control_IsHorizontalStackLayout()
	{
		RatingView ratingView = new();
		_ = Assert.IsType<HorizontalStackLayout>(ratingView.Control);
	}

	[Fact]
	public void ViewStructure_CorrectNumberOfChildren()
	{
		const int maximumRating = 3;
		RatingView ratingView = new()
		{
			MaximumRating = maximumRating
		};

		_ = (ratingView.Control?.GetVisualTreeDescendants().Should().HaveCount((maximumRating * 2) + 1));
		_ = (ratingView.Control?.Children.Should().HaveCount(maximumRating));
	}

	[Fact]
	public void ViewStructure_Item_IsBorder()
	{
		RatingView ratingView = new();
		Assert.NotNull(ratingView.Control);
		Assert.NotNull(ratingView.Control.Children[0]);
		_ = Assert.IsType<Border>(ratingView.Control!.Children[0]);
	}

	[Fact]
	public void ViewStructure_ItemChild_IsPath()
	{
		RatingView ratingView = new();
		Assert.NotNull(((Border)ratingView.Control!.Children[0]).Content);
		_ = Assert.IsType<Microsoft.Maui.Controls.Shapes.Path>(((Border)ratingView.Control!.Children[0]).Content!.GetVisualTreeDescendants()[0]);
	}

	[Fact]
	public void ViewStructure_ItemFill_Colors()
	{
		Color filledBackgroundColor = Colors.Red;
		Color emptyBackgroundColor = Colors.Grey;
		Color backgroundColor = Colors.CornflowerBlue;
		RatingView ratingView = new() { Rating = 0, MaximumRating = 3, RatingFill = RatingFillElement.Item, FilledBackgroundColor = filledBackgroundColor, EmptyBackgroundColor = emptyBackgroundColor, BackgroundColor = backgroundColor };
		ratingView.Rating = 1.5;
		Border filledRatingItem = (Border)ratingView.Control!.Children[0];
		Border partialFilledRatingItem = (Border)ratingView.Control!.Children[1];
		Border emptyFilledRatingItem = (Border)ratingView.Control!.Children[2];
		_ = filledRatingItem.BackgroundColor.Should().BeOfType<Color>().And.Be(filledBackgroundColor);
		_ = ((Shape)filledRatingItem.Content!).Fill.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(emptyBackgroundColor));
		_ = emptyFilledRatingItem.BackgroundColor.Should().BeOfType<Color>().And.Be(backgroundColor);
		_ = emptyFilledRatingItem.Background.Should().BeNull();
		_ = ((Shape)emptyFilledRatingItem.Content!).Fill.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(emptyBackgroundColor));
		_ = partialFilledRatingItem.Background.Should().BeOfType<LinearGradientBrush>();
		_ = ((Shape)partialFilledRatingItem.Content!).Fill.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(emptyBackgroundColor));
	}

	[Fact]
	public void ViewStructure_ShapeFill_Colors()
	{
		Color filledBackgroundColor = Colors.Red;
		Color emptyBackgroundColor = Colors.Grey;
		RatingView ratingView = new() { Rating = 1.5, MaximumRating = 3, RatingFill = RatingFillElement.Shape, FilledBackgroundColor = filledBackgroundColor, EmptyBackgroundColor = emptyBackgroundColor };
		Microsoft.Maui.Controls.Shapes.Path filledRatingItem = (Microsoft.Maui.Controls.Shapes.Path)((Border)ratingView.Control!.Children[0]).Content!.GetVisualTreeDescendants()[0];
		Microsoft.Maui.Controls.Shapes.Path partialFilledRatingItem = (Microsoft.Maui.Controls.Shapes.Path)((Border)ratingView.Control!.Children[1]).Content!.GetVisualTreeDescendants()[0];
		Microsoft.Maui.Controls.Shapes.Path emptyFilledRatingItem = (Microsoft.Maui.Controls.Shapes.Path)((Border)ratingView.Control!.Children[2]).Content!.GetVisualTreeDescendants()[0];
		_ = filledRatingItem.Fill.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(filledBackgroundColor));
		_ = emptyFilledRatingItem.Fill.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(emptyBackgroundColor));
		_ = partialFilledRatingItem.Fill.Should().BeOfType<LinearGradientBrush>();
	}
}

class RetingViewModel : INotifyPropertyChanged
{
	byte maxRating = 0;

	public event PropertyChangedEventHandler? PropertyChanged;

	public byte MaxRating
	{
		get => maxRating;
		set
		{
			if (!Equals(value, maxRating))
			{
				maxRating = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxRating)));
			}
		}
	}
}