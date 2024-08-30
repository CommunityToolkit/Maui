// Ignore Spelling: color, bindable

using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Shapes;

namespace CommunityToolkit.Maui.Views;

/// <summary>Rating view control.</summary>
public class RatingView : TemplatedView, IContentView, IRatingViewShape
{
	/// <summary>Bindable property for <see cref="CustomShape"/> bindable property.</summary>
	public static readonly BindableProperty CustomShapeProperty = RatingViewItemElement.CustomShapeProperty;

	/// <summary>The backing store for the <see cref="EmptyBackgroundColor" /> bindable property.</summary>
	public static readonly BindableProperty EmptyBackgroundColorProperty = BindableProperty.Create(nameof(EmptyBackgroundColor), typeof(Color), typeof(RatingView), defaultValue: RatingViewDefaults.EmptyBackgroundColor, propertyChanged: OnUpdateRatingDraw);

	/// <summary>The backing store for the <see cref="FilledBackgroundColor" /> bindable property.</summary>
	public static readonly BindableProperty FilledBackgroundColorProperty = BindableProperty.Create(nameof(FilledBackgroundColor), typeof(Color), typeof(RatingView), defaultValue: RatingViewDefaults.FilledBackgroundColor, propertyChanged: OnUpdateRatingDraw);

	/// <summary>The backing store for the <see cref="IsEnabled" /> bindable property.</summary>
	public new static readonly BindableProperty IsEnabledProperty = BindableProperty.Create(nameof(IsEnabled), typeof(bool), typeof(RatingView), defaultValue: RatingViewDefaults.IsEnabled, propertyChanged: OnIsEnabledChanged);

	/// <summary>The backing store for the <see cref="MaximumRating" /> bindable property.</summary>
	public static readonly BindableProperty MaximumRatingProperty = BindableProperty.Create(nameof(MaximumRating), typeof(byte), typeof(RatingView), defaultValue: RatingViewDefaults.MaximumRating, validateValue: maximumRatingValidator, propertyChanged: OnMaximumRatingChange);

	/// <summary>The backing store for the <see cref="RatingFill" /> bindable property.</summary>
	public static readonly BindableProperty RatingFillProperty = BindableProperty.Create(nameof(RatingFill), typeof(RatingFillElement), typeof(RatingView), defaultValue: RatingFillElement.Shape, propertyChanged: OnUpdateRatingDraw);

	/// <summary>The backing store for the <see cref="Rating" /> bindable property.</summary>
	public static readonly BindableProperty RatingProperty = BindableProperty.Create(nameof(Rating), typeof(double), typeof(RatingView), defaultValue: RatingViewDefaults.DefaultRating, propertyChanged: OnUpdateRatingDraw);

	/// <summary>The backing store for the <see cref="RatingShapeOutlineColor" /> bindable property.</summary>
	public static readonly BindableProperty RatingShapeOutlineColorProperty = BindableProperty.Create(nameof(RatingShapeOutlineColor), typeof(Color), typeof(RatingView), defaultValue: RatingViewDefaults.RatingShapeOutlineColor, propertyChanged: OnShapeOutlineColorChanged);

	/// <summary>The backing store for the <see cref="RatingShapeOutlineThickness" /> bindable property.</summary>
	public static readonly BindableProperty RatingShapeOutlineThicknessProperty = BindableProperty.Create(nameof(RatingShapeOutlineThickness), typeof(double), typeof(RatingView), defaultValue: RatingViewDefaults.RatingShapeOutlineThickness, propertyChanged: OnShapeOutlineThicknessChanged);

	/// <summary>Bindable property for <see cref="ShapePadding"/> bindable property.</summary>
	public static readonly BindableProperty ShapePaddingProperty = RatingViewItemElement.ShapePaddingProperty;

