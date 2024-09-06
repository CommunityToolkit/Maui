// Ignore Spelling: color, bindable, colors

using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Shapes;

namespace CommunityToolkit.Maui.Views;

/// <summary>Rating view control.</summary>
public class RatingView : TemplatedView, IRatingViewShape
{
	/// <summary>Bindable property for <see cref="CustomShape"/> bindable property.</summary>
	public static readonly BindableProperty CustomShapeProperty = RatingViewItemElement.CustomShapeProperty;

	// TODO: Add BindableProperty to allow the developer to define and style the rating Item(s), then adjust the control creation accordingly.

	/// <summary>The backing store for the <see cref="EmptyBackgroundColor" /> bindable property.</summary>
	public static readonly BindableProperty EmptyBackgroundColorProperty = RatingViewItemElement.EmptyBackgroundColorProperty;

	/// <summary>The backing store for the <see cref="FilledBackgroundColor" /> bindable property.</summary>
	public static readonly BindableProperty FilledBackgroundColorProperty = RatingViewItemElement.FilledBackgroundColorProperty;

	/// <summary>The backing store for the <see cref="IsEnabled" /> bindable property.</summary>
	public static new readonly BindableProperty IsEnabledProperty = BindableProperty.Create(nameof(IsEnabled), typeof(bool), typeof(RatingView), defaultValue: RatingViewDefaults.IsEnabled, propertyChanged: OnIsEnabledChanged);

	// TODO: Add some kind of Roslyn Analyser to validate at design/build time that the MaximumRating is in bounds (1-25).
	/// <summary>The backing store for the <see cref="MaximumRating" /> bindable property.</summary>
	public static readonly BindableProperty MaximumRatingProperty = BindableProperty.Create(nameof(MaximumRating), typeof(byte), typeof(RatingView), defaultValue: RatingViewDefaults.MaximumRating, validateValue: maximumRatingValidator, propertyChanged: OnMaximumRatingChange);

	/// <summary>The backing store for the <see cref="RatingFill" /> bindable property.</summary>
	public static readonly BindableProperty RatingFillProperty = BindableProperty.Create(nameof(RatingFill), typeof(RatingFillElement), typeof(RatingView), defaultValue: RatingFillElement.Shape, propertyChanged: OnUpdateRatingDraw);

	/// <summary>The backing store for the <see cref="Rating" /> bindable property.</summary>
	public static readonly BindableProperty RatingProperty = BindableProperty.Create(nameof(Rating), typeof(double), typeof(RatingView), defaultValue: RatingViewDefaults.DefaultRating, propertyChanged: OnRatingChanged);

	/// <summary>The backing store for the <see cref="ShapeBorderColor" /> bindable property.</summary>
	public static readonly BindableProperty ShapeBorderColorProperty = RatingViewItemElement.ShapeBorderColorProperty;

	/// <summary>The backing store for the <see cref="ShapeBorderThickness" /> bindable property.</summary>
	public static readonly BindableProperty ShapeBorderThicknessProperty = RatingViewItemElement.ShapeBorderThicknessProperty;

	/// <summary>Bindable property for <see cref="ItemPadding"/> bindable property.</summary>
	public static readonly BindableProperty ItemPaddingProperty = RatingViewItemElement.ItemPaddingProperty;

	/// <summary>The backing store for the <see cref="ItemShapeSize" /> bindable property.</summary>
	public static readonly BindableProperty SizeProperty = RatingViewItemElement.SizeProperty;

	/// <summary>The backing store for the <see cref="Spacing" /> bindable property.</summary>
	public static readonly BindableProperty SpacingProperty = BindableProperty.Create(nameof(Spacing), typeof(double), typeof(RatingView), defaultValue: RatingViewDefaults.Spacing, propertyChanged: OnSpacingChanged);

	/// <summary>The backing store for the <see cref="Shape" /> bindable property.</summary>
	public readonly BindableProperty ShapeProperty = RatingViewItemElement.ShapeProperty;

	static readonly WeakEventManager weakEventManager = new();

