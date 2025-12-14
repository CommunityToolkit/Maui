// Ignore Spelling: color, bindable, colors

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Shapes;
using Path = Microsoft.Maui.Controls.Shapes.Path;

namespace CommunityToolkit.Maui.Views;

/// <summary>Rating view control.</summary>
public partial class RatingView : TemplatedView, IRatingView
{
	readonly WeakEventManager weakEventManager = new();

	///<summary>Instantiates <see cref="RatingView"/> .</summary>
	public RatingView()
	{
		RatingLayout.SetBinding<RatingView, object>(BindingContextProperty, static ratingView => ratingView.BindingContext, source: this);
		base.ControlTemplate = new ControlTemplate(() => RatingLayout);

		AddChildrenToLayout(0, MaximumRating);
	}

	/// <summary>Fires when <see cref="Rating"/> is changed.</summary>
	public event EventHandler<RatingChangedEventArgs> RatingChanged
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	/// <inheritdoc cref="ControlTemplate"/>
	public new ControlTemplate ControlTemplate => base.ControlTemplate; // Ensures the ControlTemplate is readonly, preventing users from breaking the HorizontalStackLayout

	/// <summary>
	/// Gets or sets the path data that defines a custom shape for rendering.
	/// </summary>
	/// <remarks>The path data should be provided in a format compatible with the rendering system, such as SVG path
	/// syntax. If the value is null or empty, no custom shape will be applied.</remarks>
	[BindableProperty(PropertyChangedMethodName = nameof(OnCustomShapePathPropertyChanged))]
	public partial string? CustomShapePath { get; set; }

	/// <summary>
	/// Gets or sets the padding applied to the shape's content area.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnShapePaddingPropertyChanged))]
	public partial Thickness ShapePadding { get; set; } = RatingViewDefaults.ShapePadding;

	/// <summary>
	/// Gets or sets the shape used to display each rating item in the view.
	/// </summary>
	/// <remarks>Changing this property updates the visual appearance of the rating items. The available shapes are
	/// defined by the <see cref="RatingViewShape"/> enumeration.</remarks>
	[BindableProperty(PropertyChangedMethodName = nameof(OnShapePropertyChanged))]
	public partial RatingViewShape Shape { get; set; } = RatingViewDefaults.Shape;

	/// <summary>
	/// Gets or sets the color used to draw the border of the shape.
	/// </summary>
	/// <remarks>
	/// This uses a non-nullable <see cref="Color"/>. A <see langword="null"/> value will be converted to <see cref="Colors.Transparent"/>
	/// </remarks>
	[AllowNull]
	[BindableProperty(PropertyChangedMethodName = nameof(OnShapeBorderColorChanged), CoerceValueMethodName = nameof(CoerceColorToTransparent))]
	public partial Color ShapeBorderColor { get; set; } = RatingViewDefaults.ShapeBorderColor;

	/// <summary>
	/// Gets or sets the thickness of the border applied to the shape, in device-independent units.
	/// </summary>
	/// <remarks>A value of 0 indicates that no border will be rendered. Negative values are not supported and may
	/// result in undefined behavior.</remarks>
	[BindableProperty(PropertyChangedMethodName = nameof(OnShapeBorderThicknessChanged), PropertyChangingMethodName = nameof(OnShapeBorderThicknessChanging))]
	public partial double ShapeBorderThickness { get; set; } = RatingViewDefaults.ShapeBorderThickness;

	/// <summary>
	/// Gets or sets the diameter of the shape, in device-independent units.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnShapeDiameterSizeChanged))]
	public partial double ShapeDiameter { get; set; } = RatingViewDefaults.ItemShapeSize;

	/// <summary>
	/// Gets or sets the color used to display empty rating shapes.
	/// </summary>
	/// <remarks>
	/// This uses a non-nullable <see cref="Color"/>. A <see langword="null"/> value will be converted to <see cref="Colors.Transparent"/>
	/// </remarks>
	[AllowNull]
	[BindableProperty(PropertyChangedMethodName = nameof(OnRatingColorChanged), CoerceValueMethodName = nameof(CoerceColorToTransparent))]
	public partial Color EmptyShapeColor { get; set; } = RatingViewDefaults.EmptyShapeColor;

