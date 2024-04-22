using CommunityToolkit.Maui.Core.Primitives.Defaults;
using FluentAssertions;
using Microsoft.Maui.Controls.Shapes;
using Xunit;
using Path = Microsoft.Maui.Controls.Shapes.Path;

namespace CommunityToolkit.Maui.UnitTests.Views.RatingView;

public class RatingViewTests : BaseHandlerTest
{
	[Fact]
	public void DefaultInitialization_ShouldHaveCorrectDefaultValues()
	{
		// Arrange
		var ratingView = new Maui.Views.RatingView.RatingView();

		// Assert
		ratingView.CurrentRating.Should().Be(0.0);
		ratingView.MaximumRating.Should().Be(5);
		ratingView.Size.Should().Be(20.0);
		ratingView.FilledBackgroundColor.Should().BeEquivalentTo(Colors.Yellow);
		ratingView.EmptyBackgroundColor.Should().BeEquivalentTo(Colors.White);
		ratingView.StrokeThickness.Should().Be(7.0);
		ratingView.Spacing.Should().Be(10.0);
		ratingView.ShouldAllowRating.Should().BeFalse();
	}

	[Fact]
	public void OnControlInitialized_ShouldCreateCorrectNumberOfShapes()
	{
		// Arrange
		var ratingView = new Maui.Views.RatingView.RatingView();
		ratingView.MaximumRating = 3;

		// Act
		ratingView.InitializeShape();

		// Assert
		ratingView.Control?.ColumnDefinitions.Count.Should().Be(3);
		ratingView.Control?.Children.Count.Should().Be(3);
	}

	[Fact]
	public void PropertyChangedEvent_ShouldBeRaised_WhenCurrentRatingChanges()
	{
		// Arrange
		var ratingView = new Maui.Views.RatingView.RatingView();
		bool eventRaised = false;
		ratingView.PropertyChanged += (_, e) => { eventRaised = true; };

		// Act
		ratingView.CurrentRating = 3.5;

		// Assert
		eventRaised.Should().BeTrue();
	}

	[Fact]
	public void Draw_ShouldCreateCorrectNumberOfShapes()
	{
		// Arrange
		var ratingView = new Maui.Views.RatingView.RatingView
		{
			MaximumRating = 3
		};

		// Assert
		ratingView.Control?.ColumnDefinitions.Count.Should().Be(3);
		ratingView.Control?.Children.Count.Should().Be(3);
	}

	[Fact]
	public void ReDraw_ShouldRecreateShapes()
	{
		// Arrange
		var ratingView = new Maui.Views.RatingView.RatingView
		{
			MaximumRating = 3
		};

		// Act
		ratingView.ReDraw();

		// Assert
		ratingView.Control?.ColumnDefinitions.Count.Should().Be(3);
		ratingView.Control?.Children.Count.Should().Be(3);
	}

	[Fact]
	public void UpdateDraw_ShouldUpdateShapesCorrectly()
	{
		// Arrange
		var ratingView = new Maui.Views.RatingView.RatingView
		{
			MaximumRating = 3,
			CurrentRating = 2.5
		};

		// Act
		ratingView.UpdateDraw();

		// Assert
		ratingView.Control?.Children.Count.Should().Be(3);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
		(ratingView.Control?.Children[0] as Path).Fill.Should()
			.BeEquivalentTo(Brush.Yellow);
		(ratingView.Control?.Children[0] as Path).Stroke.Should()
			.BeEquivalentTo(Brush.Yellow);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
	}

	[Fact]
	public void InitializeShape_ShouldSetShapeAndControlColumnSpacing()
	{
		// Arrange
		var ratingView = new Maui.Views.RatingView.RatingView();

		// Act
		ratingView.InitializeShape();

		// Assert
		ratingView.Shape.Should().NotBeNull();
		ratingView.Control?.ColumnSpacing.Should().Be(ratingView.Spacing);
	}

	[Fact]
	public void OnShapeTapped_ShouldUpdateCurrentRatingCorrectly()
	{
		// Arrange
		var ratingView = new Maui.Views.RatingView.RatingView
		{
			MaximumRating = 3,
			ShouldAllowRating = true
		};

		Path tappedShape = new()
		{
			Data = (Geometry?)new PathGeometryConverter().ConvertFromInvariantString(RatingShape.Star.PathData)
		};

		// Act
		ratingView.OnShapeTapped(tappedShape, null);

		// Assert
		ratingView.CurrentRating.Should().Be(1);
	}

	[Fact]
	public void ShapeProperty_ShouldSetShapeCorrectly()
	{
		// Arrange
		var ratingView = new Maui.Views.RatingView.RatingView();

		// Act
		ratingView.Shape = RatingShape.Heart;

		// Assert
		ratingView.Shape.Should().Be(RatingShape.Heart);
	}
}