	///<summary>The default constructor of the control.</summary>
	public RatingView()
	{
		ControlTemplate = new ControlTemplate(typeof(HorizontalStackLayout));
		AddChildrenToControl();
	}

	/// <summary>Occurs when <see cref="Rating"/> is changed.</summary>
	public static event EventHandler<RatingChangedEventArgs> RatingChanged
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	///<summary>The control to be displayed</summary>
	public HorizontalStackLayout? Control { get; private set; }

	///<summary>Defines the shape to be drawn.</summary>
	public string CustomShape
	{
		get => (string)GetValue(CustomShapeProperty);
		set => SetValue(CustomShapeProperty, value);
	}

	/// <summary>Gets or sets a value of the empty rating background property.</summary>
	public Color EmptyBackgroundColor
	{
		get => (Color)GetValue(EmptyBackgroundColorProperty);
		set => SetValue(EmptyBackgroundColorProperty, value);
	}

	/// <summary>Gets or sets a value of the filled rating background property.</summary>
	public Color FilledBackgroundColor
	{
		get => (Color)GetValue(FilledBackgroundColorProperty);
		set => SetValue(FilledBackgroundColorProperty, value);
	}

	///<summary>Defines if the drawn shapes can be clickable to rate.</summary>
	public new bool IsEnabled
	{
		get => (bool)GetValue(IsEnabledProperty);
		set => SetValue(IsEnabledProperty, value);
	}

	/// <summary>Defines the shape padding thickness.</summary>
	public Thickness ItemPadding
	{
		get => (Thickness)GetValue(ItemPaddingProperty);
		set => SetValue(ItemPaddingProperty, value);
	}

	/// <summary>Defines the item shape size.</summary>
	public double ItemShapeSize
	{
		get => (double)GetValue(SizeProperty);
		set => SetValue(SizeProperty, value);
	}