	/// <summary>
	/// Gets or sets the color used to fill the rating indicator.
	/// </summary>
	/// <remarks>
	/// This uses a non-nullable <see cref="Color"/>. A <see langword="null"/> value will be converted to <see cref="Colors.Transparent"/>
	/// </remarks>
	[AllowNull]
	[BindableProperty(PropertyChangedMethodName = nameof(OnRatingColorChanged), CoerceValueMethodName = nameof(CoerceColorToTransparent))]
	public partial Color FillColor { get; set; } = RatingViewDefaults.FillColor;

	/// <summary>
	/// Gets or sets a value indicating whether the control is in a read-only state.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnIsReadOnlyChanged))]
	public partial bool IsReadOnly { get; set; } = RatingViewDefaults.IsReadOnly;

	/// <summary>
	/// Gets or sets the maximum rating value that can be assigned.
	/// </summary>
	/// <remarks>The maximum rating determines the upper limit for rating inputs. Changing this value may affect
	/// validation and display of rating controls. The value must be a positive integer.</remarks>
	[BindableProperty(PropertyChangedMethodName = nameof(OnMaximumRatingChanged), PropertyChangingMethodName = nameof(OnMaximumRatingChanging))]
	public partial int MaximumRating { get; set; } = RatingViewDefaults.MaximumRating;

	/// <summary>
	/// Gets or sets the fill behavior to apply when the rating view is tapped.
	/// </summary>
	/// <remarks>Use this property to control how the rating view visually responds to user interaction. The
	/// selected fill option determines the appearance of the rating elements when tapped.</remarks>
	[Obsolete($"Use {nameof(FillOption)} instead")]
	[BindableProperty(PropertyChangedMethodName = nameof(OnRatingColorChanged))]
	public partial RatingViewFillOption FillWhenTapped { get; set; } = RatingViewDefaults.FillWhenTapped;

	/// <summary>
	/// Gets or sets the rating value for the item.
	/// </summary>
	/// <remarks>The rating must be a valid value as determined by the associated validation method. Changing the
	/// rating triggers the property changed callback, which may update related UI or logic.</remarks>
	[BindableProperty(PropertyChangedMethodName = nameof(OnRatingChanged), PropertyChangingMethodName = nameof(OnRatingChanging))]
	public partial double Rating { get; set; } = RatingViewDefaults.Rating;

	/// <summary>
	/// Gets or sets the amount of space, in device-independent units, between adjacent items.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnSpacingChanged))]
	public partial double Spacing { get; set; } = RatingViewDefaults.Spacing;

	/// <summary>Gets or sets the element to fill when a <see cref="Rating"/> is set.</summary>
	/// <remarks>Use this property to control how the rating view visually responds to user interaction.
	/// The selected fill option determines the appearance of the rating elements when tapped.</remarks>
	[BindableProperty(PropertyChangingMethodName = nameof(OnRatingColorChanged))]
	public partial RatingViewFillOption FillOption { get; set; } = RatingViewDefaults.FillOption;

	internal HorizontalStackLayout RatingLayout { get; } = [];

	static int GetRatingWhenMaximumRatingEqualsOne(double rating) => rating.Equals(0.0) ? 1 : 0;

	static Border CreateChild(in string shape, in Thickness itemPadding, in double shapeBorderThickness, in double itemShapeSize, in Brush shapeBorderColor, in Color itemColor) => new()
	{
		BackgroundColor = itemColor,
		Margin = 0,
		Padding = itemPadding,
		Stroke = new SolidColorBrush(Colors.Transparent),
		StrokeThickness = 0,

		Content = new Path
		{
			Aspect = Stretch.Uniform,
			Data = (Geometry?)new PathGeometryConverter().ConvertFromInvariantString(shape),
			HeightRequest = itemShapeSize,
			Stroke = shapeBorderColor,
			StrokeLineCap = PenLineCap.Round,
			StrokeLineJoin = PenLineJoin.Round,
			StrokeThickness = shapeBorderThickness,
			WidthRequest = itemShapeSize,
		}
	};

