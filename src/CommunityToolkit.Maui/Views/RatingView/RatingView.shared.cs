// Ignore Spelling: color, bindable, colors

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;
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
	public static readonly BindableProperty ItemShapeProperty = BindableProperty.Create(nameof(ItemShape), typeof(RatingViewShapes), typeof(RatingView), defaultValue: RatingViewDefaults.ItemShape, propertyChanged: OnItemShapePropertyChanged, defaultValueCreator: static _ => RatingViewDefaults.ItemShape);

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
	public RatingViewShapes ItemShape
	{
		get => (RatingViewShapes)GetValue(ItemShapeProperty);
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

	internal HorizontalStackLayout RatingLayout { get; } = new();

	static int GetRatingWhenMaximumRatingEqualsOne(double rating) => rating.Equals(0.0) ? 1 : 0;

	static Border CreateChild(string shape, Thickness itemPadding, double shapeBorderThickness, double itemShapeSize, Brush shapeBorderColor, Color itemColor)
	{
		return new()
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
	}

	static ReadOnlyCollection<VisualElement> GetVisualTreeDescendantsWithBorderAndShape(VisualElement root, bool isShapeFill)
	{
		List<VisualElement> result = [];
		HorizontalStackLayout stackLayout = (HorizontalStackLayout)root.GetVisualTreeDescendants().OfType<VisualElement>().First();
		foreach (IView? child in stackLayout.Children)
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
		ratingView.OnPropertyChanged(nameof(ratingView.IsReadOnly));
		ratingView.HandleIsReadOnlyChanged();
	}

			child.GestureRecognizers.Clear();
		}
	}
	
	static void OnMaximumRatingChange(BindableObject bindable, object oldValue, object newValue)
	{
		var ratingView = (RatingView)bindable;
		if (ratingView.Control is null)
		{
			return;
		}

		var element = ratingView.Control;
		var newMaximumRatingValue = (int)newValue;
		var oldMaximumRatingValue = (int)oldValue;
		if (newMaximumRatingValue < (int)oldValue)
		{
			for (var lastElement = element.Count - 1; lastElement >= newMaximumRatingValue; lastElement--)
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
		if (ratingView.Control is null)
		{
			return;
		}

		var newValueDouble = (double)newValue;
		ratingView.UpdateRatingDrawing(ratingView.RatingFill);
		if (ratingView.IsReadOnly)
		{
			return;
		}

		ratingView.OnRatingChangedEvent(new RatingChangedEventArgs(newValueDouble));
	}

	static void OnSpacingChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var ratingView = (RatingView)bindable;
		if (ratingView.Control is null)
		{
			return;
		}

		ratingView.Control.Spacing = (double)newValue;
	}

	static void OnRatingColorChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var ratingView = (RatingView)bindable;
		if (ratingView.Control is null)
		{
			return;
		}

		ratingView.UpdateRatingDrawing(ratingView.RatingFill);
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

	/// <summary>Updates each rating item colors based on the rating value (filled, partially filled, empty).</summary>
	/// <remarks>At this time, since .NET MAUI doesn't have direct partial fill, we approximate with a gradient.</remarks>
	/// <param name="ratingItems">A list of rating item visual elements to update.</param>
	/// <param name="rating">Current rating value (e.g., 3.7)</param>
	/// <param name="filledColor">Color of a filled item.</param>
	/// <param name="emptyColor">Color of an empty item.</param>
	/// <param name="backgroundColor">Color of the item.</param>
	static void UpdateRatingItemColors(ReadOnlyCollection<VisualElement> ratingItems, double rating, Color filledColor, Color emptyColor, Color? backgroundColor)
	{
		var fullRatingItems = (int)Math.Floor(rating); // Determine the number of fully filled rating items
		var partialFill = rating - fullRatingItems; // Determine the fraction for the partially filled rating item (if any)
		backgroundColor ??= Colors.Transparent;
		for (var i = 0; i < ratingItems.Count; i++)
		{
			var border = (Border)ratingItems[i];
			if (border.Content is not Shape shape)
			{
				continue;
			}

			shape.Fill = emptyColor;
			if (i < fullRatingItems)
			{
				border.Background = new SolidColorBrush(filledColor); // Fully filled shape
			}
			else if (i == fullRatingItems && partialFill > 0)
			{
				border.Background = PartialFill(filledColor, partialFill, backgroundColor); // Partial fill
			}
			else
			{
				border.Background = new SolidColorBrush(backgroundColor); // Empty fill
			}
		}
	}

	/// <summary>Updates each rating item shape colors based on the rating value (filled, partially filled, empty).</summary>
	/// <remarks>At this time, since .NET MAUI doesn't have direct partial fill, we approximate with a gradient.</remarks>
	/// <param name="ratingItems">A list of rating item shape visual elements to update.</param>
	/// <param name="rating">Current rating value (e.g., 3.7)</param>
	/// <param name="filledColor">Color of a filled shape.</param>
	/// <param name="emptyColor">Color of an empty shape.</param>
	static void UpdateRatingShapeColors(ReadOnlyCollection<VisualElement> ratingItems, double rating, Color filledColor, Color emptyColor)
	{
		var fullShapes = (int)Math.Floor(rating); // Determine the number of fully filled shapes
		var partialFill = rating - fullShapes; // Determine the fraction for the partially filled shape (if any)
		for (var i = 0; i < ratingItems.Count; i++)
		{
			var ratingShape = (Shape)ratingItems[i];
			if (i < fullShapes)
			{
				ratingShape.Fill = filledColor; // Fully filled shape
			}
			else if (i == fullShapes && partialFill > 0)
			{
				ratingShape.Fill = PartialFill(filledColor, partialFill, emptyColor); // Partial fill
			}
			else
			{
				ratingShape.Fill = emptyColor; // Empty fill
			}
		}
	}

	static void OnCustomShapePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var ratingView = (RatingView)bindable;
		if (ratingView.Control is null)
		{
			return;
		}

		if (ratingView.ItemShape is not RatingViewShapes.Custom)
		{
			return;
		}

		string newShapePathData;
		if (string.IsNullOrEmpty(newShape))
		{
			ratingView.ItemShape = RatingViewDefaults.Shape;
			newShapePathData = Core.Primitives.RatingViewShape.Star.PathData;
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
		if (ratingView.Control is null)
		{
			return;
		}

		for (var element = 0; element < ratingView.Control.Count; element++)
		{
			((Border)ratingView.RatingLayout.Children[element]).Padding = (Thickness)newValue;
		}
	}

	static void OnItemShapeBorderColorChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var ratingView = (RatingView)bindable;
		if (ratingView.Control is null)
		{
			return;
		}

		for (var element = 0; element < ratingView.Control.Count; element++)
		{
			var border = (Border)ratingView.Control.Children[element];
			if (border.Content is not null)
			{
				((Path)border.Content.GetVisualTreeDescendants()[0]).Stroke = (Color)newValue;
			}
		}
	}

	static void OnItemShapeBorderThicknessChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var ratingView = (RatingView)bindable;
		if (ratingView.Control is null)
		{
			return;
		}

		for (var element = 0; element < ratingView.Control.Count; element++)
		{
			var border = (Border)ratingView.Control.Children[element];
			if (border.Content is not null)
			{
				((Path)border.Content.GetVisualTreeDescendants()[0]).StrokeThickness = (double)newValue;
			}
		}
	}

	static void OnItemShapePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var ratingView = (RatingView)bindable;
		if (ratingView.Control is null)
		{
			return;
		}

		ratingView.ChangeRatingItemShape(ratingView.GetShapePathData((RatingViewShapes)newValue));
	}

	static void OnItemShapeSizeChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var ratingView = (RatingView)bindable;
		if (ratingView.Control is null)
		{
			return;
		}

		for (var element = 0; element < ratingView.Control.Count; element++)
		{
			var border = (Border)ratingView.Control.Children[element];
			if (border.Content is null)
			{
				continue;
			}

			Path rating = (Path)border.Content.GetVisualTreeDescendants()[0];
			rating.WidthRequest = (double)newValue;
			rating.HeightRequest = (double)newValue;
		}
	}
	
	void AddChildrenToLayout(int minimumRating, int maximumRating)
	{
		if (Control is null)
		{
			return;
		}

		Control.Spacing = Spacing;
		var shape = GetShapePathData(ItemShape);
		for (var i = minimum; i < maximum; i++)
		{
			Border child = CreateChild(shape, ItemPadding, ShapeBorderThickness, ItemShapeSize, ShapeBorderColor, BackgroundColor);
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
			return;
		}

		for (var element = 0; element < Control.Count; element++)
		{
			var border = (Border)Control.Children[element];
			if (border.Content is not null)
			{
				((Path)border.Content.GetVisualTreeDescendants()[0]).Data = (Geometry?)new PathGeometryConverter().ConvertFromInvariantString(shape);
			}
		}
	}

	string GetShapePathData(RatingViewShape shape)
	{
		return shape switch
		{
			RatingViewShape.Circle => Core.Primitives.RatingViewShape.Circle.PathData,
			RatingViewShape.Custom => CustomItemShape ?? Core.Primitives.RatingViewShape.Star.PathData,
			RatingViewShape.Dislike => Core.Primitives.RatingViewShape.Dislike.PathData,
			RatingViewShape.Heart => Core.Primitives.RatingViewShape.Heart.PathData,
			RatingViewShape.Like => Core.Primitives.RatingViewShape.Like.PathData,
			_ => Core.Primitives.RatingViewShape.Star.PathData,
		};
	}

	void OnRatingChangedEvent(RatingChangedEventArgs e)
	{
		weakEventManager.HandleEvent(this, e, nameof(RatingChanged));
	}

		for (var i = 0; i < Control.Children.Count; i++)
		{
			var child = (Border)Control.Children[i];
			if (!IsReadOnly)
			{
				TapGestureRecognizer tapGestureRecognizer = new();
				tapGestureRecognizer.Tapped += OnItemTapped;
				child.GestureRecognizers.Add(tapGestureRecognizer);
				continue;
			}

			child.GestureRecognizers.Clear();
		}
	}

	/// <summary>Item tapped event, to re-draw the current rating.</summary>
	/// <remarks>When a shape such as 'Like' and there is only a maximum rating of 1, we then toggle the current between 0 and 1.</remarks>
	/// <param name="sender">Element sender of event.</param>
	/// <param name="e">Event arguments.</param>
	void OnItemTapped(object? sender, TappedEventArgs? e)
	{
		if (Control is null)
		{
			return;
		}

		ArgumentNullException.ThrowIfNull(sender);
		var border = (Border)sender;
		var itemIndex = Control.Children.IndexOf(border);
		Rating = MaximumRating > 1 ? itemIndex + 1 : GetRatingWhenMaximumRatingEqualsOne(Rating);
	}

	void OnRatingChangedEvent(RatingChangedEventArgs e)
	{
		weakEventManager.HandleEvent(this, e, nameof(RatingChanged));
	}

	/// <summary>Update the drawing of the controls ratings.</summary>
	void UpdateRatingDrawing(RatingFillElement ratingFill)
	{
		if (Control is null)
		{
			return;
		}

		var isShapeFill = ratingFill is RatingFillElement.Shape;
		var visualElements = GetVisualTreeDescendantsWithBorderAndShape((VisualElement)Control.GetVisualTreeDescendants()[0], isShapeFill);
		if (visualElements is null)
		{
			return;
		}

		if (isShapeFill)
		{
			UpdateRatingShapeColors(visualElements, Rating, FilledColor, EmptyColor);
		}
		else
		{
			UpdateRatingItemColors(visualElements, Rating, FilledColor, EmptyColor, BackgroundColor);
		}
	}
}

