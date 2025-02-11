// Ignore Spelling: color, bindable, colors

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using Microsoft.Maui.Controls.Shapes;
using Path = Microsoft.Maui.Controls.Shapes.Path;

namespace CommunityToolkit.Maui.Views;

/// <summary>Rating view control.</summary>
public partial class RatingView : TemplatedView, IRatingView
{
	/// <summary>Bindable property for attached property <see cref="CustomItemShape"/>.</summary>
	public static readonly BindableProperty CustomItemShapeProperty = BindableProperty.Create(nameof(CustomItemShape), typeof(string), typeof(RatingView), defaultValue: null, propertyChanged: OnCustomShapePropertyChanged);

	/// <summary>Bindable property for <see cref="ItemPadding"/>.</summary>
	public static readonly BindableProperty ItemPaddingProperty = BindableProperty.Create(nameof(ItemPadding), typeof(Thickness), typeof(RatingView), default(Thickness), propertyChanged: OnItemPaddingPropertyChanged, defaultValueCreator: static _ => RatingViewDefaults.ItemPadding);

	/// <summary>Bindable property for attached property <see cref="ItemShape"/>.</summary>
	public static readonly BindableProperty ItemShapeProperty = BindableProperty.Create(nameof(ItemShape), typeof(RatingViewShape), typeof(RatingView), defaultValue: RatingViewDefaults.Shape, propertyChanged: OnItemShapePropertyChanged, defaultValueCreator: static _ => RatingViewDefaults.Shape);

	/// <summary>Bindable property for attached property <see cref="ShapeBorderColor"/>.</summary>
	public static readonly BindableProperty ShapeBorderColorProperty = BindableProperty.Create(nameof(ShapeBorderColor), typeof(Color), typeof(RatingView), defaultValue: RatingViewDefaults.ShapeBorderColor, propertyChanged: OnItemShapeBorderColorChanged, defaultValueCreator: static _ => RatingViewDefaults.ShapeBorderColor);

	/// <summary>Bindable property for attached property <see cref="ShapeBorderThickness"/>.</summary>
	public static readonly BindableProperty ShapeBorderThicknessProperty = BindableProperty.Create(nameof(ShapeBorderThickness), typeof(double), typeof(RatingView), defaultValue: RatingViewDefaults.ShapeBorderThickness, propertyChanged: OnItemShapeBorderThicknessChanged, defaultValueCreator: static _ => RatingViewDefaults.ShapeBorderThickness);

	/// <summary>Bindable property for attached property <see cref="Size"/>.</summary>
	public static readonly BindableProperty ItemShapeSizeProperty = BindableProperty.Create(nameof(ItemShapeSize), typeof(double), typeof(RatingView), defaultValue: RatingViewDefaults.ItemShapeSize, propertyChanged: OnItemShapeSizeChanged, defaultValueCreator: static _ => RatingViewDefaults.ItemShapeSize);

	/// <summary>Bindable property for attached property <see cref="EmptyColor"/>.</summary>
	public static readonly BindableProperty EmptyColorProperty = BindableProperty.Create(nameof(EmptyColor), typeof(Color), typeof(RatingView), defaultValue: RatingViewDefaults.EmptyColor, propertyChanged: OnRatingColorChanged, defaultValueCreator: static _ => RatingViewDefaults.EmptyColor);

	/// <summary>Bindable property for attached property <see cref="FilledColor"/>.</summary>
	public static readonly BindableProperty FilledColorProperty = BindableProperty.Create(nameof(FilledColor), typeof(Color), typeof(RatingView), defaultValue: RatingViewDefaults.FilledColor, propertyChanged: OnRatingColorChanged, defaultValueCreator: static _ => RatingViewDefaults.FilledColor);

	/// <summary>The backing store for the <see cref="IsReadOnly" /> bindable property.</summary>
	public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(nameof(IsReadOnly), typeof(bool), typeof(RatingView), defaultValue: RatingViewDefaults.IsReadOnly, propertyChanged: OnIsReadOnlyChanged);