	object CoerceColorToTransparent(object value)
	{
		var colorValue = (Color?)value;
		return colorValue ?? Colors.Transparent;
	}

	static ReadOnlyCollection<VisualElement> GetVisualTreeDescendantsWithBorderAndShape(VisualElement root, bool isShapeFill)
	{
		List<VisualElement> result = [];
		var stackLayout = (HorizontalStackLayout)root.GetVisualTreeDescendants().OfType<VisualElement>().First();
		foreach (var child in stackLayout.Children)
		{
			if (isShapeFill)
			{
				if (child is not Border border)
				{
					throw new InvalidOperationException($"Children must be of type {nameof(Border)}");
				}

				if (border.Content is not Shape borderShape)
				{
					throw new InvalidOperationException($"Border Content must be of type {nameof(Shape)}");
				}

				result.Add(borderShape);
			}
			else
			{
				result.Add((Border)child);
			}
		}

		return result.AsReadOnly();
	}

	void OnIsReadOnlyChanged(object oldValue, object newValue)
	{
		foreach (var child in RatingLayout.Children.Cast<Border>())
		{
			if (!IsReadOnly)
			{
				TapGestureRecognizer tapGestureRecognizer = new();
				tapGestureRecognizer.Tapped += OnShapeTapped;
				child.GestureRecognizers.Add(tapGestureRecognizer);
				continue;
			}

			child.GestureRecognizers.Clear();
		}
	}

	void OnRatingChanging(object oldValue, object newValue)
	{
		var rating = (double)newValue;

		if (rating < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(newValue), $"{nameof(Rating)} cannot be less than 0");
		}

