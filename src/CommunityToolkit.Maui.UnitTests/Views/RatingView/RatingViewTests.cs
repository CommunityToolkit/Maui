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
		MockRatingViewViewModel vm = new();
		RatingView ratingViewWithBinding = new();
		
		ratingViewWithBinding.BindingContext.Should().BeNull();
		ratingViewWithBinding.RatingLayout.BindingContext.Should().BeNull();
		ratingViewWithBinding.RatingLayout.BindingContext.Should().BeEquivalentTo(ratingViewWithBinding.BindingContext);
		
		ratingViewWithBinding.BindingContext = vm;
		
		ratingViewWithBinding.BindingContext.Should().Be(vm);
		ratingViewWithBinding.RatingLayout.BindingContext.Should().Be(vm);
		ratingViewWithBinding.RatingLayout.BindingContext.Should().BeEquivalentTo(ratingViewWithBinding.BindingContext);
	}

	[Fact]
	public void Defaults_ItemDefaultsApplied()
	{
		RatingView ratingView = new();
		var firstItem = (Border)ratingView.RatingLayout.Children[0];

		firstItem.Should().BeOfType<Border>();
		firstItem.BackgroundColor.Should().BeNull();
		firstItem.Margin.Should().Be(Thickness.Zero);
		firstItem.Padding.Should().Be(RatingViewDefaults.ItemPadding);
		firstItem.Stroke.Should().Be(new SolidColorBrush(Colors.Transparent));
		firstItem.StrokeThickness.Should().Be(0);
		firstItem.Style.Should().BeNull();
	}

	[Fact]
	public void Defaults_ShapeDefaultsApplied()
	{
		RatingView ratingView = new();
		var firstItemShape = GetItemShape(ratingView, 0);
		
		firstItemShape.Should().NotBeNull();
		firstItemShape.Should().BeOfType<Microsoft.Maui.Controls.Shapes.Path>();
		firstItemShape.Aspect.Should().Be(Stretch.Uniform);
		firstItemShape.HeightRequest.Should().Be(RatingViewDefaults.ItemShapeSize);
		firstItemShape.Stroke.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(RatingViewDefaults.ShapeBorderColor));
		firstItemShape.StrokeLineCap.Should().Be(PenLineCap.Round);
		firstItemShape.StrokeLineJoin.Should().Be(PenLineJoin.Round);
		firstItemShape.StrokeThickness.Should().Be(RatingViewDefaults.ShapeBorderThickness);
		firstItemShape.WidthRequest.Should().Be(RatingViewDefaults.ItemShapeSize);
	}

	[Fact]
	public void Defaults_ShouldHaveCorrectDefaultProperties()
	{
		RatingView ratingView = new();
		ratingView.Rating.Should().Be(RatingViewDefaults.DefaultRating);
		ratingView.EmptyColor.Should().BeOfType<Color>().And.Be(RatingViewDefaults.EmptyColor);
		ratingView.FilledColor.Should().BeOfType<Color>().And.Be(RatingViewDefaults.FilledColor);
		ratingView.IsReadOnly.Should().BeFalse().And.Be(RatingViewDefaults.IsReadOnly);
		ratingView.ItemPadding.Should().BeOfType<Thickness>().And.Be(RatingViewDefaults.ItemPadding);
		ratingView.ItemShapeSize.Should().Be(RatingViewDefaults.ItemShapeSize);
		ratingView.MaximumRating.Should().Be(RatingViewDefaults.MaximumRating);
		ratingView.ItemShape.Should().BeOneOf(RatingViewDefaults.Shape).And.Be(RatingViewDefaults.Shape);
		ratingView.ShapeBorderColor.Should().BeOfType<Color>().And.Be(RatingViewDefaults.ShapeBorderColor);
		ratingView.ShapeBorderThickness.Should().Be(RatingViewDefaults.ShapeBorderThickness);
		ratingView.Spacing.Should().Be(RatingViewDefaults.Spacing);
		ratingView.RatingFill.Should().BeOneOf(RatingFillElement.Shape).And.Be(RatingFillElement.Shape);
		ratingView.CustomItemShape.Should().BeNullOrEmpty();
	}

	[Fact]
	public void Events_Border_TapGestureRecognizer_SingleRating_Toggled()
	{
		RatingView ratingView = new()
		{
			MaximumRating = 1
		};
		var child = (Border)ratingView.RatingLayout.Children[0];
		var tapGestureRecognizer = (TapGestureRecognizer)child.GestureRecognizers[0];
		tapGestureRecognizer.SendTapped(child);
		ratingView.Rating.Should().Be(1);

		tapGestureRecognizer.SendTapped(child);
		ratingView.Rating.Should().Be(0);
	}

	[Fact]
	public void Events_Border_TapGestureRecognizer_Tapped()
	{
		var handlerTappedCount = 0;
		RatingView ratingView = new();
		ratingView.Rating.Should().Be(handlerTappedCount);
		var child = (Border)ratingView.RatingLayout.Children[0];
		var tgr = (TapGestureRecognizer)child.GestureRecognizers[0];
		tgr.SendTapped(child);
		handlerTappedCount++;
		ratingView.Rating.Should().Be(handlerTappedCount);
	}

	[Fact]
	public void Events_RatingChanged_AddRemove()
	{
		List<RatingChangedEventArgs> receivedEvents = [];
		const double expectedRating = 2;
		RatingView ratingView = new()
		{
			MaximumRating = 3,
			Rating = 3
		};
		ratingView.RatingChanged += OnRatingChanged;
		ratingView.Rating = expectedRating;
		receivedEvents.Should().HaveCount(1);
		receivedEvents[0].Rating.Should().Be(expectedRating);

		void OnRatingChanged(object? sender, RatingChangedEventArgs e)
		{
			ratingView.RatingChanged -= OnRatingChanged;
			receivedEvents.Add(e);
		}
	}

	[Fact]
	public void Events_RatingChanged_ShouldBeRaised_MaximumRatingPropertyChanged_LNotReadOnly()
	{
		const double currentRating = 5;
		const double maximumRating = 4;
		RatingView ratingView = new()
		{
			Rating = currentRating
		};

		var signaled = false;
		ratingView.RatingChanged += (sender, e) => signaled = true;
		ratingView.MaximumRating = 4;
		ratingView.Rating.Should().Be(maximumRating);
		signaled.Should().BeTrue();
	}

	[Fact]
	public void Events_RatingChanged_ShouldBeRaised_RatingPropertyChanged_NotReadOnly()
	{
		const double currentRating = 3.5;
		RatingView ratingView = new();
		ratingView.Rating.Should().Be(RatingViewDefaults.DefaultRating);
		var signaled = false;
		ratingView.RatingChanged += (sender, e) => signaled = true;
		ratingView.Rating = currentRating;
		ratingView.Rating.Should().Be(currentRating);
		signaled.Should().BeTrue();
	}

	[Fact]
	public void Events_RatingChanged_ShouldNotBeRaised_MaximumRatingPropertyChanged_HNotReadOnly()
	{
		const double currentRating = 5;
		const int maximumRating = 7;
		RatingView ratingView = new()
		{
			Rating = currentRating
		};

		var signaled = false;
		ratingView.RatingChanged += (sender, e) => signaled = true;
		ratingView.MaximumRating = maximumRating;
		ratingView.Rating.Should().Be(currentRating);
		signaled.Should().BeFalse();
	}

	[Fact]
	public void Events_RatingChanged_ShouldNotBeRaised_MaximumRatingPropertyChanged_HReadOnly()
	{
		const double currentRating = 5;
		const int maximumRating = 7;
		RatingView ratingView = new()
		{
			Rating = currentRating,
			IsReadOnly = true,
		};

		var signaled = false;
		ratingView.RatingChanged += (sender, e) => signaled = true;
		ratingView.MaximumRating = maximumRating;
		ratingView.Rating.Should().Be(currentRating);
		signaled.Should().BeFalse();
	}

	[Fact]
	public void Events_RatingChanged_ShouldBeRaised_MaximumRatingPropertyChanged_LReadOnly()
	{
		const double currentRating = 5;
		const int maximumRating = 4;
		RatingView ratingView = new()
		{
			Rating = currentRating,
			IsReadOnly = true,
		};

		var signaled = false;
		ratingView.RatingChanged += (sender, e) => signaled = true;
		ratingView.MaximumRating = maximumRating;
		ratingView.Rating.Should().Be(maximumRating);
		signaled.Should().BeTrue();
	}

	[Fact]
	public void Events_RatingChanged_ShouldBeRaised_RatingPropertyChanged_ReadOnly()
	{
		const double currentRating = 3.5;
		RatingView ratingView = new()
		{
			IsReadOnly = true
		};

		var signaled = false;
		ratingView.RatingChanged += (sender, e) => signaled = true;
		ratingView.Rating = currentRating;
		ratingView.Rating.Should().Be(currentRating);
		signaled.Should().BeTrue();
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
		receivedEvents.Should().HaveCount(1);
		receivedEvents[0].Rating.Should().Be(expectedRating);
	}

	[Fact]
	public void Events_ShouldBeRaised_RatingChangedEvent()
	{
		List<RatingChangedEventArgs> receivedEvents = [];
		const double expectedRating = 2.0;
		RatingView ratingView = new();
		ratingView.RatingChanged += (sender, e) => receivedEvents.Add(e);
		ratingView.Rating = expectedRating;
		receivedEvents.Should().ContainSingle();
		receivedEvents[0].Rating.Should().Be(expectedRating);
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
		receivedEvents.Should().HaveCount(0);
	}

	[Fact]
	public void MaximumRatingViewThrowsArgumentOutOfRangeExceptionWhenOutsideLowerBounds()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new RatingView().MaximumRating = 0);
	}

	[Fact]
	public void MaximumRatingViewThrowsArgumentOutOfRangeExceptionWhenOutsideUpperBounds()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new RatingView().MaximumRating = RatingViewDefaults.MaximumRatingLimit + 1);
	}

	[Fact]
	public void Null_EmptyColor()
	{
		RatingView ratingView = new();
		ratingView.EmptyColor.Should().NotBeNull();
		ratingView.EmptyColor = null;
		ratingView.EmptyColor.Should().BeOfType<Color>().And.Be(Colors.Transparent);
	}

	[Fact]
	public void Null_FilledColor()
	{
		RatingView ratingView = new();
		ratingView.FilledColor.Should().NotBeNull();
		ratingView.FilledColor = null;
		ratingView.FilledColor.Should().BeOfType<Color>().And.Be(Colors.Transparent);
	}

	[Fact]
	public void Null_ShapeBorderColor()
	{
		RatingView ratingView = new();
		ratingView.ShapeBorderColor.Should().NotBeNull();
		ratingView.ShapeBorderColor = null;
		ratingView.ShapeBorderColor.Should().BeOfType<Color>().And.Be(Colors.Transparent);
	}

	[Fact]
	public void Properties_Change_CustomShape()
	{
		const string customShape = "M 12 0C5.388 0 0 5.388 0 12s5.388 12 12 12 12-5.38 12-12c0-6.612-5.38-12-12-12z";
		RatingView ratingView = new();
		ratingView.CustomItemShape.Should().BeNullOrEmpty();
		ratingView.CustomItemShape = customShape;
		ratingView.CustomItemShape.Should().Be(customShape);
	}

	[Theory]
	[InlineData("")]
	[InlineData(null)]
	public void Properties_Change_CustomShape_Null(string? customShapes)
	{
		const string customShape = "M 12 0C5.388 0 0 5.388 0 12s5.388 12 12 12 12-5.38 12-12c0-6.612-5.38-12-12-12z";
		RatingView ratingView = new()
		{
			ItemShape = RatingViewShape.Custom,
			CustomItemShape = customShape,
		};
		ratingView.ItemShape.Should().Be(RatingViewShape.Custom);
		ratingView.CustomItemShape.Should().Be(customShape);
		ratingView.CustomItemShape = customShapes;
		ratingView.ItemShape.Should().Be(RatingViewShape.Star);
	}

	[Fact]
	public void Properties_Change_CustomShape_ShapeCustom()
	{
		const string customShape = "M 12 0C5.388 0 0 5.388 0 12s5.388 12 12 12 12-5.38 12-12c0-6.612-5.38-12-12-12z";
		RatingView ratingView = new()
		{
			ItemShape = RatingViewShape.Custom
		};
		ratingView.ItemShape.Should().Be(RatingViewShape.Custom);
		ratingView.CustomItemShape = customShape;
		ratingView.CustomItemShape.Should().Be(customShape);
		ratingView.ItemShape.Should().Be(RatingViewShape.Custom);
	}

	[Fact]
	public void Properties_Change_CustomShape_ShapeNotCustom()
	{
		const string customShape = "M 12 0C5.388 0 0 5.388 0 12s5.388 12 12 12 12-5.38 12-12c0-6.612-5.38-12-12-12z";
		RatingView ratingView = new()
		{
			ItemShape = RatingViewShape.Heart
		};
		ratingView.ItemShape.Should().Be(RatingViewShape.Heart);
		ratingView.CustomItemShape = customShape;
		ratingView.CustomItemShape.Should().Be(customShape);
		ratingView.ItemShape.Should().Be(RatingViewShape.Heart);
	}

	[Fact]
	public void Properties_Change_EmptyColor_Item()
	{
		const double rating = 1.5;
		const byte maximumRating = 7;
		var emptyColor = Colors.Snow;
		RatingView ratingView = new()
		{
			MaximumRating = maximumRating,
			Rating = rating,
			RatingFill = RatingFillElement.Item
		};
		ratingView.EmptyColor.Should().NotBe(emptyColor);
		ratingView.EmptyColor = emptyColor;
		ratingView.EmptyColor.Should().Be(emptyColor);
		
		var emptyRatingItem = (Microsoft.Maui.Controls.Shapes.Path) GetItemShape(ratingView, maximumRating - 1).GetVisualTreeDescendants()[0];
		emptyRatingItem.Fill.Should().Be(new SolidColorBrush(emptyColor));
	}

	[Fact]
	public void Properties_Change_EmptyColor_Shape()
	{
		const double rating = 1.5;
		const byte maximumRating = 7;
		var emptyColor = Colors.Snow;
		RatingView ratingView = new()
		{
			MaximumRating = maximumRating,
			Rating = rating
		};
		ratingView.EmptyColor.Should().NotBe(emptyColor);
		ratingView.EmptyColor = emptyColor;
		ratingView.EmptyColor.Should().Be(emptyColor);
		var emptyRatingItem = GetItemShape(ratingView, maximumRating - 1);
		emptyRatingItem.Fill.Should().Be(new SolidColorBrush(emptyColor));
	}

	[Fact]
	public void Properties_Change_FilledColor_Item()
	{
		const double rating = 1.5;
		const byte maximumRating = 7;
		var filledColor = Colors.Snow;
		var emptyColor = Colors.Firebrick;
		var backgroundColor = Colors.DarkGreen;
		RatingView ratingView = new()
		{
			MaximumRating = maximumRating,
			Rating = rating,
			RatingFill = RatingFillElement.Item
		};
		ratingView.FilledColor.Should().NotBe(filledColor);
		ratingView.BackgroundColor.Should().BeNull();
		ratingView.BackgroundColor = backgroundColor;
		ratingView.EmptyColor = emptyColor;
		ratingView.FilledColor = filledColor;
		ratingView.FilledColor.Should().Be(filledColor);
		var filledRatingShape = GetItemShape(ratingView, (int)Math.Floor(rating));
		filledRatingShape.Fill.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(emptyColor));
		var filledRatingItem = (Border)ratingView.RatingLayout.Children[0];
		filledRatingItem.Background.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(filledColor));
		var partialFilledRatingItem = (Border)ratingView.RatingLayout.Children[(int)Math.Floor(rating)];
		partialFilledRatingItem.Background.Should().BeOfType<LinearGradientBrush>();
		var emptyFilledRatingItem = (Border)ratingView.RatingLayout.Children[maximumRating - 1]; // Check the last one, as this is where we expect the background colour to be set
		emptyFilledRatingItem.Background.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(backgroundColor));
	}

	[Fact]
	public void Properties_Change_FilledColor_Shape()
	{
		const double rating = 1.5;
		const byte maximumRating = 7;
		var filledColor = Colors.Snow;
		RatingView ratingView = new()
		{
			MaximumRating = maximumRating,
			Rating = rating
		};
		ratingView.FilledColor.Should().NotBe(filledColor);
		ratingView.FilledColor = filledColor;
		ratingView.FilledColor.Should().Be(filledColor);
		var filledRatingItem = GetItemShape(ratingView, 0);
		filledRatingItem.Fill.Should().Be(new SolidColorBrush(filledColor));
	}

	[Fact]
	public void Properties_Change_IsReadOnly()
	{
		RatingView ratingView = new();
		ratingView.IsReadOnly.Should().BeFalse();
		ratingView.IsReadOnly = true;
		ratingView.IsReadOnly.Should().BeTrue();
		ratingView.IsReadOnly = false;
		ratingView.IsReadOnly.Should().BeFalse();
	}

	[Fact]
	public void Properties_Change_IsReadOnly_GestureRecognizers()
	{
		RatingView ratingView = new();
		ratingView.IsReadOnly.Should().BeFalse();
		foreach (var t in ratingView.RatingLayout.Children)
		{
			var child = (Border)t;
			child.GestureRecognizers.Should().ContainSingle();
		}

		ratingView.IsReadOnly = true;
		ratingView.IsReadOnly.Should().BeTrue();
		foreach (var t in ratingView.RatingLayout.Children)
		{
			var child = (Border)t;
			child.GestureRecognizers.Should().BeEmpty();
		}
	}

	[Fact]
	public void Properties_Change_ItemPadding()
	{
		Thickness itemPadding = new(1, 2, 3, 4);
		RatingView ratingView = new();
		ratingView.ItemPadding.Should().NotBe(itemPadding);
		ratingView.ItemPadding = itemPadding;
		ratingView.ItemPadding.Should().Be(itemPadding);
		var firstItem = (Border)ratingView.RatingLayout.Children[0];
		firstItem.Padding.Should().Be(itemPadding);
	}

	[Fact]
	public void Properties_Change_MaximumRating()
	{
		const byte maximumRating = 7;
		RatingView ratingView = new();
		ratingView.MaximumRating.Should().NotBe(maximumRating);
		ratingView.MaximumRating = maximumRating;
		ratingView.MaximumRating.Should().Be(maximumRating);
		ratingView.RatingLayout.Children.Should().HaveCount(maximumRating);
	}

	[Fact]
	public void Properties_Change_Rating()
	{
		const double rating = 2.3;
		RatingView ratingView = new();
		ratingView.Rating.Should().NotBe(rating);
		ratingView.Rating = rating;
		ratingView.Rating.Should().Be(rating);
	}

	[Fact]
	public void Properties_Change_RatingFill()
	{
		const RatingFillElement ratingFill = RatingFillElement.Item;
		RatingView ratingView = new();
		ratingView.RatingFill.Should().NotBe(ratingFill);
		ratingView.RatingFill = ratingFill;
		ratingView.RatingFill.Should().Be(ratingFill);
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
		ratingView.ItemShape.Should().NotBe(expectedShape);
		ratingView.ItemShape = expectedShape;
		ratingView.ItemShape.Should().Be(expectedShape);
	}

	[Fact]
	public void Properties_Change_ShapeBorderColor()
	{
		var shapeBorderColor = Colors.Snow;
		Brush brush = new SolidColorBrush(shapeBorderColor);
		RatingView ratingView = new();
		
		ratingView.ShapeBorderColor.Should().NotBe(shapeBorderColor);
		ratingView.ShapeBorderColor = shapeBorderColor;
		ratingView.ShapeBorderColor.Should().Be(shapeBorderColor);
		
		var firstRatingItem = GetItemShape(ratingView, 0);
		firstRatingItem.Stroke.Should().BeOfType<SolidColorBrush>().And.Be(brush);
	}

	[Fact]
	public void Properties_Change_ShapeBorderThickness()
	{
		const double shapeBorderThickness = 7.3;
		RatingView ratingView = new();
		ratingView.ShapeBorderThickness.Should().NotBe(shapeBorderThickness);
		ratingView.ShapeBorderThickness = shapeBorderThickness;
		ratingView.ShapeBorderThickness.Should().Be(shapeBorderThickness);
		
		var firstRatingItem = GetItemShape(ratingView, 0);
		firstRatingItem.StrokeThickness.Should().Be(shapeBorderThickness);
	}

	[Fact]
	public void Properties_Change_Size()
	{
		const int itemShapeSize = 73;
		RatingView ratingView = new();
		ratingView.ItemShapeSize.Should().NotBe(itemShapeSize);
		ratingView.ItemShapeSize = itemShapeSize;
		ratingView.ItemShapeSize.Should().Be(itemShapeSize);
		
		var firstRatingItem = GetItemShape(ratingView, 0);
		firstRatingItem.WidthRequest.Should().Be(itemShapeSize);
		firstRatingItem.HeightRequest.Should().Be(itemShapeSize);
	}

	[Fact]
	public void Properties_Change_Spacing()
	{
		const int spacing = 73;
		RatingView ratingView = new();
		ratingView.Spacing.Should().NotBe(spacing);
		ratingView.Spacing = spacing;
		ratingView.Spacing.Should().Be(spacing);
		var control = ratingView.RatingLayout;
		control.Should().NotBeNull();
		control.Spacing.Should().Be(spacing);
	}

	[Fact]
	public void Properties_MaximumRating_KeptInRange()
	{
		RatingView ratingView = new();
		const byte minMaximumRating = 1;
		const byte maxMaximumRating = RatingViewDefaults.MaximumRatingLimit;
		ratingView.MaximumRating = minMaximumRating;
		ratingView.MaximumRating.Should().Be(1);
		ratingView.MaximumRating = maxMaximumRating;
		ratingView.MaximumRating.Should().Be(RatingViewDefaults.MaximumRatingLimit);
	}

	[Fact]
	public void Properties_MaximumRating_NumberOfChildrenHigher()
	{
		const byte minMaximumRating = 7;
		RatingView ratingView = new();
		ratingView.RatingLayout.Count.Should().Be(RatingViewDefaults.MaximumRating);
		ratingView.MaximumRating = minMaximumRating;
		ratingView.RatingLayout.Count.Should().Be(minMaximumRating);
	}

	[Fact]
	public void Properties_MaximumRating_NumberOfChildrenLower()
	{
		const byte minMaximumRating = 3;
		RatingView ratingView = new();
		ratingView.RatingLayout.Count.Should().Be(RatingViewDefaults.MaximumRating);
		ratingView.MaximumRating = minMaximumRating;
		ratingView.RatingLayout.Count.Should().Be(minMaximumRating);
	}

	[Fact]
	public void Properties_MaximumRating_Validator()
	{
		RatingView ratingView = new();
		RatingView.MaximumRatingProperty.ValidateValue(ratingView, 0).Should().BeFalse();
		RatingView.MaximumRatingProperty.ValidateValue(ratingView, RatingViewDefaults.MaximumRatingLimit + 1).Should().BeFalse();
		RatingView.MaximumRatingProperty.ValidateValue(ratingView, 1).Should().BeTrue();
	}

	[Fact]
	public void Properties_Rating_Validator()
	{
		RatingView ratingView = new();
		RatingView.RatingProperty.ValidateValue(ratingView, -1.0).Should().BeFalse();
		RatingView.RatingProperty.ValidateValue(ratingView, (double)(RatingViewDefaults.MaximumRatingLimit + 1)).Should().BeFalse();
		RatingView.RatingProperty.ValidateValue(ratingView, 0.1).Should().BeTrue();
	}

	[Fact]
	public void RatingViewDoesNotThrowsArgumentOutOfRangeExceptionWhenRatingSetBeforeMaximumRating()
	{
		const int maximumRating = RatingViewDefaults.MaximumRatingLimit - 1;
		const int rating = RatingViewDefaults.MaximumRatingLimit - 3;

		RatingView ratingView = new()
		{
			MaximumRating = maximumRating,
			Rating = rating,
		};

		Assert.Equal(rating, ratingView.Rating);
		Assert.Equal(maximumRating, ratingView.MaximumRating);
	}

	[Fact]
	public void RatingViewThrowsInvalidOperationExceptionWhenBorderChildIsNotShape()
	{
		RatingView ratingView = new();
		((Border)ratingView.RatingLayout.Children[0]).Content = new Button();
		Assert.Throws<InvalidOperationException>(() => ratingView.Rating = 1);
	}

	[Fact]
	public void RatingViewThrowsInvalidOperationExceptionWhenChildIsNotBorder()
	{
		RatingView ratingView = new();
		ratingView.RatingLayout.Children.Add(new Button());
		Assert.Throws<InvalidOperationException>(() => ratingView.Rating = 1);
	}

	[Fact]
	public void RatingViewThrowsArgumentOutOfRangeExceptionWhenOutsideLowerBounds()
	{
		RatingView ratingView = new();
		Assert.Throws<ArgumentOutOfRangeException>(() => ratingView.Rating = 0 - double.Epsilon);
	}

	[Fact]
	public void RatingViewThrowsArgumentOutOfRangeExceptionWhenOutsideUpperBounds()
	{
		RatingView ratingView = new();
		Assert.Throws<ArgumentOutOfRangeException>(() => ratingView.Rating = ratingView.MaximumRating + 1);
	}

	[Fact]
	public void RatingViewThrowsArgumentOutOfRangeExceptionWhenRatingSetBeforeMaximumRating()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new RatingView
		{
			Rating = RatingViewDefaults.MaximumRatingLimit - 1,
			MaximumRating = RatingViewDefaults.MaximumRatingLimit
		});
	}

	[Fact]
	public void ShapeBorderThicknessShouldThrowArgumentOutOfRangeExceptionForNegativeNumbers()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new RatingView
		{
			ShapeBorderThickness = -1
		});
	}

	[Fact]
	public void ViewStructure_Control_IsHorizontalStackLayout()
	{
		RatingView ratingView = new();
		ratingView.RatingLayout.Should().BeOfType<HorizontalStackLayout>();
	}

	[Fact]
	public void ViewStructure_CorrectNumberOfChildren()
	{
		const int maximumRating = 3;
		RatingView ratingView = new()
		{
			MaximumRating = maximumRating
		};

		Assert.NotNull(ratingView.ControlTemplate);
		ratingView.RatingLayout.GetVisualTreeDescendants().Should().HaveCount((maximumRating * 2) + 1);
		ratingView.RatingLayout.Children.Should().HaveCount(maximumRating);
	}

	[Fact]
	public void ViewStructure_Item_IsBorder()
	{
		RatingView ratingView = new();
		ratingView.RatingLayout.Should().NotBeNull();
		ratingView.RatingLayout.Children[0].Should().NotBeNull();
		ratingView.RatingLayout.Children[0].Should().BeOfType<Border>();
	}

	[Fact]
	public void ViewStructure_ItemChild_IsPath()
	{
		RatingView ratingView = new();
		ratingView.RatingLayout.Children[0].Should().BeOfType<Border>();
		var child = (Border)ratingView.RatingLayout.Children[0];
		
		Assert.NotNull(child.Content);
		child.Content.GetVisualTreeDescendants()[0].Should().BeOfType<Microsoft.Maui.Controls.Shapes.Path>();
	}

	[Fact]
	public void ViewStructure_ItemChild_Path_Star()
	{
		RatingView ratingView = new();
		ratingView.RatingLayout.Children[0].Should().BeOfType<Border>();
		var child = (Border)ratingView.RatingLayout.Children[0];
		
		Assert.NotNull(child.Content);
		
		var shape = (Microsoft.Maui.Controls.Shapes.Path)child.Content.GetVisualTreeDescendants()[0];
		shape.GetPath().Should().Be(Core.Primitives.RatingViewShape.Star.PathData);
	}

	[Fact]
	public void ViewStructure_ItemFill_Colors()
	{
		var filledColor = Colors.Red;
		var emptyColor = Colors.Grey;
		var backgroundColor = Colors.CornflowerBlue;
		RatingView ratingView = new()
		{
			Rating = 0,
			MaximumRating = 3,
			RatingFill = RatingFillElement.Item,
			FilledColor = filledColor,
			EmptyColor = emptyColor,
			BackgroundColor = backgroundColor
		};
		ratingView.Rating = 1.5;
		var filledRatingItem = (Border)ratingView.RatingLayout.Children[0];
		var partialFilledRatingItem = (Border)ratingView.RatingLayout.Children[1];
		var emptyFilledRatingItem = (Border)ratingView.RatingLayout.Children[2];
		filledRatingItem.Background.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(filledColor));
		((Shape)filledRatingItem.Content!).Fill.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(emptyColor));
		partialFilledRatingItem.Background.Should().BeOfType<LinearGradientBrush>();
		((Shape)partialFilledRatingItem.Content!).Fill.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(emptyColor));
		emptyFilledRatingItem.Background.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(backgroundColor));
		((Shape)emptyFilledRatingItem.Content!).Fill.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(emptyColor));
	}

	[Fact]
	public void ViewStructure_ItemPadding()
	{
		const double expectedLeftPadding = 7;
		const double expectedTopPadding = 3;
		const double expectedRightPadding = 3;
		const double expectedBottomPadding = 7;
		Thickness expectedItemPadding = new(expectedLeftPadding, expectedTopPadding, expectedRightPadding, expectedBottomPadding);
		RatingView ratingView = new()
		{
			ItemPadding = expectedItemPadding,
		};
		var firstItem = (Border)ratingView.RatingLayout.Children[0];
		firstItem.Padding.Should().Be(expectedItemPadding);
	}

	[Fact]
	public void ViewStructure_ShapeFill_Colors()
	{
		var filledColor = Colors.Red;
		var emptyColor = Colors.Grey;
		RatingView ratingView = new()
		{
			Rating = 1.5,
			MaximumRating = 3,
			RatingFill = RatingFillElement.Shape,
			FilledColor = filledColor,
			EmptyColor = emptyColor
		};
		var filledRatingItem = GetItemShape(ratingView, 0);
		var partialFilledRatingItem = GetItemShape(ratingView, 1);
		var emptyFilledRatingItem = GetItemShape(ratingView, 2);
		filledRatingItem.Fill.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(filledColor));
		emptyFilledRatingItem.Fill.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(emptyColor));
		partialFilledRatingItem.Fill.Should().BeOfType<LinearGradientBrush>();
	}

	[Fact]
	public void ViewStructure_Spacing()
	{
		RatingView ratingView = new();
		var rvControl = ratingView.RatingLayout;
		rvControl.Spacing.Should().Be(RatingViewDefaults.Spacing);
	}

	static Microsoft.Maui.Controls.Shapes.Path GetItemShape(in RatingView ratingView, int itemIndex)
	{
		var border = (Border)ratingView.RatingLayout.Children[itemIndex];
		Assert.NotNull(border.Content);

		return (Microsoft.Maui.Controls.Shapes.Path)border.Content.GetVisualTreeDescendants()[0];
	}

	sealed class MockRatingViewViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler? PropertyChanged;

		public int MaxRating
		{
			get;
			set
			{
				if (!Equals(value, field))
				{
					field = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxRating)));
				}
			}
		} = 0;
	}
}