	/// <summary>The backing store for the <see cref="MaximumRating" /> bindable property.</summary>
	public static readonly BindableProperty MaximumRatingProperty = BindableProperty.Create(nameof(MaximumRating), typeof(int), typeof(RatingView), defaultValue: RatingViewDefaults.MaximumRating, validateValue: IsMaximumRatingValid, propertyChanged: OnMaximumRatingChange);

	/// <summary>The backing store for the <see cref="RatingFill" /> bindable property.</summary>
	public static readonly BindableProperty RatingFillProperty = BindableProperty.Create(nameof(RatingFill), typeof(RatingFillElement), typeof(RatingView), defaultValue: RatingFillElement.Shape, propertyChanged: OnRatingColorChanged);

	/// <summary>The backing store for the <see cref="Rating" /> bindable property.</summary>
	public static readonly BindableProperty RatingProperty = BindableProperty.Create(nameof(Rating), typeof(double), typeof(RatingView), defaultValue: RatingViewDefaults.DefaultRating, validateValue: IsRatingValid, propertyChanged: OnRatingChanged);

	/// <summary>The backing store for the <see cref="Spacing" /> bindable property.</summary>
	public static readonly BindableProperty SpacingProperty = BindableProperty.Create(nameof(Spacing), typeof(double), typeof(RatingView), defaultValue: RatingViewDefaults.Spacing, propertyChanged: OnSpacingChanged);

	readonly WeakEventManager weakEventManager = new();

	///<summary>The default constructor of the control.</summary>
	public RatingView()
	{
		RatingLayout.SetBinding<RatingView, object>(BindingContextProperty, static ratingView => ratingView.BindingContext, source: this);
		base.ControlTemplate = new ControlTemplate(() => RatingLayout);

		AddChildrenToLayout(0, MaximumRating);
	}

	/// <summary>Occurs when <see cref="Rating"/> is changed.</summary>
	public event EventHandler<RatingChangedEventArgs> RatingChanged
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	/// <inheritdoc cref="ControlTemplate"/>
	public new ControlTemplate ControlTemplate => base.ControlTemplate; // Ensures the ControlTemplate is readonly, preventing users from breaking the HorizontalStackLayout 

	///<summary>Defines the shape to be drawn.</summary>
	public string? CustomItemShape
	{
		get => (string?)GetValue(CustomItemShapeProperty);
		set => SetValue(CustomItemShapeProperty, value);
	}

	/// <summary>Gets or sets a value of the empty rating color property.</summary>
	[AllowNull]
	public Color EmptyColor
	{
		get => (Color)GetValue(EmptyColorProperty);
		set => SetValue(EmptyColorProperty, value ?? Colors.Transparent);
	}

	/// <summary>Gets or sets a value of the filled rating color property.</summary>
	[AllowNull]
	public Color FilledColor
	{
		get => (Color)GetValue(FilledColorProperty);
		set => SetValue(FilledColorProperty, value ?? Colors.Transparent);
	}

	///<summary>Gets or sets a value indicating if the controls is read-only.</summary>
	public bool IsReadOnly
	{
		get => (bool)GetValue(IsReadOnlyProperty);
		set => SetValue(IsReadOnlyProperty, value);
	}

	/// <summary>Gets or sets a value indicating the padding between the rating item and the rating shape.</summary>
	public Thickness ItemPadding
	{
		get => (Thickness)GetValue(ItemPaddingProperty);
		set => SetValue(ItemPaddingProperty, value);
	}

