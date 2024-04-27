using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;
using Microsoft.Maui.Controls.Shapes;

namespace CommunityToolkit.Maui.Views.RatingView;

/// <summary>RatingView control.</summary>
public class RatingView : TemplatedView
{
	/// <summary> Rating value bindable property <see cref="CurrentRating"/> </summary>
	public static readonly BindableProperty CurrentRatingProperty = BindableProperty.Create(nameof(CurrentRating),
		typeof(double), typeof(RatingView), defaultValue: RatingViewDefaults.CurrentRating, propertyChanged: OnBindablePropertyChanged);


	/// <summary>MaximumRating rating value Bindable property </summary>
	public static readonly BindableProperty MaximumRatingProperty = BindableProperty.Create(nameof(MaximumRating),
		typeof(int), typeof(RatingView), defaultValue: RatingViewDefaults.MaximumRating, propertyChanged: OnBindablePropertyChanged);


	/// <summary>Shape size Bindable property</summary>
	public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(double),
		typeof(RatingView), defaultValue: RatingViewDefaults.Size, propertyChanged: OnBindablePropertyChanged);


	/// <summary>Shape filled Color Bindable property.</summary>
	public static readonly BindableProperty FilledBackgroundColorProperty = BindableProperty.Create(nameof(FilledBackgroundColor), 
		typeof(Color), typeof(RatingView), defaultValue: RatingViewDefaults.FilledBackgroundColor, propertyChanged: OnBindablePropertyChanged);

	///<summary>Shape empty Color Bindable property.</summary>
	public static readonly BindableProperty EmptyBackgroundColorProperty = BindableProperty.Create(nameof(EmptyBackgroundColor), 
		typeof(Color), typeof(RatingView), defaultValue: RatingViewDefaults.EmptyBackgroundColor, propertyChanged: OnBindablePropertyChanged);

	///<summary>Shape stroke color bindable property.</summary>
	public static readonly BindableProperty StrokeColorProperty = BindableProperty.Create(nameof(StrokeColor),
		typeof(Color), typeof(RatingView), defaultValue: RatingViewDefaults.StrokeColor, propertyChanged: OnBindablePropertyChanged);

	///<summary>Shapes space between bindable property.</summary>
	public static readonly BindableProperty SpacingProperty = BindableProperty.Create(nameof(Spacing), typeof(double),
		typeof(RatingView), defaultValue: RatingViewDefaults.Spacing, propertyChanged: OnBindablePropertyChanged);

	///<summary>Shape clickable property.</summary>
	public static new readonly BindableProperty IsEnabledProperty = BindableProperty.Create(nameof(IsEnabled), 
		typeof(bool), typeof(RatingView), defaultValue: false, propertyChanged: OnIsEnabledChanged);

	///<summary>The shape to be drawn bindable property.</summary>
	public readonly BindableProperty ShapeProperty = BindableProperty.Create(nameof(Shape), typeof(RatingShape),
		typeof(RatingView), propertyChanged: OnShapePropertyChanged);

	///<summary>Shape stroke thickness bindable property.</summary>
	public static readonly BindableProperty StrokeThicknessProperty = BindableProperty.Create(nameof(StrokeThickness),
		typeof(double), typeof(RatingView), defaultValue: RatingViewDefaults.StrokeThickness, propertyChanged: OnBindablePropertyChanged);
	
	Microsoft.Maui.Controls.Shapes.Path[] shapes;

	string shape = string.Empty;
	
	///<summary>The default constructor of the control.</summary>
	public RatingView()
	{
		ControlTemplate = new ControlTemplate(typeof(Grid));
		
		shapes = new Microsoft.Maui.Controls.Shapes.Path[MaximumRating];

		HorizontalOptions = LayoutOptions.CenterAndExpand;

		InitializeShape();

		Draw();
	}

	///<summary>Method called everytime the control's Binding Context is changed.</summary>
	protected override void OnBindingContextChanged()
	{
		base.OnBindingContextChanged();

		if (Control is not null)
		{
			Control.BindingContext = BindingContext;
		}
	}

	///<summary>Called everytime a child is added to the control.</summary>
	protected override void OnChildAdded(Element child)
	{
		if (Control is null && child is Grid grid)
		{
			Control = grid;
			OnControlInitialized();
		}

		base.OnChildAdded(child);
	}

	///<summary>Defines the current rating value.</summary>
	public double CurrentRating
	{
		get => (double)GetValue(CurrentRatingProperty);
		set => SetValue(CurrentRatingProperty, value);
	}

	///<summary>Defines the maximum value allowed for the rating.</summary>
	public int MaximumRating
	{
		get => (int)GetValue(MaximumRatingProperty);
		set => SetValue(MaximumRatingProperty, value);
	}

	///<summary>Defines the size of each drawn shapes.</summary>
	public double Size
	{
		get => (double)GetValue(SizeProperty);
		set => SetValue(SizeProperty, value);
	}

	///<summary>Defines the color to fill with the drawn shape.</summary>
	public Color FilledBackgroundColor
	{
		get => (Color)GetValue(FilledBackgroundColorProperty);
		set => SetValue(FilledBackgroundColorProperty, value);
	}

	///<summary>Defines the color of the drawn shape is not filled.</summary>
	public Color EmptyBackgroundColor
	{
		get => (Color)GetValue(EmptyBackgroundColorProperty);
		set => SetValue(EmptyBackgroundColorProperty, value);
	}

	///<summary>Defines the color of the shape's stroke(border).</summary>
	public Color StrokeColor
	{
		get => (Color)GetValue(StrokeColorProperty);
		set => SetValue(StrokeColorProperty, value);
	}

	///<summary>Defines the thickness of the shape's stroke(border).</summary>
	public double StrokeThickness
	{
		get => (double)GetValue(StrokeThicknessProperty);
		set => SetValue(StrokeThicknessProperty, value);
	}

	///<summary>Defines the space between the drawn shapes.</summary>
	public double Spacing
	{
		get => (double)GetValue(SpacingProperty);
		set => SetValue(SpacingProperty, value);
	}

	///<summary>Defines if the drawn shapes can be clickable to rate.</summary>
	public new bool IsEnabled
	{
		get => (bool)GetValue(IsEnabledProperty);
		set => SetValue(IsEnabledProperty, value);
	}

	///<summary>Defines the shape to be drawn.</summary>
	public RatingShape Shape
	{
		get => (RatingShape)GetValue(ShapeProperty);
		set => SetValue(ShapeProperty, value);
	}
	
	///<summary>The control to be displayed</summary>
	public Grid? Control { get; private set; }
	
	static void OnIsEnabledChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var ratingView = (RatingView)bindable;
		ratingView.HandleIsEnabledChanged();
		ratingView.ReDraw();
	}
	
	static void OnBindablePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((RatingView)bindable).ReDraw();
	}
	
	static void OnShapePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((RatingView)bindable).InitializeShape();
		((RatingView)bindable).ReDraw();
	}
	
	void OnShapeTapped(object? sender, TappedEventArgs? e)
	{
		if (sender is not Microsoft.Maui.Controls.Shapes.Path tappedShape)
		{
			return;
		}

		if (Control is not null)
		{
			int columnIndex = Control.GetColumn(tappedShape);
			
			if (MaximumRating > 1)
			{
				CurrentRating = columnIndex + 1;
			}
		}
	}
	
	void Draw()
	{
		for (int i = 0; i < MaximumRating; i++)
		{
			Control?.ColumnDefinitions.Add(new ColumnDefinition { Width = Size });

			Microsoft.Maui.Controls.Shapes.Path image = new()
			{
				Data = (Geometry?)new PathGeometryConverter().ConvertFromInvariantString(shape)
			};

			if (i <= CurrentRating)
			{
				image.Fill = FilledBackgroundColor;
				image.Stroke = FilledBackgroundColor;
				image.Aspect = Stretch.Uniform;
				image.HeightRequest = Size;
				image.WidthRequest = Size;
			}
			else
			{
				image.Fill = EmptyBackgroundColor;
				image.Stroke = StrokeColor;

				image.StrokeLineJoin = PenLineJoin.Round;
				image.StrokeLineCap = PenLineCap.Round;
				image.StrokeThickness = StrokeThickness;

				image.Aspect = Stretch.Uniform;
				image.HeightRequest = Size;
				image.WidthRequest = Size;
			}


			if (IsEnabled)
			{
				var tapGestureRecognizer = new TapGestureRecognizer();
				tapGestureRecognizer.Tapped += OnShapeTapped;
				image.GestureRecognizers.Add(tapGestureRecognizer);
			}

			Control?.Children.Add(image);
			Control?.SetColumn(image, i);

			shapes[i] = image;
		}


		UpdateDraw();
	}
	
	void ReDraw()
	{
		Control?.Children.Clear();

		Control?.ColumnDefinitions.Clear();

		shapes = new Microsoft.Maui.Controls.Shapes.Path[MaximumRating];

		Draw();

		UpdateDraw();
	}

	void UpdateDraw()
	{
		for (int i = 0; i < MaximumRating; i++)
		{
			var image = shapes[i];

			if (CurrentRating >= i + 1)
			{
				image.HeightRequest = Size;
				image.WidthRequest = Size;
				image.StrokeLineJoin = PenLineJoin.Round;
				image.StrokeThickness = StrokeThickness;
				image.Stroke = FilledBackgroundColor;
			}
			else
			{
				image.HeightRequest = Size;
				image.WidthRequest = Size;
				if (CurrentRating % 1 is 0)
				{
					image.Fill = EmptyBackgroundColor;
					image.Stroke = StrokeColor;
					image.StrokeThickness = StrokeThickness;
					image.StrokeLineJoin = PenLineJoin.Round;
					image.StrokeLineCap = PenLineCap.Round;
				}
				else
				{
					var fraction = CurrentRating - Math.Floor(CurrentRating);
					var element = shapes[(int)(CurrentRating - fraction)];
					{
						var colors = new GradientStopCollection
						{
							new(FilledBackgroundColor, (float)fraction),
							new(EmptyBackgroundColor, (float)fraction)
						};

						element.Fill =
							new LinearGradientBrush(colors, new Point(0, 0), new Point(1, 0));
						element.StrokeThickness = StrokeThickness;
						element.StrokeLineJoin = PenLineJoin.Round;
						element.Stroke = StrokeColor;
					}
				}
			}
		}
	}
	
	void InitializeShape()
	{
		if (Control is not null)
		{
			Control.ColumnSpacing = Spacing;
		}

		StrokeColor = Colors.Transparent;

		shape = Shape.PathData;
	}

	// Ensure VisualElement.IsEnabled always matches RatingView.IsEnabled 
	void HandleIsEnabledChanged()
	{
		base.IsEnabled = IsEnabled;
	}
	
	void OnControlInitialized()
	{
		shapes = new Microsoft.Maui.Controls.Shapes.Path[MaximumRating];

		HorizontalOptions = LayoutOptions.CenterAndExpand;

		if (Control is not null)
		{
			Control.ColumnSpacing = Spacing;
		}

		Draw();
	}
}