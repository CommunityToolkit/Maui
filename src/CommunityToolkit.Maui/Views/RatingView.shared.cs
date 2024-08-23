// Ignore Spelling: color, bindable

using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Shapes;

namespace CommunityToolkit.Maui.Views;

/// <summary>Rating view control.</summary>
public class RatingView : TemplatedView
{
	/// <summary>The backing store for the <see cref="RatingBorder" /> bindable property.</summary>
	public static readonly BindableProperty RatingBorderProperty = BindableProperty.Create(nameof(RatingBorder), typeof(Border), typeof(RatingView), propertyChanged: OnRatingBorderChanged, defaultValueCreator: (_) => new Border());

	/// <summary>The backing store for the <see cref="RatingShapeOutlineColor" /> bindable property.</summary>
	public static readonly BindableProperty RatingShapeOutlineColorProperty = BindableProperty.Create(nameof(RatingShapeOutlineColor), typeof(Color), typeof(RatingView), defaultValue: RatingViewDefaults.RatingShapeOutlineColor, propertyChanged: OnShapeOutlineColorChanged);

	/// <summary>The backing store for the <see cref="RatingShapeOutlineThickness" /> bindable property.</summary>
	public static readonly BindableProperty RatingShapeOutlineThicknessProperty = BindableProperty.Create(nameof(RatingShapeOutlineThickness), typeof(double), typeof(RatingView), defaultValue: RatingViewDefaults.RatingShapeOutlineThickness, propertyChanged: OnShapeOutlineThicknessChanged);

	/// <summary>The backing store for the <see cref="CurrentRating" /> bindable property.</summary>
	public static readonly BindableProperty CurrentRatingProperty = BindableProperty.Create(nameof(CurrentRating), typeof(double), typeof(RatingView), defaultValue: RatingViewDefaults.CurrentRating, propertyChanged: OnUpdateRatingDraw);

	/// <summary>The backing store for the <see cref="CustomShape" /> bindable property.</summary>
	public readonly BindableProperty CustomShapeProperty = BindableProperty.Create(nameof(CustomShape), typeof(string), typeof(RatingView), defaultValue: null);

	/// <summary>The backing store for the <see cref="EmptyBackgroundColor" /> bindable property.</summary>
	public static readonly BindableProperty EmptyBackgroundColorProperty = BindableProperty.Create(nameof(EmptyBackgroundColor), typeof(Color), typeof(RatingView), defaultValue: RatingViewDefaults.EmptyBackgroundColor, propertyChanged: OnUpdateRatingDraw);

	/// <summary>The backing store for the <see cref="FilledBackgroundColor" /> bindable property.</summary>
	public static readonly BindableProperty FilledBackgroundColorProperty = BindableProperty.Create(nameof(FilledBackgroundColor), typeof(Color), typeof(RatingView), defaultValue: RatingViewDefaults.FilledBackgroundColor, propertyChanged: OnUpdateRatingDraw);

	/// <summary>The backing store for the <see cref="IsEnabled" /> bindable property.</summary>
	public static new readonly BindableProperty IsEnabledProperty = BindableProperty.Create(nameof(IsEnabled), typeof(bool), typeof(RatingView), defaultValue: RatingViewDefaults.IsEnabled, propertyChanged: OnIsEnabledChanged);

	/// <summary>The backing store for the <see cref="MaximumRating" /> bindable property.</summary>
	public static readonly BindableProperty MaximumRatingProperty = BindableProperty.Create(nameof(MaximumRating), typeof(byte), typeof(RatingView), defaultValue: RatingViewDefaults.MaximumRating, validateValue: maximumRatingValidator, propertyChanged: OnMaximumRatingChange);

