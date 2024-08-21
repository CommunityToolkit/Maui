using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;
using FluentAssertions;
using Xunit;
using Path = Microsoft.Maui.Controls.Shapes.Path;

namespace CommunityToolkit.Maui.UnitTests.Views.RatingView;

public class RatingViewTests : BaseHandlerTest
{
	[Fact]
	public void DefaultInitialization_ShouldHaveCorrectDefaultValues()
	{
		// Arrange
		var ratingView = new Maui.Views.RatingView();

		// Assert
		Assert.Equal(RatingViewDefaults.BorderColor, ratingView.BorderColor);
		Assert.Equal(RatingViewDefaults.BorderThickness, ratingView.BorderThickness);
		Assert.Equal(RatingViewDefaults.CurrentRating, ratingView.CurrentRating);
		Assert.Equal(RatingViewDefaults.EmptyBackgroundColor, ratingView.EmptyBackgroundColor);
		Assert.Equal(RatingViewDefaults.FilledBackgroundColor, ratingView.FilledBackgroundColor);
		Assert.Equal(RatingViewDefaults.IsEnabled, ratingView.IsEnabled);
		Assert.Equal(RatingViewDefaults.MaximumRating, ratingView.MaximumRating);
		Assert.Equal(RatingViewDefaults.Size, ratingView.Size);
		Assert.Equal(RatingViewDefaults.Spacing, ratingView.Spacing);
	}

	[Fact]
	public void OnControlInitialized_ShouldCreateCorrectNumberOfShapes()
	{
		const int maximumRating = 3;
		Maui.Views.RatingView ratingView = new();
		ratingView.Control?.ColumnDefinitions.Count.Should().Be(RatingViewDefaults.MaximumRating);
		ratingView.Control?.Children.Count.Should().Be(RatingViewDefaults.MaximumRating);

		ratingView.MaximumRating = maximumRating;
		ratingView.Control?.ColumnDefinitions.Count.Should().Be(maximumRating);
		ratingView.Control?.Children.Count.Should().Be(maximumRating);
	}

	[Fact]
	public void PropertyChangedEvent_ShouldBeRaised_WhenCurrentRatingChanges()
	{
		const double currentRating = 3.5;
		Maui.Views.RatingView ratingView = new();
		ratingView.CurrentRating.Should().Be(RatingViewDefaults.CurrentRating);

		bool signaled = false;
		ratingView.PropertyChanged += (sender, e) =>
		{
			if (e.PropertyName == "CurrentRating")
			{
				signaled = true;
			}
		};

		ratingView.CurrentRating = currentRating;
		ratingView.CurrentRating.Should().Be(currentRating);
		signaled.Should().BeTrue();
	}

	[Fact]
	public void Draw_ShouldCreateCorrectNumberOfShapes()
	{
		const int maximumRating = 3;
		var ratingView = new Maui.Views.RatingView
		{
			MaximumRating = maximumRating
		};

		ratingView.Control?.ColumnDefinitions.Count.Should().Be(maximumRating);
		ratingView.Control?.Children.Count.Should().Be(maximumRating);
	}

	[Fact]
	public void ShapeProperty_ShouldSetShapeCorrectly()
	{
		Maui.Views.RatingView ratingView = new()
		{
			Shape = RatingViewShape.Heart
		};

		ratingView.Shape.Should().Be(RatingViewShape.Heart);
	}
}