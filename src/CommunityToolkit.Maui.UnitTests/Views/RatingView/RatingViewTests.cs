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
		_ = ratingViewWithBinding.BindingContext.Should().BeNull();
		ratingViewWithBinding.BindingContext = vm;
		_ = ratingViewWithBinding.BindingContext.Should().Be(vm);
	}

	[Fact]
	public void Defaults_ItemDefaultsApplied()
	{
		RatingView ratingView = new();
		var firstItem = (Border)ratingView.Control!.Children[0];

		_ = firstItem.Should().BeOfType<Border>();
		_ = firstItem.BackgroundColor.Should().BeNull();
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
		var firstItemShape = (Microsoft.Maui.Controls.Shapes.Path)((Border)ratingView.Control!.Children[0]).Content!.GetVisualTreeDescendants()[0];
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
		_ = ratingView.Rating.Should().Be(RatingViewDefaults.DefaultRating);
		_ = ratingView.EmptyColor.Should().BeOfType<Color>().And.Be(RatingViewDefaults.EmptyColor);
		_ = ratingView.FilledColor.Should().BeOfType<Color>().And.Be(RatingViewDefaults.FilledColor);
		_ = ratingView.IsReadOnly.Should().BeFalse().And.Be(RatingViewDefaults.IsReadOnly);
		_ = ratingView.ItemPadding.Should().BeOfType<Thickness>().And.Be(RatingViewDefaults.ItemPadding);
		_ = ratingView.ItemShapeSize.Should().Be(RatingViewDefaults.ItemShapeSize);
		_ = ratingView.MaximumRating.Should().Be(RatingViewDefaults.MaximumRating);
		_ = ratingView.ItemShape.Should().BeOneOf(RatingViewDefaults.Shape).And.Be(RatingViewDefaults.Shape);
		_ = ratingView.ShapeBorderColor.Should().BeOfType<Color>().And.Be(RatingViewDefaults.ShapeBorderColor);
		_ = ratingView.ShapeBorderThickness.Should().Be(RatingViewDefaults.ShapeBorderThickness);
		_ = ratingView.Spacing.Should().Be(RatingViewDefaults.Spacing);
		_ = ratingView.RatingFill.Should().BeOneOf(RatingFillElement.Shape).And.Be(RatingFillElement.Shape);
		_ = ratingView.CustomItemShape.Should().BeNullOrEmpty();
	}

	[Fact]
	public void Events_Border_TapGestureRecognizer_SingleRating_Toggled()
	{
		RatingView ratingView = new()
		{
			MaximumRating = 1
		};
		var child = (Border)ratingView.Control!.Children[0];
		var tapGestureRecognizer = (TapGestureRecognizer)child.GestureRecognizers[0];
		tapGestureRecognizer.SendTapped(child);
		_ = ratingView.Rating.Should().Be(1);

		tapGestureRecognizer.SendTapped(child);
		_ = ratingView.Rating.Should().Be(0);
	}

	[Fact]
	public void Events_Border_TapGestureRecognizer_Tapped()
	{
		var handlerTappedCount = 0;
		RatingView ratingView = new();
		_ = ratingView.Rating.Should().Be(handlerTappedCount);
		var child = (Border)ratingView.Control!.Children[0];
		var tgr = (TapGestureRecognizer)child.GestureRecognizers[0];
		tgr.SendTapped(child);
		handlerTappedCount++;
		_ = ratingView.Rating.Should().Be(handlerTappedCount);
	}

	[Fact]
	public void Events_Command()
	{
		RatingView ratingView = new();
		bool commandHasBeenExecuted = false;
		ratingView.RatingChangedCommand = new Command<string>((_) => commandHasBeenExecuted = true);
		ratingView.Rating = 3;
		_ = commandHasBeenExecuted.Should().BeFalse();
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
		_ = receivedEvents.Should().HaveCount(1);
		_ = receivedEvents[0].Rating.Should().Be(expectedRating);

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
		_ = ratingView.Rating.Should().Be(maximumRating);
		_ = signaled.Should().BeTrue();
	}

	[Fact]
	public void Events_RatingChanged_ShouldBeRaised_RatingPropertyChanged_NotReadOnly()
	{
		const double currentRating = 3.5;
		RatingView ratingView = new();
		_ = ratingView.Rating.Should().Be(RatingViewDefaults.DefaultRating);
		var signaled = false;
		ratingView.RatingChanged += (sender, e) => signaled = true;
		ratingView.Rating = currentRating;
		_ = ratingView.Rating.Should().Be(currentRating);
		_ = signaled.Should().BeTrue();
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
		_ = ratingView.Rating.Should().Be(currentRating);
		_ = signaled.Should().BeFalse();
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
		_ = ratingView.Rating.Should().Be(currentRating);
		_ = signaled.Should().BeFalse();
	}

	[Fact]
	public void Events_RatingChanged_ShouldNotBeRaised_MaximumRatingPropertyChanged_LReadOnly()
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
		_ = ratingView.Rating.Should().Be(maximumRating);
		_ = signaled.Should().BeFalse();
	}

	[Fact]
	public void Events_RatingChanged_ShouldNotBeRaised_RatingPropertyChanged_ReadOnly()
	{
		const double currentRating = 3.5;
		RatingView ratingView = new()
		{
			IsReadOnly = true
		};

		var signaled = false;
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
		_ = receivedEvents.Should().ContainSingle();
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
	public void MaximumRatingViewThrowsArgumentOutOfRangeExceptionWhenOutsideLowerBounds()
	{
		_ = Assert.Throws<ArgumentOutOfRangeException>(() => new RatingView().MaximumRating = 0);
	}

	[Fact]
	public void MaximumRatingViewThrowsArgumentOutOfRangeExceptionWhenOutsideUpperBounds()
	{
		_ = Assert.Throws<ArgumentOutOfRangeException>(() => new RatingView().MaximumRating = RatingViewDefaults.MaximumRatingLimit + 1);
	}

	[Fact]
	public void Null_Control_OnRatingChanged()
	{
		const int initialRating = 3;
		const int controlNullRating = 4;

		RatingView ratingView = new()
		{
			Rating = initialRating,
		};
		bool signaled = false;
		ratingView.RatingChanged += (sender, e) => signaled = true;
		ratingView.Control = null;
		ratingView.Rating = controlNullRating;
		_ = ratingView.Rating.Should().Be(controlNullRating);
		_ = signaled.Should().BeFalse();
	}

	[Fact]
	public void Null_Control_TapGestureRecognizer()
	{
		RatingView ratingView = new()
		{
			MaximumRating = 1
		};
		Border child = (Border)ratingView.Control!.Children[0];
		TapGestureRecognizer tapGestureRecognizer = (TapGestureRecognizer)child.GestureRecognizers[0];
		ratingView.Control = null;
		tapGestureRecognizer.SendTapped(child);
		_ = ratingView.Rating.Should().Be(0);
	}

	[Fact]
	public void Null_EmptyColor()
	{
		RatingView ratingView = new();
		_ = ratingView.EmptyColor.Should().NotBeNull();
		ratingView.EmptyColor = null;
		_ = ratingView.EmptyColor.Should().BeOfType<Color>().And.Be(Colors.Transparent);
	}

	[Fact]
	public void Null_FilledColor()
	{
		RatingView ratingView = new();
		_ = ratingView.FilledColor.Should().NotBeNull();
		ratingView.FilledColor = null;
		_ = ratingView.FilledColor.Should().BeOfType<Color>().And.Be(Colors.Transparent);
	}

	[Fact]
	public void Null_ShapeBorderColor()
	{
		RatingView ratingView = new();
		_ = ratingView.ShapeBorderColor.Should().NotBeNull();
		ratingView.ShapeBorderColor = null;
		_ = ratingView.ShapeBorderColor.Should().BeOfType<Color>().And.Be(Colors.Transparent);
	}

	[Fact]
	public void Properties_Change_CustomShape()
	{
		const string customShape = "M 12 0C5.388 0 0 5.388 0 12s5.388 12 12 12 12-5.38 12-12c0-6.612-5.38-12-12-12z";
		RatingView ratingView = new();
		_ = ratingView.CustomItemShape.Should().BeNullOrEmpty();
		ratingView.CustomItemShape = customShape;
		_ = ratingView.CustomItemShape.Should().Be(customShape);
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
		_ = ratingView.ItemShape.Should().Be(RatingViewShape.Custom);
		_ = ratingView.CustomItemShape.Should().Be(customShape);
		ratingView.CustomItemShape = customShapes!;
		_ = ratingView.ItemShape.Should().Be(RatingViewShape.Star);
	}

	[Fact]
	public void Properties_Change_CustomShape_ShapeCustom()
	{
		const string customShape = "M 12 0C5.388 0 0 5.388 0 12s5.388 12 12 12 12-5.38 12-12c0-6.612-5.38-12-12-12z";
		RatingView ratingView = new()
		{
			ItemShape = RatingViewShape.Custom
		};
		_ = ratingView.ItemShape.Should().Be(RatingViewShape.Custom);
		ratingView.CustomItemShape = customShape;
		_ = ratingView.CustomItemShape.Should().Be(customShape);
		_ = ratingView.ItemShape.Should().Be(RatingViewShape.Custom);
	}

	[Fact]
	public void Properties_Change_CustomShape_ShapeNotCustom()
	{
		const string customShape = "M 12 0C5.388 0 0 5.388 0 12s5.388 12 12 12 12-5.38 12-12c0-6.612-5.38-12-12-12z";
		RatingView ratingView = new()
		{
			ItemShape = RatingViewShape.Heart
		};
		_ = ratingView.ItemShape.Should().Be(RatingViewShape.Heart);
		ratingView.CustomItemShape = customShape;
		_ = ratingView.CustomItemShape.Should().Be(customShape);
		_ = ratingView.ItemShape.Should().Be(RatingViewShape.Heart);
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
		_ = ratingView.EmptyColor.Should().NotBe(emptyColor);
		ratingView.EmptyColor = emptyColor;
		_ = ratingView.EmptyColor.Should().Be(emptyColor);
		var emptyRatingItem = (Microsoft.Maui.Controls.Shapes.Path)((Border)ratingView.Control!.Children[maximumRating - 1]).Content!.GetVisualTreeDescendants()[0];
		_ = emptyRatingItem.Fill.Should().Be(new SolidColorBrush(emptyColor));
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
		_ = ratingView.EmptyColor.Should().NotBe(emptyColor);
		ratingView.EmptyColor = emptyColor;
		_ = ratingView.EmptyColor.Should().Be(emptyColor);
		var emptyRatingItem = (Microsoft.Maui.Controls.Shapes.Path)((Border)ratingView.Control!.Children[maximumRating - 1]).Content!.GetVisualTreeDescendants()[0];
		_ = emptyRatingItem.Fill.Should().Be(new SolidColorBrush(emptyColor));
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
		_ = ratingView.FilledColor.Should().NotBe(filledColor);
		_ = ratingView.BackgroundColor.Should().BeNull();
		ratingView.BackgroundColor = backgroundColor;
		ratingView.EmptyColor = emptyColor;
		ratingView.FilledColor = filledColor;
		_ = ratingView.FilledColor.Should().Be(filledColor);
		var filledRatingShape = (Microsoft.Maui.Controls.Shapes.Path)((Border)ratingView.Control!.Children[(int)Math.Floor(rating)]).Content!.GetVisualTreeDescendants()[0];
		_ = filledRatingShape.Fill.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(emptyColor));
		var filledRatingItem = (Border)ratingView.Control!.Children[0];
		_ = filledRatingItem.Background.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(filledColor));
		var partialFilledRatingItem = (Border)ratingView.Control!.Children[(int)Math.Floor(rating)];
		_ = partialFilledRatingItem.Background.Should().BeOfType<LinearGradientBrush>();
		var emptyFilledRatingItem = (Border)ratingView.Control!.Children[maximumRating - 1]; // Check the last one, as this is where we expect the background colour to be set
		_ = emptyFilledRatingItem.Background.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(backgroundColor));
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
		_ = ratingView.FilledColor.Should().NotBe(filledColor);
		ratingView.FilledColor = filledColor;
		_ = ratingView.FilledColor.Should().Be(filledColor);
		var filledRatingItem = (Microsoft.Maui.Controls.Shapes.Path)((Border)ratingView.Control!.Children[0]).Content!.GetVisualTreeDescendants()[0];
		_ = filledRatingItem.Fill.Should().Be(new SolidColorBrush(filledColor));
	}

	[Fact]
	public void Properties_Change_IsReadOnly()
	{
		RatingView ratingView = new();
		_ = ratingView.IsReadOnly.Should().BeFalse();
		ratingView.IsReadOnly = true;
		_ = ratingView.IsReadOnly.Should().BeTrue();
		ratingView.IsReadOnly = false;
		_ = ratingView.IsReadOnly.Should().BeFalse();
	}

	[Fact]
	public void Properties_Change_IsReadOnly_GestureRecognizers()
	{
		RatingView ratingView = new();
		_ = ratingView.IsReadOnly.Should().BeFalse();
		for (var i = 0; i < ratingView.Control?.Children.Count; i++)
		{
			var child = (Border)ratingView.Control.Children[i];
			_ = child.GestureRecognizers.Count.Should().Be(1);
		}

		ratingView.IsReadOnly = true;
		_ = ratingView.IsReadOnly.Should().BeTrue();
		for (var i = 0; i < ratingView.Control?.Children.Count; i++)
		{
			var child = (Border)ratingView.Control.Children[i];
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
		var firstItem = (Border)ratingView.Control!.Children[0];
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
		const double rating = 2.3;
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
		_ = ratingView.ItemShape.Should().NotBe(expectedShape);
		ratingView.ItemShape = expectedShape;
		_ = ratingView.ItemShape.Should().Be(expectedShape);
	}

	[Fact]
	public void Properties_Change_ShapeBorderColor()
	{
		var shapeBorderColor = Colors.Snow;
		Brush brush = new SolidColorBrush(shapeBorderColor);
		RatingView ratingView = new();
		_ = ratingView.ShapeBorderColor.Should().NotBe(shapeBorderColor);
		ratingView.ShapeBorderColor = shapeBorderColor;
		_ = ratingView.ShapeBorderColor.Should().Be(shapeBorderColor);
		var firstRatingItem = (Microsoft.Maui.Controls.Shapes.Path)((Border)ratingView.Control!.Children[0]).Content!.GetVisualTreeDescendants()[0];
		_ = firstRatingItem.Stroke.Should().BeOfType<SolidColorBrush>().And.Be(brush);
	}

	[Fact]
	public void Properties_Change_ShapeBorderThickness()
	{
		const double shapeBorderThickness = 7.3;
		RatingView ratingView = new();
		_ = ratingView.ShapeBorderThickness.Should().NotBe(shapeBorderThickness);
		ratingView.ShapeBorderThickness = shapeBorderThickness;
		_ = ratingView.ShapeBorderThickness.Should().Be(shapeBorderThickness);
		var firstRatingItem = (Microsoft.Maui.Controls.Shapes.Path)((Border)ratingView.Control!.Children[0]).Content!.GetVisualTreeDescendants()[0];
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
		var firstRatingItem = (Microsoft.Maui.Controls.Shapes.Path)((Border)ratingView.Control!.Children[0]).Content!.GetVisualTreeDescendants()[0];
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
		var control = ratingView.Control;
		_ = control.Should().NotBeNull();
		_ = control!.Spacing.Should().Be(spacing);
	}

	[Fact]
	public void Properties_MaximumRating_KeptInRange()
	{
		RatingView ratingView = new();
		const byte minMaximumRating = 1;
		const byte maxMaximumRating = RatingViewDefaults.MaximumRatingLimit;
		ratingView.MaximumRating = minMaximumRating;
		_ = ratingView.MaximumRating.Should().Be(1);
		ratingView.MaximumRating = maxMaximumRating;
		_ = ratingView.MaximumRating.Should().Be(RatingViewDefaults.MaximumRatingLimit);
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
		_ = RatingView.MaximumRatingProperty.ValidateValue(ratingView, 0).Should().BeFalse();
		_ = RatingView.MaximumRatingProperty.ValidateValue(ratingView, RatingViewDefaults.MaximumRatingLimit + 1).Should().BeFalse();
		_ = RatingView.MaximumRatingProperty.ValidateValue(ratingView, 1).Should().BeTrue();
	}

	[Fact]
	public void Properties_Rating_Validator()
	{
		RatingView ratingView = new();
		_ = RatingView.RatingProperty.ValidateValue(ratingView, -1.0).Should().BeFalse();
		_ = RatingView.RatingProperty.ValidateValue(ratingView, (double)(RatingViewDefaults.MaximumRatingLimit + 1)).Should().BeFalse();
		_ = RatingView.RatingProperty.ValidateValue(ratingView, 0.1).Should().BeTrue();
	}

	[Fact]
	public void RatingViewDoesNotThrowsArgumentOutOfRangeExceptionWhenRatingSetBeforeMaximumRating()
	{
		const int maximumRating = 12;
		const int rating = 7;

		RatingView ratingView = new()
		{
			MaximumRating = maximumRating,
			Rating = rating,
		};

		Assert.Equal(rating, ratingView.Rating);
		Assert.Equal(maximumRating, ratingView.MaximumRating);
	}

	[Fact]
	public void RatingViewThrowsArgumentOutOfRangeExceptionWhenOutsideLowerBounds()
	{
		RatingView ratingView = new();
		_ = Assert.Throws<ArgumentOutOfRangeException>(() => ratingView.Rating = 0 - double.Epsilon);
	}

	[Fact]
	public void RatingViewThrowsArgumentOutOfRangeExceptionWhenOutsideUpperBounds()
	{
		RatingView ratingView = new();
		_ = Assert.Throws<ArgumentOutOfRangeException>(() => ratingView.Rating = ratingView.MaximumRating + 1);
	}

	[Fact]
	public void RatingViewThrowsArgumentOutOfRangeExceptionWhenRatingSetBeforeMaximumRating()
	{
		_ = Assert.Throws<ArgumentOutOfRangeException>(() => new RatingView
		{
			Rating = 7,
			MaximumRating = 12
		});
	}

	[Fact]
	public void ShapeBorderThicknessShouldThrowArgumentOutOfRangeExceptionForNegativeNumbers()
	{
		_ = Assert.Throws<ArgumentOutOfRangeException>(() => new RatingView
		{
			ShapeBorderThickness = -1
		});
	}

	[Fact]
	public void ViewStructure_Control_IsHorizontalStackLayout()
	{
		RatingView ratingView = new();
		_ = ratingView.Control.Should().BeOfType<HorizontalStackLayout>();
	}

	[Fact]
	public void ViewStructure_CorrectNumberOfChildren()
	{
		const int maximumRating = 3;
		RatingView ratingView = new()
		{
			MaximumRating = maximumRating
		};

		Assert.NotNull(ratingView.Control);
		_ = ratingView.Control.GetVisualTreeDescendants().Should().HaveCount((maximumRating * 2) + 1);
		_ = ratingView.Control.Children.Should().HaveCount(maximumRating);
	}

	[Fact]
	public void ViewStructure_Item_IsBorder()
	{
		RatingView ratingView = new();
		_ = ratingView.Control.Should().NotBeNull();
		_ = ratingView.Control!.Children[0].Should().NotBeNull();
		_ = ratingView.Control!.Children[0].Should().BeOfType<Border>();
	}

	[Fact]
	public void ViewStructure_ItemChild_IsPath()
	{
		RatingView ratingView = new();
		_ = ratingView.Control!.Children[0].Should().BeOfType<Border>();
		var child = (Border)ratingView.Control!.Children[0];
		_ = child.Content.Should().NotBeNull();
		_ = child.Content!.GetVisualTreeDescendants()[0].Should().BeOfType<Microsoft.Maui.Controls.Shapes.Path>();
	}

	[Fact]
	public void ViewStructure_ItemChild_Path_Star()
	{
		RatingView ratingView = new();
		_ = ratingView.Control!.Children[0].Should().BeOfType<Border>();
		var child = (Border)ratingView.Control!.Children[0];
		_ = child.Content.Should().NotBeNull();
		var shape = (Microsoft.Maui.Controls.Shapes.Path)child.Content!.GetVisualTreeDescendants()[0];
		_ = shape.GetPath().Should().Be(Core.Primitives.RatingViewShape.Star.PathData);
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
		var filledRatingItem = (Border)ratingView.Control!.Children[0];
		var partialFilledRatingItem = (Border)ratingView.Control!.Children[1];
		var emptyFilledRatingItem = (Border)ratingView.Control!.Children[2];
		_ = filledRatingItem.Background.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(filledColor));
		_ = ((Shape)filledRatingItem.Content!).Fill.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(emptyColor));
		_ = partialFilledRatingItem.Background.Should().BeOfType<LinearGradientBrush>();
		_ = ((Shape)partialFilledRatingItem.Content!).Fill.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(emptyColor));
		_ = emptyFilledRatingItem.Background.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(backgroundColor));
		_ = ((Shape)emptyFilledRatingItem.Content!).Fill.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(emptyColor));
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
		var firstItem = (Border)ratingView.Control!.Children[0];
		_ = firstItem.Padding.Should().Be(expectedItemPadding);
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
		var filledRatingItem = (Microsoft.Maui.Controls.Shapes.Path)((Border)ratingView.Control!.Children[0]).Content!.GetVisualTreeDescendants()[0];
		var partialFilledRatingItem = (Microsoft.Maui.Controls.Shapes.Path)((Border)ratingView.Control!.Children[1]).Content!.GetVisualTreeDescendants()[0];
		var emptyFilledRatingItem = (Microsoft.Maui.Controls.Shapes.Path)((Border)ratingView.Control!.Children[2]).Content!.GetVisualTreeDescendants()[0];
		_ = filledRatingItem.Fill.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(filledColor));
		_ = emptyFilledRatingItem.Fill.Should().BeOfType<SolidColorBrush>().And.Be(new SolidColorBrush(emptyColor));
		_ = partialFilledRatingItem.Fill.Should().BeOfType<LinearGradientBrush>();
	}

	[Fact]
	public void ViewStructure_Spacing()
	{
		RatingView ratingView = new();
		var rvControl = ratingView.Control!;
		_ = rvControl.Spacing.Should().Be(RatingViewDefaults.Spacing);
	}

	sealed class MockRatingViewViewModel : INotifyPropertyChanged
	{
		int maxRating = 0;

		public event PropertyChangedEventHandler? PropertyChanged;

		public int MaxRating
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
}