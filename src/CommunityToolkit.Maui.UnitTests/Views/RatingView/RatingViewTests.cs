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
	const string customShape = "M 12 0C5.388 0 0 5.388 0 12s5.388 12 12 12 12-5.38 12-12c0-6.612-5.38-12-12-12z";

	[Fact]
	public void VerifyIncludedShapePaths()
	{
		const string expectedStarPath = "M9 11.3l3.71 2.7-1.42-4.36L15 7h-4.55L9 2.5 7.55 7H3l3.71 2.64L5.29 14z";
		const string expectedHeartPath = "M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z";
		const string expectedCirclePath = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2z";
		const string expectedLikePath = "M1 21h4V9H1v12zm22-11c0-1.1-.9-2-2-2h-6.31l.95-4.57.03-.32c0-.41-.17-.79-.44-1.06L14.17 1 7.59 7.59C7.22 7.95 7 8.45 7 9v10c0 1.1.9 2 2 2h9c.83 0 1.54-.5 1.84-1.22l3.02-7.05c.09-.23.14-.47.14-.73v-1.91l-.01-.01L23 10z";
		const string expectedDislikePath = "M15 3H6c-.83 0-1.54.5-1.84 1.22l-3.02 7.05c-.09.23-.14.47-.14.73v1.91l.01.01L1 14c0 1.1.9 2 2 2h6.31l-.95 4.57-.03.32c0 .41.17.79.44 1.06L9.83 23l6.59-6.59c.36-.36.58-.86.58-1.41V5c0-1.1-.9-2-2-2zm4 0v12h4V3h-4z";

		RatingView.PathShapes.Star.Should().BeEquivalentTo(expectedStarPath);
		RatingView.PathShapes.Heart.Should().BeEquivalentTo(expectedHeartPath);
		RatingView.PathShapes.Circle.Should().BeEquivalentTo(expectedCirclePath);
		RatingView.PathShapes.Like.Should().BeEquivalentTo(expectedLikePath);
		RatingView.PathShapes.Dislike.Should().BeEquivalentTo(expectedDislikePath);
	}

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
		firstItem.Padding.Should().Be(RatingViewDefaults.ShapePadding);
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
		ratingView.EmptyShapeColor.Should().BeOfType<Color>().And.Be(RatingViewDefaults.EmptyShapeColor);
		ratingView.FillColor.Should().BeOfType<Color>().And.Be(RatingViewDefaults.FillColor);
		ratingView.IsReadOnly.Should().BeFalse().And.Be(RatingViewDefaults.IsReadOnly);
		ratingView.ShapePadding.Should().BeOfType<Thickness>().And.Be(RatingViewDefaults.ShapePadding);
		ratingView.ShapeDiameter.Should().Be(RatingViewDefaults.ItemShapeSize);
		ratingView.MaximumRating.Should().Be(RatingViewDefaults.MaximumRating);
		ratingView.Shape.Should().BeOneOf(RatingViewDefaults.Shape).And.Be(RatingViewDefaults.Shape);
		ratingView.ShapeBorderColor.Should().BeOfType<Color>().And.Be(RatingViewDefaults.ShapeBorderColor);
		ratingView.ShapeBorderThickness.Should().Be(RatingViewDefaults.ShapeBorderThickness);
		ratingView.Spacing.Should().Be(RatingViewDefaults.Spacing);
		ratingView.FillOption.Should().BeOneOf(RatingViewFillOption.Shape).And.Be(RatingViewFillOption.Shape);
		ratingView.CustomShapePath.Should().BeNull();
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
		receivedEvents.Should().ContainSingle();
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
		receivedEvents.Should().ContainSingle();
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
		receivedEvents.Should().BeEmpty();
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
	public void Null_EmptyShapeColor()
	{
		RatingView ratingView = new();
		ratingView.EmptyShapeColor.Should().NotBeNull();
		ratingView.EmptyShapeColor = null;
		ratingView.EmptyShapeColor.Should().BeOfType<Color>().And.Be(Colors.Transparent);
	}

	[Fact]
	public void Null_FillColor()
	{
		RatingView ratingView = new();
		ratingView.FillColor.Should().NotBeNull();
		ratingView.FillColor = null;
		ratingView.FillColor.Should().BeOfType<Color>().And.Be(Colors.Transparent);
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
		RatingView ratingView = new();
		ratingView.CustomShapePath.Should().BeNull();
		ratingView.CustomShapePath = customShape;
		ratingView.CustomShapePath.Should().Be(customShape);
	}

	[Fact]
	public void Properties_Change_CustomShape_ShapeNotCustom()
	{
		RatingView ratingView = new()
		{
			Shape = RatingViewShape.Heart
		};
		ratingView.Shape.Should().Be(RatingViewShape.Heart);
		ratingView.CustomShapePath = customShape;
		ratingView.CustomShapePath.Should().Be(customShape);
		ratingView.Shape.Should().Be(RatingViewShape.Heart);
	}

	[Fact]
	public void Properties_Change_EmptyShapeColor_Item()
	{
		const double rating = 1.5;
		const byte maximumRating = 7;
		var emptyShapeColor = Colors.Snow;
		RatingView ratingView = new()
		{
			MaximumRating = maximumRating,
			Rating = rating,
			FillOption = RatingViewFillOption.Background
		};
		ratingView.EmptyShapeColor.Should().NotBe(emptyShapeColor);
		ratingView.EmptyShapeColor = emptyShapeColor;
		ratingView.EmptyShapeColor.Should().Be(emptyShapeColor);

		var emptyRatingItem = (Microsoft.Maui.Controls.Shapes.Path)GetItemShape(ratingView, maximumRating - 1).GetVisualTreeDescendants()[0];
		emptyRatingItem.Fill.Should().Be(new SolidColorBrush(emptyShapeColor));
	}

	[Fact]
	public void Properties_Change_EmptyShapeColor_Shape()
	{
		const double rating = 1.5;
		const byte maximumRating = 7;
		var emptyShapeColor = Colors.Snow;
		RatingView ratingView = new()
		{
			MaximumRating = maximumRating,
			Rating = rating
		};
		ratingView.EmptyShapeColor.Should().NotBe(emptyShapeColor);
		ratingView.EmptyShapeColor = emptyShapeColor;
		ratingView.EmptyShapeColor.Should().Be(emptyShapeColor);
		var emptyRatingItem = GetItemShape(ratingView, maximumRating - 1);
		emptyRatingItem.Fill.Should().Be(new SolidColorBrush(emptyShapeColor));
	}

	[Fact]
	public void Properties_Change_FillColor_Item()
	{
		const double rating = 1.5;
		const byte maximumRating = 7;
		var FillColor = Colors.Snow;
		var emptyShapeColor = Colors.Firebrick;
		var backgroundColor = Colors.DarkGreen;
		RatingView ratingView = new()
		{
			MaximumRating = maximumRating,
			Rating = rating,
			FillOption = RatingViewFillOption.Background
		};
		ratingView.FillColor.Should().NotBe(FillColor);
		ratingView.BackgroundColor.Should().BeNull();
		ratingView.BackgroundColor = backgroundColor;
		ratingView.EmptyShapeColor = emptyShapeColor;
		ratingView.FillColor = FillColor;
		ratingView.FillColor.Should().Be(FillColor);
		var filledRatingShape = GetItemShape(ratingView, (int)Math.Floor(rating));
		filledRatingShape.Fill.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(emptyShapeColor));
		var filledRatingItem = (Border)ratingView.RatingLayout.Children[0];
		filledRatingItem.Background.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(FillColor));
		var partialFilledRatingItem = (Border)ratingView.RatingLayout.Children[(int)Math.Floor(rating)];
		partialFilledRatingItem.Background.Should().BeOfType<LinearGradientBrush>();
		var emptyFilledRatingItem = (Border)ratingView.RatingLayout.Children[maximumRating - 1]; // Check the last one, as this is where we expect the background colour to be set
		emptyFilledRatingItem.Background.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(backgroundColor));
	}

	[Fact]
	public void Properties_Change_FillColor_Shape()
	{
		const double rating = 1.5;
		const byte maximumRating = 7;
		var FillColor = Colors.Snow;
		RatingView ratingView = new()
		{
			MaximumRating = maximumRating,
			Rating = rating
		};
		ratingView.FillColor.Should().NotBe(FillColor);
		ratingView.FillColor = FillColor;
		ratingView.FillColor.Should().Be(FillColor);
		var filledRatingItem = GetItemShape(ratingView, 0);
		filledRatingItem.Fill.Should().Be(new SolidColorBrush(FillColor));
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
		ratingView.ShapePadding.Should().NotBe(itemPadding);
		ratingView.ShapePadding = itemPadding;
		ratingView.ShapePadding.Should().Be(itemPadding);
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
		const RatingViewFillOption ratingFill = RatingViewFillOption.Background;
		RatingView ratingView = new();
		ratingView.FillOption.Should().NotBe(ratingFill);
		ratingView.FillOption = ratingFill;
		ratingView.FillOption.Should().Be(ratingFill);
	}

	[Theory]
	[InlineData(RatingViewShape.Heart)]
	[InlineData(RatingViewShape.Circle)]
	[InlineData(RatingViewShape.Like)]
	[InlineData(RatingViewShape.Dislike)]
	[InlineData(RatingViewShape.Custom)]
	public void Properties_Change_Shape(RatingViewShape expectedShape)
	{
		var ratingView = new RatingView
		{
			CustomShapePath = customShape
		};

		ratingView.Shape.Should().NotBe(expectedShape);
		ratingView.Shape = expectedShape;
		ratingView.Shape.Should().Be(expectedShape);
	}

	[Fact]
	public void InvalidOperationExceptionThrownWhenUsingNullCustomShapePath()
	{
		Assert.Throws<InvalidOperationException>(() => new RatingView { Shape = RatingViewShape.Custom });
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
		const int itsmShapeDiameter = 73;
		RatingView ratingView = new();
		ratingView.ShapeDiameter.Should().NotBe(itsmShapeDiameter);
		ratingView.ShapeDiameter = itsmShapeDiameter;
		ratingView.ShapeDiameter.Should().Be(itsmShapeDiameter);

		var firstRatingItem = GetItemShape(ratingView, 0);
		firstRatingItem.WidthRequest.Should().Be(itsmShapeDiameter);
		firstRatingItem.HeightRequest.Should().Be(itsmShapeDiameter);
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

		ratingView.Rating.Should().Be(rating);
		ratingView.MaximumRating.Should().Be(maximumRating);
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

		ratingView.ControlTemplate.Should().NotBeNull();
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

		child.Content.Should().NotBeNull();
		child.Content.GetVisualTreeDescendants()[0].Should().BeOfType<Microsoft.Maui.Controls.Shapes.Path>();
	}

	[Fact]
	public void ViewStructure_ItemChild_Path_Star()
	{
		RatingView ratingView = new();
		ratingView.RatingLayout.Children[0].Should().BeOfType<Border>();
		var child = (Border)ratingView.RatingLayout.Children[0];

		child.Content.Should().NotBeNull();

		var shape = (Microsoft.Maui.Controls.Shapes.Path)child.Content.GetVisualTreeDescendants()[0];
		shape.GetPath().Should().Be("M9 11.3l3.71 2.7-1.42-4.36L15 7h-4.55L9 2.5 7.55 7H3l3.71 2.64L5.29 14z");
	}

	[Fact]
	public void ViewStructure_ItemFill_Colors()
	{
		var FillColor = Colors.Red;
		var emptyShapeColor = Colors.Grey;
		var backgroundColor = Colors.CornflowerBlue;
		RatingView ratingView = new()
		{
			Rating = 0,
			MaximumRating = 3,
			FillOption = RatingViewFillOption.Background,
			FillColor = FillColor,
			EmptyShapeColor = emptyShapeColor,
			BackgroundColor = backgroundColor
		};
		ratingView.Rating = 1.5;
		var filledRatingItem = (Border)ratingView.RatingLayout.Children[0];
		var partialFilledRatingItem = (Border)ratingView.RatingLayout.Children[1];
		var emptyFilledRatingItem = (Border)ratingView.RatingLayout.Children[2];

		filledRatingItem.Content.Should().NotBeNull();
		filledRatingItem.Background.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(FillColor));
		((Shape)filledRatingItem.Content).Fill.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(emptyShapeColor));

		partialFilledRatingItem.Content.Should().NotBeNull();
		partialFilledRatingItem.Background.Should().BeOfType<LinearGradientBrush>();
		((Shape)partialFilledRatingItem.Content).Fill.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(emptyShapeColor));

		emptyFilledRatingItem.Content.Should().NotBeNull();
		emptyFilledRatingItem.Background.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(backgroundColor));
		((Shape)emptyFilledRatingItem.Content).Fill.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(emptyShapeColor));
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
			ShapePadding = expectedItemPadding,
		};
		var firstItem = (Border)ratingView.RatingLayout.Children[0];
		firstItem.Padding.Should().Be(expectedItemPadding);
	}

	[Fact]
	public void ViewStructure_ShapeFill_Colors()
	{
		var FillColor = Colors.Red;
		var emptyShapeColor = Colors.Grey;
		RatingView ratingView = new()
		{
			Rating = 1.5,
			MaximumRating = 3,
			FillOption = RatingViewFillOption.Shape,
			FillColor = FillColor,
			EmptyShapeColor = emptyShapeColor
		};
		var filledRatingItem = GetItemShape(ratingView, 0);
		var partialFilledRatingItem = GetItemShape(ratingView, 1);
		var emptyFilledRatingItem = GetItemShape(ratingView, 2);
		filledRatingItem.Fill.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(FillColor));
		emptyFilledRatingItem.Fill.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(emptyShapeColor));
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
		border.Content.Should().NotBeNull();

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