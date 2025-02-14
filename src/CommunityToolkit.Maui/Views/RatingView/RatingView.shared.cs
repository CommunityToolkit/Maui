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
	/// <summary>Bindable property for attached property <see cref="CustomShapePath"/>.</summary>
	public static readonly BindableProperty CustomShapePathProperty = BindableProperty.Create(nameof(CustomShapePath), typeof(string), typeof(RatingView), defaultValue: null, propertyChanged: OnCustomShapePathPropertyChanged);

	/// <summary>Bindable property for <see cref="ShapePadding"/>.</summary>
	public static readonly BindableProperty ShapePaddingProperty = BindableProperty.Create(nameof(ShapePadding), typeof(Thickness), typeof(RatingView), RatingViewDefaults.ShapePadding, propertyChanged: OnShapePaddingPropertyChanged, defaultValueCreator: static _ => RatingViewDefaults.ShapePadding);

	/// <summary>Bindable property for attached property <see cref="Shape"/>.</summary>
	public static readonly BindableProperty ShapeProperty = BindableProperty.Create(nameof(Shape), typeof(RatingViewShape), typeof(RatingView), defaultValue: RatingViewDefaults.Shape, propertyChanged: OnShapePropertyChanged, defaultValueCreator: static _ => RatingViewDefaults.Shape);

	/// <summary>Bindable property for attached property <see cref="ShapeBorderColor"/>.</summary>
	public static readonly BindableProperty ShapeBorderColorProperty = BindableProperty.Create(nameof(ShapeBorderColor), typeof(Color), typeof(RatingView), defaultValue: RatingViewDefaults.ShapeBorderColor, propertyChanged: OnShapeBorderColorChanged, defaultValueCreator: static _ => RatingViewDefaults.ShapeBorderColor);

	/// <summary>Bindable property for attached property <see cref="ShapeBorderThickness"/>.</summary>
	public static readonly BindableProperty ShapeBorderThicknessProperty = BindableProperty.Create(nameof(ShapeBorderThickness), typeof(double), typeof(RatingView), defaultValue: RatingViewDefaults.ShapeBorderThickness, propertyChanged: OnShapeBorderThicknessChanged, defaultValueCreator: static _ => RatingViewDefaults.ShapeBorderThickness);

	/// <summary>Bindable property for attached property <see cref="Size"/>.</summary>
	public static readonly BindableProperty ShapeDiameterProperty = BindableProperty.Create(nameof(ShapeDiameter), typeof(double), typeof(RatingView), defaultValue: RatingViewDefaults.ItemShapeSize, propertyChanged: OnShapeDiameterSizeChanged, defaultValueCreator: static _ => RatingViewDefaults.ItemShapeSize);

	/// <summary>Bindable property for attached property <see cref="EmptyShapeColor"/>.</summary>
	public static readonly BindableProperty EmptyShapeColorProperty = BindableProperty.Create(nameof(EmptyShapeColor), typeof(Color), typeof(RatingView), defaultValue: RatingViewDefaults.EmptyShapeColor, propertyChanged: OnRatingColorChanged, defaultValueCreator: static _ => RatingViewDefaults.EmptyShapeColor);

	/// <summary>Bindable property for attached property <see cref="FillColor"/>.</summary>
	public static readonly BindableProperty FillColorProperty = BindableProperty.Create(nameof(FillColor), typeof(Color), typeof(RatingView), defaultValue: RatingViewDefaults.FillColor, propertyChanged: OnRatingColorChanged, defaultValueCreator: static _ => RatingViewDefaults.FillColor);

	/// <summary>The backing store for the <see cref="IsReadOnly" /> bindable property.</summary>
	public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(nameof(IsReadOnly), typeof(bool), typeof(RatingView), defaultValue: RatingViewDefaults.IsReadOnly, propertyChanged: OnIsReadOnlyChanged);

	/// <summary>The backing store for the <see cref="MaximumRating" /> bindable property.</summary>
	public static readonly BindableProperty MaximumRatingProperty = BindableProperty.Create(nameof(MaximumRating), typeof(int), typeof(RatingView), defaultValue: RatingViewDefaults.MaximumRating, validateValue: IsMaximumRatingValid, propertyChanged: OnMaximumRatingChange);

	/// <summary>The backing store for the <see cref="FillOption" /> bindable property.</summary>
	public static readonly BindableProperty FillWhenTappedProperty = BindableProperty.Create(nameof(FillOption), typeof(RatingViewFillOption), typeof(RatingView), defaultValue: Core.RatingViewFillOption.Shape, propertyChanged: OnRatingColorChanged);

	/// <summary>The backing store for the <see cref="Rating" /> bindable property.</summary>
	public static readonly BindableProperty RatingProperty = BindableProperty.Create(nameof(Rating), typeof(double), typeof(RatingView), defaultValue: RatingViewDefaults.DefaultRating, validateValue: IsRatingValid, propertyChanged: OnRatingChanged);

	/// <summary>The backing store for the <see cref="Spacing" /> bindable property.</summary>
	public static readonly BindableProperty SpacingProperty = BindableProperty.Create(nameof(Spacing), typeof(double), typeof(RatingView), defaultValue: RatingViewDefaults.Spacing, propertyChanged: OnSpacingChanged);

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

	/// <inheritdoc />
	public string? CustomShapePath
	{
		get => (string?)GetValue(CustomShapePathProperty);
		set => SetValue(CustomShapePathProperty, value);
	}

	/// <summary>Gets or sets a value of the empty shape color property.</summary>
	[AllowNull]
	public Color EmptyShapeColor
	{
		get => (Color)GetValue(EmptyShapeColorProperty);
		set => SetValue(EmptyShapeColorProperty, value ?? Colors.Transparent);
	}

	/// <summary>Gets or sets the color of the fill used to display the current rating.</summary>
	/// <remarks>Use <see cref="FillOption"/> to apply this color to the <see cref="RatingViewFillOption.Background"/> or the <see cref="RatingViewFillOption.Shape"/> </remarks>
	[AllowNull]
	public Color FillColor
	{
		get => (Color)GetValue(FillColorProperty);
		set => SetValue(FillColorProperty, value ?? Colors.Transparent);
	}

	///<summary>Gets or sets a value indicating if the user can interact with the <see cref="RatingView"/>.</summary>
	public bool IsReadOnly
	{
		get => (bool)GetValue(IsReadOnlyProperty);
		set => SetValue(IsReadOnlyProperty, value);
	}

	/// <inheritdoc />
	public Thickness ShapePadding
	{
		get => (Thickness)GetValue(ShapePaddingProperty);
		set => SetValue(ShapePaddingProperty, value);
	}

	/// <inheritdoc />
	public double ShapeDiameter
	{
		get => (double)GetValue(ShapeDiameterProperty);
		set => SetValue(ShapeDiameterProperty, value);
	}

	/// <summary>Gets or sets a value indicating the maximum rating.</summary>
	public int MaximumRating
	{
		get => (int)GetValue(MaximumRatingProperty);
		set
		{
			switch (value)
			{
				case <= 0:
					throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(MaximumRating)} must be greater than 0");
				case > RatingViewDefaults.MaximumRatingLimit:
					throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(MaximumRating)} cannot be greater than {nameof(RatingViewDefaults.MaximumRatingLimit)}");
				default:
					SetValue(MaximumRatingProperty, value);
					break;
			}
		}
	}

	/// <summary>Gets or sets a value indicating the rating.</summary>
	public double Rating
	{
		get => (double)GetValue(RatingProperty);
		set
		{
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(Rating)} cannot be less than 0");
			}

			if (value > MaximumRating)
			{
				throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(Rating)} cannot be greater than {nameof(MaximumRating)}");
			}

			SetValue(RatingProperty, value);
		}
	}

	/// <summary>Gets or sets the element to fill when a <see cref="Rating"/> is set.</summary>
	public RatingViewFillOption FillOption
	{
		get => (RatingViewFillOption)GetValue(FillWhenTappedProperty);
		set => SetValue(FillWhenTappedProperty, value);
	}

	/// <inheritdoc />
	public RatingViewShape Shape
	{
		get => (RatingViewShape)GetValue(ShapeProperty);
		set => SetValue(ShapeProperty, value);
	}

	/// <inheritdoc />
	[AllowNull]
	public Color ShapeBorderColor
	{
		get => (Color)GetValue(ShapeBorderColorProperty);
		set => SetValue(ShapeBorderColorProperty, value ?? Colors.Transparent);
	}

	/// <inheritdoc />
	public double ShapeBorderThickness
	{
		get => (double)GetValue(ShapeBorderThicknessProperty);
		set
		{
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(ShapeBorderThickness), $"{nameof(ShapeBorderThickness)} must be greater than 0");
			}

			SetValue(ShapeBorderThicknessProperty, value);
		}
	}

	///<summary>Gets or sets a value indicating the space between rating items.</summary>
	public double Spacing
	{
		get => (double)GetValue(SpacingProperty);
		set => SetValue(SpacingProperty, value);
	}

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

	static bool IsMaximumRatingValid(BindableObject bindable, object value)
	{
		return (int)value is >= 1 and <= RatingViewDefaults.MaximumRatingLimit;
	}

	static void OnIsReadOnlyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var ratingView = (RatingView)bindable;
		foreach (var child in ratingView.RatingLayout.Children.Cast<Border>())
		{
			if (!ratingView.IsReadOnly)
			{
				TapGestureRecognizer tapGestureRecognizer = new();
				tapGestureRecognizer.Tapped += ratingView.OnShapeTapped;
				child.GestureRecognizers.Add(tapGestureRecognizer);
				continue;
			}

			child.GestureRecognizers.Clear();
		}
	}

	static void OnMaximumRatingChange(BindableObject bindable, object oldValue, object newValue)
	{
		var ratingView = (RatingView)bindable;
		var layout = ratingView.RatingLayout;
		var newMaximumRatingValue = (int)newValue;
		var oldMaximumRatingValue = (int)oldValue;
		if (newMaximumRatingValue < oldMaximumRatingValue)
		{
			for (var lastElement = layout.Count - 1; lastElement >= newMaximumRatingValue; lastElement--)
			{
				layout.RemoveAt(lastElement);
			}

			ratingView.UpdateShapeFills(ratingView.FillOption);
		}
		else if (newMaximumRatingValue > oldMaximumRatingValue)
		{
			ratingView.AddChildrenToLayout(oldMaximumRatingValue - 1, newMaximumRatingValue - 1);
		}

		if (newMaximumRatingValue < ratingView.Rating) // Ensure Rating is never greater than MaximumRating 
		{
			ratingView.Rating = newMaximumRatingValue;
		}
	}

	static void OnRatingChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var ratingView = (RatingView)bindable;
		var newRating = (double)newValue;

		ratingView.UpdateShapeFills(ratingView.FillOption);
		ratingView.OnRatingChangedEvent(new RatingChangedEventArgs(newRating));
	}

	static void OnSpacingChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var ratingView = (RatingView)bindable;
		ratingView.RatingLayout.Spacing = (double)newValue;
	}

	static void OnRatingColorChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var ratingView = (RatingView)bindable;
		ratingView.UpdateShapeFills(ratingView.FillOption);
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

	static bool IsRatingValid(BindableObject bindable, object value)
	{
		return (double)value is >= 0.0 and <= RatingViewDefaults.MaximumRatingLimit;
	}

	static void OnCustomShapePathPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var ratingView = (RatingView)bindable;
		var newShape = (string)newValue;

		if (ratingView.Shape is not RatingViewShape.Custom)
		{
			return;
		}

		string newShapePathData;
		if (string.IsNullOrEmpty(newShape))
		{
			ratingView.Shape = RatingViewDefaults.Shape;
			newShapePathData = PathShapes.Star;
		}
		else
		{
			newShapePathData = newShape;
		}

		ratingView.ChangeShape(newShapePathData);
	}

	static void OnShapePaddingPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var ratingView = (RatingView)bindable;
		for (var element = 0; element < ratingView.RatingLayout.Count; element++)
		{
			((Border)ratingView.RatingLayout.Children[element]).Padding = (Thickness)newValue;
		}
	}

	static void OnShapeBorderColorChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var ratingView = (RatingView)bindable;
		for (var element = 0; element < ratingView.RatingLayout.Count; element++)
		{
			var border = (Border)ratingView.RatingLayout.Children[element];
			if (border.Content is not null)
			{
				((Path)border.Content.GetVisualTreeDescendants()[0]).Stroke = (Color)newValue;
			}
		}
	}

	static void OnShapeBorderThicknessChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var ratingView = (RatingView)bindable;
		for (var element = 0; element < ratingView.RatingLayout.Count; element++)
		{
			var border = (Border)ratingView.RatingLayout.Children[element];
			if (border.Content is not null)
			{
				((Path)border.Content.GetVisualTreeDescendants()[0]).StrokeThickness = (double)newValue;
			}
		}
	}

	static void OnShapePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var ratingView = (RatingView)bindable;
		ratingView.ChangeShape(ratingView.GetShapePathData((RatingViewShape)newValue));
	}

	static void OnShapeDiameterSizeChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var ratingView = (RatingView)bindable;
		for (var element = 0; element < ratingView.RatingLayout.Count; element++)
		{
			var border = (Border)ratingView.RatingLayout.Children[element];
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