	/// <summary>Gets or sets a value of the control maximum rating property.</summary>
	public byte MaximumRating
	{
		get => (byte)GetValue(MaximumRatingProperty);
		set
		{
			if (value is < 1 or > RatingViewDefaults.MaximumRatings)
			{
				throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(MaximumRating)} must be between 1 and {RatingViewDefaults.MaximumRatings}.");
			}

			SetValue(MaximumRatingProperty, value);
		}
	}

	/// <summary>Gets or sets a value of the control rating property.</summary>
	public double Rating
	{
		get => (double)GetValue(RatingProperty);
		set => SetValue(RatingProperty, value);
	}

	/// <summary>Defines which element of the rating to fill.</summary>
	public RatingFillElement RatingFill
	{
		get => (RatingFillElement)GetValue(RatingFillProperty);
		set => SetValue(RatingFillProperty, value);
	}

	///<summary>Defines the shape to be drawn.</summary>
	public RatingViewShape Shape
	{
		get => (RatingViewShape)GetValue(ShapeProperty);
		set => SetValue(ShapeProperty, value);
	}

	/// <summary>Defines the shape border color.</summary>
	public Color ShapeBorderColor
	{
		get => (Color)GetValue(ShapeBorderColorProperty);
		set => SetValue(ShapeBorderColorProperty, value);
	}

	///<summary>Defines the shape border thickness.</summary>
	public double ShapeBorderThickness
	{
		get => (double)GetValue(ShapeBorderThicknessProperty);
		set => SetValue(ShapeBorderThicknessProperty, value);
	}

	Color IRatingViewShape.EmptyBackgroundColorDefaultValueCreator()
	{
		return RatingViewDefaults.EmptyBackgroundColor;
	}

	Color IRatingViewShape.FilledBackgroundColorDefaultValueCreator()
	{
		return RatingViewDefaults.FilledBackgroundColor;
	}

	///<summary>Defines the space between the drawn shapes.</summary>
	public double Spacing
	{
		get => (double)GetValue(SpacingProperty);
		set => SetValue(SpacingProperty, value);
	}

	Thickness IRatingViewShape.ItemPaddingDefaultValueCreator()
	{
		return RatingViewDefaults.ItemPadding;
	}

	Color IRatingViewShape.ItemShapeBorderColorDefaultValueCreator()
	{
		return RatingViewDefaults.ShapeBorderColor;
	}

	double IRatingViewShape.ItemShapeBorderThicknessDefaultValueCreator()
	{
		return RatingViewDefaults.ShapeBorderThickness;
	}

	double IRatingViewShape.ItemShapeSizeDefaultValueCreator()
	{
		return RatingViewDefaults.ItemShapeSize;
	}

	/// <summary>Change the rating shape to a custom shape.</summary>
	/// <remarks>Only change the rating shape if <see cref="RatingViewShape.Custom"/>.</remarks>
	/// <param name="oldValue">Old rating custom shape path data.</param>
	/// <param name="newValue">Old rating custom shape path data.</param>
	void IRatingViewShape.OnCustomShapePropertyChanged(string? oldValue, string? newValue)
	{
		if (Control is null)
		{
			return;
		}

		if (this.Shape is RatingViewShape.Custom)
		{
			// TODO: Change this so that we only update the shape, as there is no need to clear and redraw.
			ClearAndReDraw();
		}
	}

	/// <summary>Change the rating item padding.</summary>
	/// <param name="oldValue">Old rating item padding.</param>
	/// <param name="newValue">New rating item padding.</param>
	void IRatingViewShape.OnItemPaddingPropertyChanged(Thickness oldValue, Thickness newValue)
	{
		if (Control is null)
		{
			return;
		}

		for (int element = 0; element < Control.Count; element++)
		{
			((Border)Control.Children[element]).Padding = newValue;
		}
	}

	/// <summary>Change the rating item shape border color.</summary>
	/// <param name="oldValue">Old rating item shape border color.</param>
	/// <param name="newValue">New rating item shape border color.</param>
	void IRatingViewShape.OnItemShapeBorderColorChanged(Color oldValue, Color newValue)
	{
		if (newValue is null || Control is null)
		{
			return;
		}

		for (int element = 0; element < Control.Count; element++)
		{
			((Microsoft.Maui.Controls.Shapes.Path)((Border)Control.Children[element]).Content!.GetVisualTreeDescendants()[0]).Stroke = newValue;
		}
	}

	/// <summary>Change the rating item shape border thickness.</summary>
	/// <param name="oldValue">Old rating item shape border thickness.</param>
	/// <param name="newValue">New rating item shape border thickness.</param>
	void IRatingViewShape.OnItemShapeBorderThicknessChanged(double oldValue, double newValue)
	{
		if (Control is null)
		{
			return;
		}

		for (int element = 0; element < Control.Count; element++)
		{
			((Microsoft.Maui.Controls.Shapes.Path)((Border)Control.Children[element]).Content!.GetVisualTreeDescendants()[0]).StrokeThickness = newValue;
		}
	}

	/// <summary>Change the rating item shape.</summary>
	/// <param name="oldValue">Old rating item shape.</param>
	/// <param name="newValue">New rating item shape.</param>
	void IRatingViewShape.OnItemShapePropertyChanged(RatingViewShape oldValue, RatingViewShape newValue)
	{
		if (Control is not null)
		{
			// TODO: Change the rating shape directly, no need to clear and re-draw!!!
			ClearAndReDraw();
		}
	}

	/// <summary>Change the rating item shape size.</summary>
	/// <param name="oldValue">Old rating item shape size.</param>
	/// <param name="newValue">New rating item shape size.</param>
	void IRatingViewShape.OnItemShapeSizeChanged(double oldValue, double newValue)
	{
		if (Control is HorizontalStackLayout stack)
		{
			for (int column = 0; column < stack.Count; column++)
			{
				Microsoft.Maui.Controls.Shapes.Path rating = (Microsoft.Maui.Controls.Shapes.Path)((Border)stack.Children[column]).Content!.GetVisualTreeDescendants()[0];
				rating.WidthRequest = newValue;
				rating.HeightRequest = newValue;
			}
		}
	}

	/// <summary>Change the rating item empty background color.</summary>
	/// <param name="oldValue">Old rating item empty background color.</param>
	/// <param name="newValue">new rating item empty background color.</param>
	void IRatingViewShape.OnEmptyBackgroundColorPropertyChanged(Color oldValue, Color newValue)
	{
		if (Control is not null)
		{
			UpdateRatingDraw();
		}
	}

	/// <summary>Change the rating item filled background color.</summary>
	/// <param name="oldValue">Old rating item filled background color.</param>
	/// <param name="newValue">new rating item filled background color.</param>
	void IRatingViewShape.OnFilledBackgroundColorPropertyChanged(Color oldValue, Color newValue)
	{
		if (Control is not null)
		{
			UpdateRatingDraw();
		}
	}

	RatingViewShape IRatingViewShape.ShapeDefaultValueCreator()
	{
		return RatingViewDefaults.Shape;
	}

	///<summary>Method called every time the control's Binding Context is changed.</summary>
	protected override void OnBindingContextChanged()
	{
		base.OnBindingContextChanged();
		if (Control is not null)
		{
			Control.BindingContext = BindingContext;
		}
	}

	///<summary>Called every time a child is added to the control.</summary>
	protected override void OnChildAdded(Element child)
	{
		if (Control is null && child is HorizontalStackLayout stack)
		{
			Control = stack;
		}

		base.OnChildAdded(child);
	}

	static bool maximumRatingValidator(BindableObject bindable, object value)
	{
		return (byte)value is >= 1 and <= RatingViewDefaults.MaximumRatings;
	}

	static void OnIsEnabledChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (newValue is null)
		{
			return;
		}

		RatingView ratingView = (RatingView)bindable;
		ratingView.OnPropertyChanged(nameof(ratingView.IsEnabled));
		ratingView.HandleIsEnabledChanged();
	}

	/// <summary>Maximum rating has changed.</summary>
	/// <remarks>
	///		Re draw the control.
	///		If the current rating is greater than the maximum, then change the current rating to the new value.
	/// </remarks>
	/// <param name="bindable">RatingView object.</param>
	/// <param name="oldValue">Old maximum rating value.</param>
	/// <param name="newValue">New maximum rating value.</param>
	static void OnMaximumRatingChange(BindableObject bindable, object oldValue, object newValue)
	{
		if (newValue is null)
		{
			return;
		}

		RatingView ratingView = (RatingView)bindable;
		// TODO: Change this so that if maximum is increased, we add new children, otherwise if less then we remove!
		ratingView.ClearAndReDraw();
		if ((byte)newValue < ratingView.Rating)
		{
			byte newRatingValue = (byte)newValue;
			ratingView.Rating = newRatingValue;
			weakEventManager.HandleEvent(ratingView, new RatingChangedEventArgs(newRatingValue), nameof(MaximumRating));
		}
	}

	static void OnSpacingChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (newValue is null)
		{
			return;
		}

		((RatingView)bindable).Control!.Spacing = (double)newValue;
	}

	static void OnUpdateRatingDraw(BindableObject bindable, object oldValue, object newValue)
	{
		if (newValue is null)
		{
			return;
		}

		RatingView ratingView = (RatingView)bindable;
		ratingView.UpdateRatingDraw();
	}

	/// <summary>Add the required children to the control.</summary>
	void AddChildrenToControl()
	{
		string shape = Shape switch
		{
			RatingViewShape.Circle => Core.Primitives.RatingViewShape.Circle.PathData,
			RatingViewShape.Custom => CustomShape ?? Core.Primitives.RatingViewShape.Star.PathData,
			RatingViewShape.Dislike => Core.Primitives.RatingViewShape.Dislike.PathData,
			RatingViewShape.Heart => Core.Primitives.RatingViewShape.Heart.PathData,
			RatingViewShape.Like => Core.Primitives.RatingViewShape.Like.PathData,
			_ => Core.Primitives.RatingViewShape.Star.PathData,
		};

		for (int i = 0; i < MaximumRating; i++)
		{
			Border child = CreateChild(shape, ItemPadding, ShapeBorderThickness, ItemShapeSize, ShapeBorderColor);
			if (IsEnabled)
			{
				TapGestureRecognizer tapGestureRecognizer = new();
				tapGestureRecognizer.Tapped += OnItemTapped;
				child.GestureRecognizers.Add(tapGestureRecognizer);
			}

			Control?.Children.Add(child);
		}

		UpdateRatingDraw();
	}

	static Border CreateChild(string shape, Thickness itemPadding, double shapeBorderThickness, double itemShapeSize, Brush shapeBorderColor)
	{
		Border shapeBorder = new()
		{
			BackgroundColor = Colors.Transparent,
			Margin = 0,
			Padding = itemPadding,
			Stroke = new SolidColorBrush(Colors.Transparent),
			StrokeThickness = 0,
			Style = null,
		};
		Microsoft.Maui.Controls.Shapes.Path shapePath = new()
		{
			Aspect = Stretch.Uniform,
			Data = (Geometry?)new PathGeometryConverter().ConvertFromInvariantString(shape),
			HeightRequest = itemShapeSize,
			Stroke = shapeBorderColor,
			StrokeLineCap = PenLineCap.Round,
			StrokeLineJoin = PenLineJoin.Round,
			StrokeThickness = shapeBorderThickness,
			WidthRequest = itemShapeSize,
		};
		shapeBorder.Content = shapePath;

		return shapeBorder;
	}

	/// <summary>Clear and re-draw the control.</summary>
	void ClearAndReDraw()
	{
		Control?.Children.Clear();
		AddChildrenToControl();
	}

	/// <summary>Ensure VisualElement.IsEnabled always matches RatingView.IsEnabled, and remove any added gesture recognisers.</summary>
	void HandleIsEnabledChanged()
	{
		base.IsEnabled = IsEnabled;
		if (!IsEnabled)
		{
			for (int i = 0; i < Control?.Children.Count; i++)
			{
				((Border)Control.Children[i]).GestureRecognizers.Clear();
			}
		}
	}

	/// <summary>Item tapped event, to re-draw the current rating and bubble up the event.</summary>
	/// <remarks>When a shape such as 'Like' and there is only a maximum rating of 1, we then toggle the current between 0 and 1.</remarks>
	/// <param name="sender">Element sender of event.</param>
	/// <param name="e">Event arguments.</param>
	void OnItemTapped(object? sender, TappedEventArgs? e)
	{
		if (sender is not Border tappedItem)
		{
			return;
		}

		if (Control is null)
		{
			return;
		}

		int itemIndex = Control.Children.IndexOf(tappedItem);
		Rating = MaximumRating > 1 ? itemIndex + 1 : Rating.Equals(0.0) ? 1 : 0;
	}

	static void OnRatingChanged(BindableObject bindable, object oldValue, object newValue)
	{
		RatingView ratingView = (RatingView)bindable;
		double oldValueDouble = (double)oldValue;
		double newValueDouble = (double)newValue;
		if (!oldValueDouble.Equals(newValueDouble))
		{
			ratingView.UpdateRatingDraw();
			weakEventManager.HandleEvent(ratingView, new RatingChangedEventArgs(newValueDouble), nameof(RatingChanged));
		}
	}

	/// <summary>Update the drawing of the controls ratings.</summary>
	void UpdateRatingDraw()
	{
		if (Control is null)
		{
			return;
		}

		bool isShapeFill = RatingFill is RatingFillElement.Shape;
		List<VisualElement> visualElements = GetVisualTreeDescendantsWithBorderAndShape((VisualElement)Control!.GetVisualTreeDescendants()[0], isShapeFill);
		if (isShapeFill)
		{
			UpdateRatingShapeColors(visualElements, Rating, FilledBackgroundColor, EmptyBackgroundColor);
		}
		else
		{
			UpdateRatingItemColors(visualElements, Rating, FilledBackgroundColor, EmptyBackgroundColor, BackgroundColor);
		}

		return;
	}
	static List<VisualElement> GetVisualTreeDescendantsWithBorderAndShape(VisualElement root, bool isShapeFill)
	{
		List<VisualElement> result = new();
		if (root.GetVisualTreeDescendants().OfType<VisualElement>().FirstOrDefault() is HorizontalStackLayout stackLayout)
		{
			// Iterate through the children of the HorizontalStackLayout
			foreach (IView? child in stackLayout.Children)
			{
				// Check if each child of the HorizontalStackLayout is a Border and contains a Shape
				if (child is not Border border || border.Content is not Shape shape)
				{
					break;
				}

				if (isShapeFill)
				{
					result.Add(shape);
				}
				else
				{
					result.Add(border);
				}
			}
		}

		return result;
	}

	/// <summary>Updates each rating item colors based on the rating value (filled, partially filled, empty).</summary>
	/// <param name="ratingItems">A list of rating item visual elements to update.</param>
	/// <param name="rating">Current rating value (e.g., 3.7)</param>
	/// <param name="filledBackgroundColor">Background color of a filled shape.</param>
	/// <param name="emptyBackgroundColor">Background color of an empty shape.</param>
	/// <param name="backgroundColor">Background color of the control.</param>
	static void UpdateRatingItemColors(List<VisualElement> ratingItems, double rating, Color filledBackgroundColor, Color emptyBackgroundColor, Color backgroundColor)
	{
		int fullShapes = (int)Math.Floor(rating); // Determine the number of fully filled shapes
		double partialFill = rating - fullShapes; // Determine the fraction for the partially filled shape (if any)

		for (int i = 0; i < ratingItems.Count; i++)
		{
			((Shape)((Border)ratingItems[i]).Content!).Fill = emptyBackgroundColor;
			if (i < fullShapes)
			{
				ratingItems[i].BackgroundColor = filledBackgroundColor; // Fully filled shape
			}
			else if (i == fullShapes && partialFill > 0)
			{
				// Set the background of the shape to partially filled, since .NET MAUI doesn't have direct partial fill, we'll approximate with a gradient
				ratingItems[i].Background = new LinearGradientBrush(
					[
							new GradientStop(filledBackgroundColor, 0),
							new GradientStop(filledBackgroundColor, (float)partialFill),  // Adjust the fill percentage
							new GradientStop(backgroundColor, (float)partialFill),
					],
					new Point(0, 0), new Point(1, 0));
			}
			else
			{
				ratingItems[i].BackgroundColor = backgroundColor;  // Empty shape
			}
		}
	}

	/// <summary>Updates each rating item shape colors based on the rating value (filled, partially filled, empty).</summary>
	/// <param name="ratingItems">A list of rating item shape visual elements to update.</param>
	/// <param name="rating">Current rating value (e.g., 3.7)</param>
	/// <param name="filledBackgroundColor">Background color of a filled shape.</param>
	/// <param name="emptyBackgroundColor">Background color of an empty shape.</param>
	static void UpdateRatingShapeColors(List<VisualElement> ratingItems, double rating, Color filledBackgroundColor, Color emptyBackgroundColor)
	{
		int fullShapes = (int)Math.Floor(rating); // Determine the number of fully filled shapes
		double partialFill = rating - fullShapes; // Determine the fraction for the partially filled shape (if any)

		for (int i = 0; i < ratingItems.Count; i++)
		{
			Shape ratingShape = (Shape)ratingItems[i];
			if (i < fullShapes)
			{
				ratingShape.Fill = filledBackgroundColor; // Fully filled shape
			}
			else if (i == fullShapes && partialFill > 0)
			{
				// Set the background of the shape to partially filled, since .NET MAUI doesn't have direct partial fill, we'll approximate with a gradient
				ratingShape.Fill = new LinearGradientBrush(
					[
							new GradientStop(filledBackgroundColor, 0),
							new GradientStop(filledBackgroundColor, (float)partialFill),  // Adjust the fill percentage
							new GradientStop(emptyBackgroundColor, (float)partialFill),
					],
					new Point(0, 0), new Point(1, 0));
			}
			else
			{
				ratingShape.Fill = emptyBackgroundColor;  // Empty shape
			}
		}
	}
}

/// <summary>Rating view fill element.</summary>
public enum RatingFillElement
{
	/// <summary>Fill the rating shape.</summary>
	Shape = 0,

	/// <summary>Fill the rating item.</summary>
	Item = 1
}