	/// <summary>The backing store for the <see cref="Size" /> bindable property.</summary>
	public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(double), typeof(RatingView), defaultValue: RatingViewDefaults.Size, propertyChanged: OnSizeChanged);

	/// <summary>The backing store for the <see cref="Spacing" /> bindable property.</summary>
	public static readonly BindableProperty SpacingProperty = BindableProperty.Create(nameof(Spacing), typeof(double), typeof(RatingView), defaultValue: RatingViewDefaults.Spacing, propertyChanged: OnSpacingChanged);

	/// <summary>The backing store for the <see cref="Shape" /> bindable property.</summary>
	public readonly BindableProperty ShapeProperty = RatingViewItemElement.ShapeProperty;

	static readonly WeakEventManager weakEventManager = new();
	Microsoft.Maui.Controls.Shapes.Path[] shapes;

	///<summary>The default constructor of the control.</summary>
	public RatingView()
	{
		ControlTemplate = new ControlTemplate(typeof(HorizontalStackLayout));
		shapes = new Microsoft.Maui.Controls.Shapes.Path[MaximumRating];
		HorizontalOptions = new LayoutOptions(LayoutAlignment.Center, true);
		AddChildrenToControl();
	}

	/// <summary>Occurs when <see cref="OnShapeTapped"/> and the rating is changed.</summary>
	public static event EventHandler RatingChanged
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

	/// <summary>Gets or sets a value of the rating outline border color property.</summary>
	public Color RatingShapeOutlineColor
	{
		get => (Color)GetValue(RatingShapeOutlineColorProperty);
		set => SetValue(RatingShapeOutlineColorProperty, value);
	}

	///<summary>Gets or sets a value of the rating outline border thickness property.</summary>
	public double RatingShapeOutlineThickness
	{
		get => (double)GetValue(RatingShapeOutlineThicknessProperty);
		set => SetValue(RatingShapeOutlineThicknessProperty, value);
	}

	///<summary>Defines the shape to be drawn.</summary>
	public RatingViewShape Shape
	{
		get => (RatingViewShape)GetValue(ShapeProperty);
		set => SetValue(ShapeProperty, value);
	}

	/// <summary>Defines the shape padding thickness.</summary>
	public Thickness ShapePadding
	{
		get => (Thickness)GetValue(ShapePaddingProperty);
		set => SetValue(ShapePaddingProperty, value);
	}

	/// <summary>Gets or sets a value of the control size for the drawn shape property.</summary>
	public double Size
	{
		get => (double)GetValue(SizeProperty);
		set => SetValue(SizeProperty, value);
	}

	///<summary>Defines the space between the drawn shapes.</summary>
	public double Spacing
	{
		get => (double)GetValue(SpacingProperty);
		set => SetValue(SpacingProperty, value);
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

		if ((this).Shape is RatingViewShape.Custom)
		{
			ClearAndReDraw();
		}
	}

	void IRatingViewShape.OnShapePaddingPropertyChanged(Thickness oldValue, Thickness newValue)
	{
		if (Control is null)
		{
			return;
		}

		for (int element = 0; element < Control.Count; element++)
		{
			Microsoft.Maui.Controls.Shapes.Path rating = (Microsoft.Maui.Controls.Shapes.Path)((Border)Control.Children[element]).Content!.GetVisualTreeDescendants()[0];
			rating.Margin = newValue;
		}
	}

	void IRatingViewShape.OnShapePropertyChanged(RatingViewShape oldValue, RatingViewShape newValue)
	{
		if (Control is null)
		{
			return;
		}

		ClearAndReDraw();
	}

	RatingViewShape IRatingViewShape.ShapeDefaultValueCreator()
	{
		return RatingViewDefaults.Shape;
	}

	Thickness IRatingViewShape.ShapePaddingDefaultValueCreator()
	{
		return RatingViewDefaults.ShapePadding;
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
			OnControlInitialized();
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
		ratingView.ClearAndReDraw();
		if ((byte)newValue < ratingView.Rating)
		{
			ratingView.Rating = (byte)newValue;
			weakEventManager.HandleEvent(ratingView, EventArgs.Empty, nameof(RatingChanged));
		}
	}

	static void OnShapeOutlineColorChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (newValue is null)
		{
			return;
		}

		RatingView ratingView = (RatingView)bindable;
		if (ratingView.Control is HorizontalStackLayout stack)
		{
			Color newColor = (Color)newValue;
			for (int column = 0; column < stack.Count; column++)
			{
				Microsoft.Maui.Controls.Shapes.Path rating = (Microsoft.Maui.Controls.Shapes.Path)((Border)stack.Children[column]).Content!.GetVisualTreeDescendants()[0];
				rating.Stroke = newColor;
				ratingView.shapes[column].Stroke = newColor;
			}
		}
	}

	static void OnShapeOutlineThicknessChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (newValue is null)
		{
			return;
		}

		RatingView ratingView = (RatingView)bindable;
		if (ratingView.Control is HorizontalStackLayout stack)
		{
			double newThickness = (double)newValue;
			for (int column = 0; column < stack.Count; column++)
			{
				Microsoft.Maui.Controls.Shapes.Path rating = (Microsoft.Maui.Controls.Shapes.Path)((Border)stack.Children[column]).Content!.GetVisualTreeDescendants()[0];
				rating.StrokeThickness = newThickness;
				ratingView.shapes[column].StrokeThickness = newThickness;
			}
		}
	}

	static void OnSizeChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (newValue is null)
		{
			return;
		}

		RatingView ratingView = (RatingView)bindable;
		if (ratingView.Control is HorizontalStackLayout stack)
		{
			double newSize = (double)newValue;
			for (int column = 0; column < stack.Count; column++)
			{
				Microsoft.Maui.Controls.Shapes.Path rating = (Microsoft.Maui.Controls.Shapes.Path)((Border)stack.Children[column]).Content!.GetVisualTreeDescendants()[0];
				rating.WidthRequest = newSize;
				rating.HeightRequest = newSize;
				rating.Margin = ratingView.ShapePadding;
				ratingView.shapes[column].WidthRequest = newSize;
				ratingView.shapes[column].HeightRequest = newSize;
			}
		}
	}

	static void OnSpacingChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (newValue is null)
		{
			return;
		}

		RatingView ratingView = (RatingView)bindable;
		if (ratingView.Control is HorizontalStackLayout stack)
		{
			stack.Spacing = (double)newValue;
		}
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
			Border shapeBorder = new()
			{
				Padding = ShapePadding,
				Margin = 0,
				StrokeThickness = 0
			};
			Microsoft.Maui.Controls.Shapes.Path image = new()
			{
				Aspect = Stretch.Uniform,
				Data = (Geometry?)new PathGeometryConverter().ConvertFromInvariantString(shape),
				HeightRequest = Size,
				Margin = ShapePadding,
				Stroke = RatingShapeOutlineColor,
				StrokeLineCap = PenLineCap.Round,
				StrokeLineJoin = PenLineJoin.Round,
				StrokeThickness = RatingShapeOutlineThickness,
				WidthRequest = Size,
			};

			if (RatingFill is RatingFillElement.Shape)
			{
				image.Fill = i <= Rating ? (Brush)FilledBackgroundColor : (Brush)EmptyBackgroundColor;
			}
			else
			{
				image.Fill = BackgroundColor;
				image.BackgroundColor = FilledBackgroundColor;
			}

			if (IsEnabled)
			{
				TapGestureRecognizer tapGestureRecognizer = new();
				tapGestureRecognizer.Tapped += OnShapeTapped;
				image.GestureRecognizers.Add(tapGestureRecognizer);
			}

			shapeBorder.Content = image;
			Control?.Children.Add(shapeBorder);
			shapes[i] = image;
		}

		UpdateRatingDraw();
	}

	/// <summary>Clear and re-draw the control.</summary>
	void ClearAndReDraw()
	{
		Control?.Children.Clear();
		shapes = new Microsoft.Maui.Controls.Shapes.Path[MaximumRating];
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
				Microsoft.Maui.Controls.Shapes.Path rankControl = (Microsoft.Maui.Controls.Shapes.Path)((Border)Control.Children[i]).Content!.GetVisualTreeDescendants()[0];
				rankControl?.GestureRecognizers.Clear();
			}
		}
	}

	/// <summary>Initialise the control.</summary>
	void OnControlInitialized()
	{
		shapes = new Microsoft.Maui.Controls.Shapes.Path[MaximumRating];
		HorizontalOptions = new LayoutOptions(LayoutAlignment.Center, true);
		if (Control is not null)
		{
			Control.Spacing = Spacing;
		}
	}

	/// <summary>Shape tapped event, to re-draw the current rating and bubble up the event.</summary>
	/// <remarks>When a shape such as 'Like' and there is only a maximum rating of 1, we then toggle the current between 0 and 1.</remarks>
	/// <param name="sender">Element sender of event.</param>
	/// <param name="e">Event arguments.</param>
	void OnShapeTapped(object? sender, TappedEventArgs? e)
	{
		if (sender is not Microsoft.Maui.Controls.Shapes.Path tappedShape)
		{
			return;
		}

		if (Control is null)
		{
			return;
		}

		int childIndex = Control.Children.IndexOf(tappedShape.Parent);
		double originalRating = Rating;
		Rating = MaximumRating > 1 ? childIndex + 1 : Rating.Equals(0.0) ? 1 : 0;
		if (!originalRating.Equals(Rating))
		{
			UpdateRatingDraw();
			weakEventManager.HandleEvent(this, e, nameof(RatingChanged));
		}
	}

	/// <summary>Update the drawing of the controls ratings.</summary>
	void UpdateRatingDraw()
	{
		for (int i = 0; i < MaximumRating; i++)
		{
			Microsoft.Maui.Controls.Shapes.Path image = shapes[i];
			if (Rating >= i + 1)
			{
				image.Stroke = RatingShapeOutlineColor;
				if (RatingFill is RatingFillElement.Shape)
				{
					image.Fill = FilledBackgroundColor;
				}
				else
				{
					image.Fill = BackgroundColor;
					image.BackgroundColor = FilledBackgroundColor;
					((Border)image.Parent).BackgroundColor = FilledBackgroundColor;
				}

				continue;
			}

			if (Rating % 1 is 0)
			{
				if (RatingFill is RatingFillElement.Shape)
				{
					image.Fill = EmptyBackgroundColor;
				}
				else
				{
					image.Fill = BackgroundColor;
					image.Background = null;
					image.BackgroundColor = EmptyBackgroundColor;
					((Border)image.Parent).BackgroundColor = EmptyBackgroundColor;
				}

				image.Stroke = RatingShapeOutlineColor;
				continue;
			}

			double fraction = Rating - Math.Floor(Rating);
			Microsoft.Maui.Controls.Shapes.Path element = shapes[(int)(Rating - fraction)];
			GradientStopCollection colors =
			[
				new(FilledBackgroundColor, (float)fraction),
				new(EmptyBackgroundColor, (float)fraction)
			];

			if (RatingFill is RatingFillElement.Shape)
			{
				element.Fill = new LinearGradientBrush(colors, new Point(0, 0), new Point(1, 0));
			}
			else
			{
				image.Fill = BackgroundColor;
				image.Background = new LinearGradientBrush(colors, new Point(0, 0), new Point(1, 0));
				((Border)image.Parent).Background = new LinearGradientBrush(colors, new Point(0, 0), new Point(1, 0));
			}

			element.Stroke = RatingShapeOutlineColor;
		}
	}
}

/// <summary>Rating view fill element.</summary>
public enum RatingFillElement
{
	/// <summary>Fill the rating shape.</summary>
	Shape = 0,

	/// <summary>Fill the rating cell.</summary>
	Cell = 1
}