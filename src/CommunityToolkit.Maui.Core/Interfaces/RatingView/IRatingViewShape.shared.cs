using System.ComponentModel;

namespace CommunityToolkit.Maui.Core;

/// <summary>RatingView interface.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IRatingViewShape
{
	/// <summary>Gets a value indicating the custom rating view shape path.</summary>
	string? CustomShape { get; }

	/// <summary>Gets a value indicating the Rating View shape.</summary>
	RatingViewShape Shape { get; }

	/// <summary>Gets a value indicating the Rating View shape padding.</summary>
	Thickness ShapePadding { get; }

	/// <summary>Action when <see cref="CustomShape"/> changes.</summary>
	/// <param name="oldValue">Old shape path.</param>
	/// <param name="newValue">New shape path.</param>
	void OnCustomShapePropertyChanged(string? oldValue, string? newValue);

	/// <summary>Action when <see cref="ShapePadding"/> changes.</summary>
	/// <param name="oldValue">Old padding thickness.</param>
	/// <param name="newValue">New padding thickness</param>
	void OnShapePaddingPropertyChanged(Thickness oldValue, Thickness newValue);

	/// <summary>Action when <see cref="Shape"/> changes.</summary>
	/// <param name="oldValue">Old shape.</param>
	/// <param name="newValue">New shape.</param>
	void OnShapePropertyChanged(RatingViewShape oldValue, RatingViewShape newValue);

	/// <summary>Retrieves the default shape value.</summary>
	/// <returns>The shape <see cref="RatingViewShape"/>.</returns>
	RatingViewShape ShapeDefaultValueCreator();

	/// <summary>Retrieves the default shape padding value.</summary>
	/// <returns>The shape padding <see cref="Thickness"/>.</returns>
	Thickness ShapePaddingDefaultValueCreator();
}

/// <summary>Rating view shape enumerator.</summary>
public enum RatingViewShape
{
	/// <summary>A star rating shape.</summary>
	Star = 0,

	/// <summary>A heart rating shape.</summary>
	Heart = 1,

	/// <summary>A circle rating shape.</summary>
	Circle = 2,

	/// <summary>A like/thumbs up rating shape.</summary>
	Like = 3,

	/// <summary>A dislike/thumbs down rating shape.</summary>
	Dislike = 4,

	/// <summary>A custom rating shape.</summary>
	Custom = 99
}