	/// <summary>Gets or sets a value indicating the shape size.</summary>
	public double ItemShapeSize
	{
		get => (double)GetValue(ItemShapeSizeProperty);
		set => SetValue(ItemShapeSizeProperty, value);
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

	/// <summary>Gets or sets a value indicating which element of the rating to fill.</summary>
	public RatingFillElement RatingFill
	{
		get => (RatingFillElement)GetValue(RatingFillProperty);
		set => SetValue(RatingFillProperty, value);
	}

	///<summary>Gets or sets a value indicating the rating shape.</summary>
	public RatingViewShape ItemShape
	{
		get => (RatingViewShape)GetValue(ItemShapeProperty);
		set => SetValue(ItemShapeProperty, value);
	}

	/// <summary>Gets or sets a value indicating the rating shape border color.</summary>
	[AllowNull]
	public Color ShapeBorderColor
	{
		get => (Color)GetValue(ShapeBorderColorProperty);
		set => SetValue(ShapeBorderColorProperty, value ?? Colors.Transparent);
	}

	///<summary>Gets or sets a value indicating the rating shape border thickness.</summary>
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

	static Border CreateChild(string shape, Thickness itemPadding, double shapeBorderThickness, double itemShapeSize, Brush shapeBorderColor, Color itemColor) => new()
	{
		BackgroundColor = itemColor,
		Margin = 0,
		Padding = itemPadding,
		Stroke = new SolidColorBrush(Colors.Transparent),
		StrokeThickness = 0,
		Style = null!,

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
					throw new InvalidOperationException($"Border Content must be of type {nameof(ItemShape)}");
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
				tapGestureRecognizer.Tapped += ratingView.OnItemTapped;
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

			ratingView.UpdateAllRatingsFills(ratingView.RatingFill);
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

		ratingView.UpdateAllRatingsFills(ratingView.RatingFill);
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
		ratingView.UpdateAllRatingsFills(ratingView.RatingFill);
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

	static void OnCustomShapePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var ratingView = (RatingView)bindable;
		var newShape = (string)newValue;

		if (ratingView.ItemShape is not RatingViewShape.Custom)
		{
			return;
		}

		string newShapePathData;
		if (string.IsNullOrEmpty(newShape))
		{
			ratingView.ItemShape = RatingViewDefaults.Shape;
			newShapePathData = PathShapes.Star;
		}
		else
		{
			newShapePathData = newShape;
		}

		ratingView.ChangeRatingItemShape(newShapePathData);
	}

	static void OnItemPaddingPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var ratingView = (RatingView)bindable;
		for (var element = 0; element < ratingView.RatingLayout.Count; element++)
		{
			((Border)ratingView.RatingLayout.Children[element]).Padding = (Thickness)newValue;
		}
	}

	static void OnItemShapeBorderColorChanged(BindableObject bindable, object oldValue, object newValue)
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

	static void OnItemShapeBorderThicknessChanged(BindableObject bindable, object oldValue, object newValue)
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