	/// <summary>The backing store for the <see cref="Size" /> bindable property.</summary>
	public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(double), typeof(RatingView), defaultValue: RatingViewDefaults.Size, propertyChanged: OnSizeChanged);

	/// <summary>The backing store for the <see cref="Spacing" /> bindable property.</summary>
	public static readonly BindableProperty SpacingProperty = BindableProperty.Create(nameof(Spacing), typeof(double), typeof(RatingView), defaultValue: RatingViewDefaults.Spacing, propertyChanged: OnSpacingChanged);

	/// <summary>The backing store for the <see cref="Shape" /> bindable property.</summary>
	public readonly BindableProperty ShapeProperty = BindableProperty.Create(nameof(Shape), typeof(RatingViewShape), typeof(RatingView), defaultValue: RatingViewShape.Star, propertyChanged: OnShapePropertyChanged);

	static readonly WeakEventManager weakEventManager = new();
	Microsoft.Maui.Controls.Shapes.Path[] shapes;

	///<summary>The default constructor of the control.</summary>
	public RatingView()
	{
		ControlTemplate = new ControlTemplate(typeof(Grid));
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

	/// <summary>Gets or sets the rating border property.</summary>
	public Border RatingBorder
	{
		get => (Border)GetValue(RatingBorderProperty);
		set => SetValue(RatingBorderProperty, value);
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

	///<summary>The control to be displayed</summary>
	public Grid? Control { get; private set; }

	/// <summary>Gets or sets a value of the control current rating property.</summary>
	public double CurrentRating
	{
		get => (double)GetValue(CurrentRatingProperty);
		set => SetValue(CurrentRatingProperty, value);
	}

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
		set => SetValue(MaximumRatingProperty, value);
	}

	///<summary>Defines the shape to be drawn.</summary>
	public RatingViewShape Shape
	{
		get => (RatingViewShape)GetValue(ShapeProperty);
		set => SetValue(ShapeProperty, value);
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
		if (Control is null && child is Grid grid)
		{
			Control = grid;
			OnControlInitialized();
		}

		base.OnChildAdded(child);
	}

	static bool maximumRatingValidator(BindableObject bindable, object value)
	{
		return (byte)value <= RatingViewDefaults.MaximumRatings;
	}

	static void OnRatingBorderChanged(BindableObject bindable, object oldValue, object newValue)
	{
		// TODO: Need to add the Border to the child controls of the grid, so as to allow true styling (such as TripAdvisor ratings)!
	}

	static void OnSpacingChanged(BindableObject bindable, object oldValue, object newValue)
	{
		RatingView ratingView = (RatingView)bindable;
		if (ratingView.Control is Grid grid)
		{
			grid.ColumnSpacing = (double)newValue;
		}
	}

	static void OnSizeChanged(BindableObject bindable, object oldValue, object newValue)
	{
		RatingView ratingView = (RatingView)bindable;
		if (ratingView.Control is Grid grid)
		{
			double newSize = (double)newValue;
			for (int column = 0; column < grid.Count; column++)
			{
				grid.ColumnDefinitions[column] = new ColumnDefinition { Width = newSize };
				Microsoft.Maui.Controls.Shapes.Path rating = (Microsoft.Maui.Controls.Shapes.Path)grid.Children[column];
				rating.WidthRequest = newSize;
				rating.HeightRequest = newSize;
				ratingView.shapes[column].WidthRequest = newSize;
				ratingView.shapes[column].HeightRequest = newSize;
			}
		}
	}

	static void OnShapeOutlineColorChanged(BindableObject bindable, object oldValue, object newValue)
	{
		RatingView ratingView = (RatingView)bindable;
		if (ratingView.Control is Grid grid)
		{
			Color newColor = (Color)newValue;
			for (int column = 0; column < grid.Count; column++)
			{
				Microsoft.Maui.Controls.Shapes.Path rating = (Microsoft.Maui.Controls.Shapes.Path)grid.Children[column];
				rating.Stroke = newColor;
				ratingView.shapes[column].Stroke = newColor;
			}
		}
	}

	static void OnShapeOutlineThicknessChanged(BindableObject bindable, object oldValue, object newValue)
	{
		RatingView ratingView = (RatingView)bindable;
		if (ratingView.Control is Grid grid)
		{
			double newThickness = (double)newValue;
			for (int column = 0; column < grid.Count; column++)
			{
				Microsoft.Maui.Controls.Shapes.Path rating = (Microsoft.Maui.Controls.Shapes.Path)grid.Children[column];
				rating.StrokeThickness = newThickness;
				ratingView.shapes[column].StrokeThickness = newThickness;
			}
		}
	}

	static void OnIsEnabledChanged(BindableObject bindable, object oldValue, object newValue)
	{
		RatingView ratingView = (RatingView)bindable;
		ratingView.OnPropertyChanged(nameof(ratingView.IsEnabled));
		ratingView.HandleIsEnabledChanged();
	}

	/// <summary>Shape property changed.</summary>
	/// <param name="bindable">RatingView object.</param>
	/// <param name="oldValue">Old shape value.</param>
	/// <param name="newValue">New shape value.</param>
	static void OnShapePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		RatingView ratingView = (RatingView)bindable;
		ratingView.ReDraw();
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
		RatingView ratingView = (RatingView)bindable;
		if ((byte)newValue < ratingView.CurrentRating)
		{
			ratingView.CurrentRating = (byte)newValue;
			weakEventManager.HandleEvent(ratingView, EventArgs.Empty, nameof(RatingChanged));
		}

		ratingView.ReDraw();
	}

	static void OnUpdateRatingDraw(BindableObject bindable, object oldValue, object newValue)
	{
		RatingView ratingView = (RatingView)bindable;
		ratingView.UpdateRatingDraw();
	}

	/// <summary>Add the required children to the control.</summary>
	void AddChildrenToControl()
	{
		string shape = Shape switch
		{
			RatingViewShape.Heart => Core.Primitives.RatingViewShape.Heart.PathData,
			RatingViewShape.Circle => Core.Primitives.RatingViewShape.Circle.PathData,
			RatingViewShape.Like => Core.Primitives.RatingViewShape.Like.PathData,
			RatingViewShape.Dislike => Core.Primitives.RatingViewShape.Dislike.PathData,
			RatingViewShape.Custom => CustomShape ?? Core.Primitives.RatingViewShape.Star.PathData,
			_ => Core.Primitives.RatingViewShape.Star.PathData,
		};

		for (int i = 0; i < MaximumRating; i++)
		{
			Control?.ColumnDefinitions.Add(new ColumnDefinition { Width = Size });
			Microsoft.Maui.Controls.Shapes.Path image = new()
			{
				Data = (Geometry?)new PathGeometryConverter().ConvertFromInvariantString(shape),
				Fill = i <= CurrentRating ? FilledBackgroundColor : EmptyBackgroundColor,
				Stroke = RatingShapeOutlineColor,
				StrokeLineJoin = PenLineJoin.Round,
				StrokeLineCap = PenLineCap.Round,
				StrokeThickness = RatingShapeOutlineThickness,
				Aspect = Stretch.Uniform,
				HeightRequest = Size,
				WidthRequest = Size
			};
			if (IsEnabled)
			{
				TapGestureRecognizer tapGestureRecognizer = new();
				tapGestureRecognizer.Tapped += OnShapeTapped;
				image.GestureRecognizers.Add(tapGestureRecognizer);
			}

			Control?.Children.Add(image);
			Control?.SetColumn(image, i);

			shapes[i] = image;
		}

		UpdateRatingDraw();
	}

	/// <summary>Ensure VisualElement.IsEnabled always matches RatingView.IsEnabled, and remove any gesture recognisers added</summary>
	void HandleIsEnabledChanged()
	{
		base.IsEnabled = IsEnabled;
		if (!IsEnabled)
		{
			for (int i = 0; i < Control?.Children.Count; i++)
			{
				Microsoft.Maui.Controls.Shapes.Path rankControl = (Microsoft.Maui.Controls.Shapes.Path)Control.Children[i];
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
			Control.ColumnSpacing = Spacing;
		}
	}

	/// <summary>Shape tapped event.</summary>
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

		int columnIndex = Control.GetColumn(tappedShape);
		double originalRating = CurrentRating;
		if (MaximumRating > 1)
		{
			CurrentRating = columnIndex + 1;
		}

		if (!originalRating.Equals(CurrentRating))
		{
			UpdateRatingDraw();
			weakEventManager.HandleEvent(this, e, nameof(RatingChanged));
		}
	}

	/// <summary>Re-draw the control.</summary>
	void ReDraw()
	{
		Control?.Children.Clear();
		Control?.ColumnDefinitions.Clear();
		shapes = new Microsoft.Maui.Controls.Shapes.Path[MaximumRating];
		AddChildrenToControl();
	}

	/// <summary>Update the drawing of the controls ratings.</summary>
	void UpdateRatingDraw()
	{
		for (int i = 0; i < MaximumRating; i++)
		{
			Microsoft.Maui.Controls.Shapes.Path image = shapes[i];
			if (CurrentRating >= i + 1)
			{
				image.Stroke = RatingShapeOutlineColor;
				image.Fill = FilledBackgroundColor;
				continue;
			}

			if (CurrentRating % 1 is 0)
			{
				image.Fill = EmptyBackgroundColor;
				image.Stroke = RatingShapeOutlineColor;
				continue;
			}

			double fraction = CurrentRating - Math.Floor(CurrentRating);
			Microsoft.Maui.Controls.Shapes.Path element = shapes[(int)(CurrentRating - fraction)];
			GradientStopCollection colors =
			[
				new(FilledBackgroundColor, (float)fraction),
				new(EmptyBackgroundColor, (float)fraction)
			];

			element.Fill = new LinearGradientBrush(colors, new Point(0, 0), new Point(1, 0));
			element.Stroke = RatingShapeOutlineColor;
		}
	}
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