		if (rating > MaximumRating)
		{
			throw new ArgumentOutOfRangeException(nameof(newValue), $"{nameof(Rating)} cannot be greater than {nameof(MaximumRating)}");
		}
	}

	void OnMaximumRatingChanging(object oldValue, object newValue)
	{
		var maximumRating = (int)newValue;

		switch (maximumRating)
		{
			case <= 0:
				throw new ArgumentOutOfRangeException(nameof(newValue), $"{nameof(MaximumRating)} must be greater than 0");
			case > RatingViewDefaults.MaximumRatingLimit:
				throw new ArgumentOutOfRangeException(nameof(newValue), $"{nameof(MaximumRating)} cannot be greater than {nameof(RatingViewDefaults.MaximumRatingLimit)}");
		}
	}

	void OnMaximumRatingChanged(object oldValue, object newValue)
	{
		var layout = RatingLayout;
		var newMaximumRatingValue = (int)newValue;
		var oldMaximumRatingValue = (int)oldValue;
		if (newMaximumRatingValue < oldMaximumRatingValue)
		{
			for (var lastElement = layout.Count - 1; lastElement >= newMaximumRatingValue; lastElement--)
			{
				layout.RemoveAt(lastElement);
			}

			UpdateShapeFills(FillOption);
		}
		else if (newMaximumRatingValue > oldMaximumRatingValue)
		{
			AddChildrenToLayout(oldMaximumRatingValue - 1, newMaximumRatingValue - 1);
		}

		if (newMaximumRatingValue < Rating) // Ensure Rating is never greater than MaximumRating 
		{
			Rating = newMaximumRatingValue;
		}
	}

	void OnRatingChanged(object oldValue, object newValue)
	{
		var newRating = (double)newValue;

		UpdateShapeFills(FillOption);
		OnRatingChangedEvent(new RatingChangedEventArgs(newRating));
	}

	void OnSpacingChanged(object oldValue, object newValue)
	{
		RatingLayout.Spacing = (double)newValue;
	}

	void OnRatingColorChanged(object oldValue, object newValue)
	{
		UpdateShapeFills(FillOption);
	}

	static LinearGradientBrush GetPartialFillBrush(Color filledColor, double partialFill, Color emptyColor)
	{
		return new(
			[
				new GradientStop(filledColor, 0),
				new GradientStop(filledColor, (float)partialFill),
				new GradientStop(emptyColor, (float)partialFill)
			],
			new Point(0, 0), new Point(1, 0));
	}

	void OnCustomShapePathPropertyChanged(object oldValue, object newValue)
	{
		var newShape = (string)newValue;

		if (Shape is not RatingViewShape.Custom)
		{
			return;
		}

		string newShapePathData;
		if (string.IsNullOrEmpty(newShape))
		{
			Shape = RatingViewDefaults.Shape;
			newShapePathData = PathShapes.Star;
		}
		else
		{
			newShapePathData = newShape;
		}

		ChangeShape(newShapePathData);
	}

	void OnShapePaddingPropertyChanged(object oldValue, object newValue)
	{
		for (var element = 0; element < RatingLayout.Count; element++)
		{
			((Border)this.RatingLayout.Children[element]).Padding = (Thickness)newValue;
		}
	}

	void OnShapeBorderColorChanged(object oldValue, object newValue)
	{
		for (var element = 0; element < RatingLayout.Count; element++)
		{
			var border = (Border)this.RatingLayout.Children[element];
			if (border.Content is not null)
			{
				((Path)border.Content.GetVisualTreeDescendants()[0]).Stroke = (Color)newValue;
			}
		}
	}

	void OnShapeBorderThicknessChanging(object oldValue, object newValue)
	{
		if ((double)newValue < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(newValue), $"{nameof(ShapeBorderThickness)} must be greater than 0");
		}
	}

	void OnShapeBorderThicknessChanged(object oldValue, object newValue)
	{
		for (var element = 0; element < RatingLayout.Count; element++)
		{
			var border = (Border)this.RatingLayout.Children[element];
			if (border.Content is not null)
			{
				((Path)border.Content.GetVisualTreeDescendants()[0]).StrokeThickness = (double)newValue;
			}
		}
	}

	void OnShapePropertyChanged(object oldValue, object newValue)
	{
		ChangeShape(GetShapePathData((RatingViewShape)newValue));
	}

	void OnShapeDiameterSizeChanged(object oldValue, object newValue)
	{
		for (var element = 0; element < RatingLayout.Count; element++)
		{
			var border = (Border)this.RatingLayout.Children[element];
			if (border.Content is null)
			{
				continue;
			}

			var rating = (Path)border.Content.GetVisualTreeDescendants()[0];
			rating.WidthRequest = (double)newValue;
			rating.HeightRequest = (double)newValue;
		}
	}

	void AddChildrenToLayout(int minimumRating, int maximumRating)
	{
		RatingLayout.Spacing = Spacing;
		var shape = GetShapePathData(Shape);
		for (var i = minimumRating; i < maximumRating; i++)
		{
			var child = CreateChild(shape, ShapePadding, ShapeBorderThickness, ShapeDiameter, ShapeBorderColor, BackgroundColor);
			if (!IsReadOnly)
			{
				TapGestureRecognizer tapGestureRecognizer = new();
				tapGestureRecognizer.Tapped += OnShapeTapped;
				child.GestureRecognizers.Add(tapGestureRecognizer);
			}

			RatingLayout.Children.Add(child);
		}

		UpdateShapeFills(FillOption);
	}

	void ChangeShape(string shape)
	{
		for (var element = 0; element < RatingLayout.Count; element++)
		{
			var border = (Border)RatingLayout.Children[element];
			if (border.Content is not null)
			{
				((Path)border.Content.GetVisualTreeDescendants()[0]).Data = (Geometry?)new PathGeometryConverter().ConvertFromInvariantString(shape);
			}
		}
	}

	string GetShapePathData(RatingViewShape shape) => shape switch
	{
		RatingViewShape.Custom when CustomShapePath is null => throw new InvalidOperationException($"Unable to draw RatingViewShape.Custom because {nameof(CustomShapePath)} is null. Please provide an SVG Path to {nameof(CustomShapePath)}."),
		RatingViewShape.Custom => CustomShapePath,
		RatingViewShape.Circle => PathShapes.Circle,
		RatingViewShape.Dislike => PathShapes.Dislike,
		RatingViewShape.Heart => PathShapes.Heart,
		RatingViewShape.Like => PathShapes.Like,
		RatingViewShape.Star => PathShapes.Star,
		_ => throw new NotSupportedException($"{shape} is not yet supported")
	};

	void OnShapeTapped(object? sender, TappedEventArgs? e)
	{
		if (sender is not Border tappedShape)
		{
			return;
		}

		var tappedShapeIndex = RatingLayout.Children.IndexOf(tappedShape);

		Rating = MaximumRating > 1
			? tappedShapeIndex + 1
			: GetRatingWhenMaximumRatingEqualsOne(Rating);
	}

	void UpdateShapeFills(RatingViewFillOption ratingViewFillOption)
	{
		var isShapeFill = ratingViewFillOption is RatingViewFillOption.Shape;
		var visualElements = GetVisualTreeDescendantsWithBorderAndShape((VisualElement)RatingLayout.GetVisualTreeDescendants()[0], isShapeFill);
		if (isShapeFill)
		{
			UpdateAllShapeFills(visualElements, Rating, FillColor, EmptyShapeColor);
		}
		else
		{
			UpdateAllBackgroundFills(visualElements, Rating, FillColor, EmptyShapeColor, BackgroundColor);
		}

		static void UpdateAllShapeFills(ReadOnlyCollection<VisualElement> shapes, double rating, Color filledColor, Color emptyColor)
		{
			var fullFillCount = (int)Math.Floor(rating); // Determine the number of fully filled shapes
			var partialFillCount = rating - fullFillCount; // Determine the fraction for the partially filled shape (if any)
			for (var i = 0; i < shapes.Count; i++)
			{
				var ratingShape = (Shape)shapes[i];
				if (i < fullFillCount)
				{
					ratingShape.Fill = filledColor; // Fully filled shape
				}
				else if (i == fullFillCount && partialFillCount > 0)
				{
					ratingShape.Fill = GetPartialFillBrush(filledColor, partialFillCount, emptyColor); // Partial fill
				}
				else
				{
					ratingShape.Fill = emptyColor; // Empty fill
				}
			}
		}

		static void UpdateAllBackgroundFills(ReadOnlyCollection<VisualElement> shapes, double rating, Color filledColor, Color emptyColor, Color? backgroundColor)
		{
			var fullFillCount = (int)Math.Floor(rating); // Determine the number of fully filled shapes
			var partialFillCount = rating - fullFillCount; // Determine the fraction for the partially filled shapes (if any)
			backgroundColor ??= Colors.Transparent;

			for (var i = 0; i < shapes.Count; i++)
			{
				var shapeBorder = (Border)shapes[i];
				if (shapeBorder.Content is not Shape shape)
				{
					continue;
				}

				shape.Fill = emptyColor;

				if (i < fullFillCount) // Fully filled shape
				{
					shapeBorder.Background = new SolidColorBrush(filledColor);
				}
				else if (i == fullFillCount && partialFillCount > 0) // Partial fill
				{
					shapeBorder.Background = GetPartialFillBrush(filledColor, partialFillCount, backgroundColor);
				}
				else // Empty fill
				{
					shapeBorder.Background = new SolidColorBrush(backgroundColor);
				}
			}
		}
	}

	void OnRatingChangedEvent(RatingChangedEventArgs e) => weakEventManager.HandleEvent(this, e, nameof(RatingChanged));
}