	static void OnItemShapePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var ratingView = (RatingView)bindable;
		ratingView.ChangeRatingItemShape(ratingView.GetShapePathData((RatingViewShape)newValue));
	}

	static void OnItemShapeSizeChanged(BindableObject bindable, object oldValue, object newValue)
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
		var shape = GetShapePathData(ItemShape);
		for (var i = minimumRating; i < maximumRating; i++)
		{
			var child = CreateChild(shape, ItemPadding, ShapeBorderThickness, ItemShapeSize, ShapeBorderColor, BackgroundColor);
			if (!IsReadOnly)
			{
				TapGestureRecognizer tapGestureRecognizer = new();
				tapGestureRecognizer.Tapped += OnItemTapped;
				child.GestureRecognizers.Add(tapGestureRecognizer);
			}

			RatingLayout.Children.Add(child);
		}

		UpdateAllRatingsFills(RatingFill);
	}

	void ChangeRatingItemShape(string shape)
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
		RatingViewShape.Circle => PathShapes.Circle,
		RatingViewShape.Custom when CustomItemShape is not null => CustomItemShape,
		RatingViewShape.Dislike => PathShapes.Dislike,
		RatingViewShape.Heart => PathShapes.Heart,
		RatingViewShape.Like => PathShapes.Like,
		_ => PathShapes.Star
	};

	void OnItemTapped(object? sender, TappedEventArgs? e)
	{
		ArgumentNullException.ThrowIfNull(sender);

		var border = (Border)sender;
		var itemIndex = RatingLayout.Children.IndexOf(border);
		Rating = MaximumRating > 1 ? itemIndex + 1 : GetRatingWhenMaximumRatingEqualsOne(Rating);
	}

	void OnRatingChangedEvent(RatingChangedEventArgs e)
	{
		weakEventManager.HandleEvent(this, e, nameof(RatingChanged));
	}

	void UpdateAllRatingsFills(RatingFillElement ratingFill)
	{
		var isShapeFill = ratingFill is RatingFillElement.Shape;
		var visualElements = GetVisualTreeDescendantsWithBorderAndShape((VisualElement)RatingLayout.GetVisualTreeDescendants()[0], isShapeFill);
		if (isShapeFill)
		{
			UpdateAllRatingShapeFills(visualElements, Rating, FilledColor, EmptyColor);
		}
		else
		{
			UpdateRatingItemsFills(visualElements, Rating, FilledColor, EmptyColor, BackgroundColor);
		}
	}

	static void UpdateAllRatingShapeFills(ReadOnlyCollection<VisualElement> ratingItems, double rating, Color filledColor, Color emptyColor)
	{
		var fullFillCount = (int)Math.Floor(rating); // Determine the number of fully filled shapes
		var partialFillCount = rating - fullFillCount; // Determine the fraction for the partially filled shape (if any)
		for (var i = 0; i < ratingItems.Count; i++)
		{
			var ratingShape = (Shape)ratingItems[i];
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

	static void UpdateRatingItemsFills(ReadOnlyCollection<VisualElement> ratingItems, double rating, Color filledColor, Color emptyColor, Color? backgroundColor)
	{
		var fullFillCount = (int)Math.Floor(rating); // Determine the number of fully filled rating items
		var partialFillCount = rating - fullFillCount; // Determine the fraction for the partially filled rating item (if any)
		backgroundColor ??= Colors.Transparent;
		for (var i = 0; i < ratingItems.Count; i++)
		{
			var border = (Border)ratingItems[i];
			if (border.Content is not Shape shape)
			{
				continue;
			}

			shape.Fill = emptyColor;
			if (i < fullFillCount)
			{
				border.Background = new SolidColorBrush(filledColor); // Fully filled shape
			}
			else if (i == fullFillCount && partialFillCount > 0)
			{
				border.Background = GetPartialFillBrush(filledColor, partialFillCount, backgroundColor); // Partial fill
			}
			else
			{
				border.Background = new SolidColorBrush(backgroundColor); // Empty fill
			}
		}
	}
	
	static class PathShapes
	{
		public const string Star = "M9 11.3l3.71 2.7-1.42-4.36L15 7h-4.55L9 2.5 7.55 7H3l3.71 2.64L5.29 14z";
		public const string Heart = "M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z";
		public const string Circle = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2z";
		public const string Like = "M1 21h4V9H1v12zm22-11c0-1.1-.9-2-2-2h-6.31l.95-4.57.03-.32c0-.41-.17-.79-.44-1.06L14.17 1 7.59 7.59C7.22 7.95 7 8.45 7 9v10c0 1.1.9 2 2 2h9c.83 0 1.54-.5 1.84-1.22l3.02-7.05c.09-.23.14-.47.14-.73v-1.91l-.01-.01L23 10z";
		public const string Dislike = "M15 3H6c-.83 0-1.54.5-1.84 1.22l-3.02 7.05c-.09.23-.14.47-.14.73v1.91l.01.01L1 14c0 1.1.9 2 2 2h6.31l-.95 4.57-.03.32c0 .41.17.79.44 1.06L9.83 23l6.59-6.59c.36-.36.58-.86.58-1.41V5c0-1.1-.9-2-2-2zm4 0v12h4V3h-4z";
	}
}