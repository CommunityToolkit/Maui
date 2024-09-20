// Ignore Spelling: color
using System.ComponentModel;

namespace CommunityToolkit.Maui.Core;

/// <summary>RatingView interface.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IRatingViewShape
{
	/// <summary>Gets a value indicating the rating item shape size.</summary>
	double ItemShapeSize { get; }

	/// <summary>Gets a value indicating the custom rating view shape path.</summary>
	string? CustomShape { get; }

	/// <summary>Gets a value indicating the Rating View shape.</summary>
	RatingViewShape Shape { get; }

	/// <summary>Gets a value indicating the Rating View item padding.</summary>
	Thickness ItemPadding { get; }

	/// <summary>Gets a value indicating the rating item shape border thickness</summary>
	double ShapeBorderThickness { get; }

	/// <summary>Get a value indicating the rating item shape border color.</summary>
	Color ShapeBorderColor { get; }

	/// <summary>Get a value indicating the rating item background empty color.</summary>
	Color EmptyColor { get; }

	/// <summary>Get a value indicating the rating item background filled color.</summary>
	Color FilledColor { get; }

	/// <summary>Action when <see cref="EmptyColor"/> changes.</summary>
	/// <param name="oldValue">Old empty background color.</param>
	/// <param name="newValue">New empty background color.</param>
	void OnEmptyColorPropertyChanged(Color oldValue, Color newValue);

	/// <summary>Action when <see cref="FilledColor"/> changes.</summary>
	/// <param name="oldValue">Old filled background color.</param>
	/// <param name="newValue">New filled background color.</param>
	void OnFilledColorPropertyChanged(Color oldValue, Color newValue);

	/// <summary>Action when <see cref="CustomShape"/> changes.</summary>
	/// <param name="oldValue">Old shape path.</param>
	/// <param name="newValue">New shape path.</param>
	void OnCustomShapePropertyChanged(string? oldValue, string? newValue);

	/// <summary>Action when <see cref="ItemPadding"/> changes.</summary>
	/// <param name="oldValue">Old padding thickness.</param>
	/// <param name="newValue">New padding thickness</param>
	void OnItemPaddingPropertyChanged(Thickness oldValue, Thickness newValue);

	/// <summary>Action when <see cref="Shape"/> changes.</summary>
	/// <param name="oldValue">Old shape.</param>
	/// <param name="newValue">New shape.</param>
	void OnItemShapePropertyChanged(RatingViewShape oldValue, RatingViewShape newValue);

	/// <summary>Action when <see cref="ShapeBorderColor"/> changes.</summary>
	/// <param name="oldValue">Old shape border color.</param>
	/// <param name="newValue">New shape border color.</param>
	void OnItemShapeBorderColorChanged(Color oldValue, Color newValue);

	/// <summary>Action when <see cref="ShapeBorderThickness"/> changes.</summary>
	/// <param name="oldValue">Old shape border thickness.</param>
	/// <param name="newValue">New shape border thickness.</param>
	void OnItemShapeBorderThicknessChanged(double oldValue, double newValue);

	/// <summary>Action when <see cref="ItemShapeSize"/> changes.</summary>
	/// <param name="oldValue">Old item shape size.</param>
	/// <param name="newValue">New item shape size.</param>
	void OnItemShapeSizeChanged(double oldValue, double newValue);

	/// <summary>Retrieves the default item shape size value.</summary>
	/// <returns>The item shape <see cref="double"/> size.</returns>
	double ItemShapeSizeDefaultValueCreator();

	/// <summary>Retrieves the default shape value.</summary>
	/// <returns>The shape <see cref="RatingViewShape"/>.</returns>
	RatingViewShape ShapeDefaultValueCreator();

	/// <summary>Retrieves the default shape border color value.</summary>
	/// <returns>The item shape border <see cref="Color"/>.</returns>
	Color ItemShapeBorderColorDefaultValueCreator();

	/// <summary>Retrieves the default shape border thickness value.</summary>
	/// <returns>The item shape border <see cref="double"/>.</returns>
	double ItemShapeBorderThicknessDefaultValueCreator();

	/// <summary>Retrieves the default rating item padding value.</summary>
	/// <returns>The rating item padding <see cref="Thickness"/>.</returns>
	Thickness ItemPaddingDefaultValueCreator();

	/// <summary>Retrieves the default background color for an empty rating item.</summary>
	/// <returns>The empty background color.</returns>
	Color EmptyColorDefaultValueCreator();

	/// <summary>Retrieves the default background color for an filled rating item.</summary>
	/// <returns>The filled background color.</returns>
	Color FilledColorDefaultValueCreator();
}

/// <summary>Rating view shape enumerator.</summary>
public enum RatingViewShape
{
	/// <summary>A star rating shape.</summary>
	Star,

	/// <summary>A heart rating shape.</summary>
	Heart,

	/// <summary>A circle rating shape.</summary>
	Circle,

	/// <summary>A like/thumbs up rating shape.</summary>
	Like,

	/// <summary>A dislike/thumbs down rating shape.</summary>
	Dislike,

	/// <summary>A custom rating shape.</summary>
	Custom
}