using System.Windows.Input;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Core.Primitives.Defaults;
using Microsoft.Maui.Controls.Shapes;

namespace CommunityToolkit.Maui.Views.RatingView;

/// <summary>RatingView control.</summary>
public class RatingView : BaseTemplatedView<Grid>
{
	Microsoft.Maui.Controls.Shapes.Path[] shapes;

	string shape = string.Empty;


	/// <summary> Rating value bindable property <see cref="CurrentRating"/> </summary>
	public static readonly BindableProperty CurrentRatingProperty = BindableProperty.Create(nameof(CurrentRating),
		typeof(double), typeof(RatingView), defaultValue: 0.0, propertyChanged: OnBindablePropertyChanged);


	/// <summary>MaximumRating rating value Bindable property </summary>
	public static readonly BindableProperty MaximumRatingProperty = BindableProperty.Create(nameof(MaximumRating),
		typeof(int), typeof(RatingView), defaultValue: 5, propertyChanged: OnBindablePropertyChanged);


	/// <summary>Shape size Bindable property</summary>
	public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(double),
		typeof(RatingView), defaultValue: 20.0, propertyChanged: OnBindablePropertyChanged);


	/// <summary>Shape filled Color Bindable property.</summary>
	public static readonly BindableProperty FilledBackgroundColorProperty =
		BindableProperty.Create(nameof(FilledBackgroundColor), typeof(Color), typeof(RatingView),
			defaultValue: Colors.Yellow, propertyChanged: OnBindablePropertyChanged);

	///<summary>Shape empty Color Bindable property.</summary>
	public static readonly BindableProperty EmptyBackgroundColorProperty =
		BindableProperty.Create(nameof(EmptyBackgroundColor), typeof(Color), typeof(RatingView),
			defaultValue: Colors.White, propertyChanged: OnBindablePropertyChanged);

	///<summary>Shape stroke color bindable property.</summary>
	public static readonly BindableProperty StrokeColorProperty = BindableProperty.Create(nameof(StrokeColor),
		typeof(Color), typeof(RatingView), defaultValue: Colors.Gray, propertyChanged: OnBindablePropertyChanged);


	///<summary>Shapes space between bindable property.</summary>
	public static readonly BindableProperty SpacingProperty = BindableProperty.Create(nameof(Spacing), typeof(double),
		typeof(RatingView), defaultValue: 10.0, propertyChanged: OnBindablePropertyChanged);

	///<summary>Shape clickable property.</summary>
	public static readonly BindableProperty ShouldAllowRatingProperty =
		BindableProperty.Create(nameof(ShouldAllowRating), typeof(bool), typeof(RatingView), defaultValue: false,
			propertyChanged: OnBindablePropertyChanged);

	///<summary>The shape to be drawn bindable property.</summary>
	public readonly BindableProperty ShapeProperty = BindableProperty.Create(nameof(Shape), typeof(RatingShape),
		typeof(RatingView), propertyChanged: OnShapePropertyChanged);

	///<summary>Shape stroke thinckness bindable property.</summary>
	public static readonly BindableProperty StrokeThicknessProperty = BindableProperty.Create(nameof(StrokeThickness),
		typeof(double), typeof(RatingView), defaultValue: 7.0, propertyChanged: OnBindablePropertyChanged);


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

	///<summary>Defines if the drwan shapes can be clickable to rate.</summary>
	public bool ShouldAllowRating
	{
		get => (bool)GetValue(ShouldAllowRatingProperty);
		set => SetValue(ShouldAllowRatingProperty, value);
	}

	///<summary>Defines the shape to be drawn.</summary>
	public RatingShape Shape
	{
		get => (RatingShape)GetValue(ShapeProperty);
		set => SetValue(ShapeProperty, value);
	}


	///<summary>The default constructor of the control.</summary>
	public RatingView()
	{
		shapes = new Microsoft.Maui.Controls.Shapes.Path[MaximumRating];


		HorizontalOptions = LayoutOptions.CenterAndExpand;

		InitializeShape();

		Draw();
	}

	///<summary>Event raised whenever the bindable property changes.</summary>
	public static void OnBindablePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		//re-draw forms.
		((RatingView)bindable).ReDraw();
	}


	///<summary>Event raised whenever the shape bindable property changes.</summary>
	public static void OnShapePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((RatingView)bindable).InitializeShape();
		((RatingView)bindable).ReDraw();
	}


	///<summary>Event raised whenever the Shape is tapped or clicked.</summary>
	public void OnShapeTapped(object? sender, TappedEventArgs? e)
	{
		var tappedShape = sender as Microsoft.Maui.Controls.Shapes.Path;


		if (tappedShape is null)
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


	///<summary>The base method that draw the shape at the control initialization state.</summary>
	public void Draw()
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


			if (ShouldAllowRating)
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

	///<summary>Redraw the shapes on bindable changed event raised.</summary>
	public void ReDraw()
	{
		Control?.Children.Clear();

		Control?.ColumnDefinitions.Clear();

		shapes = new Microsoft.Maui.Controls.Shapes.Path[MaximumRating];

		Draw();

		UpdateDraw();
	}

	///<summary>Called everytime the shape is tapped to fill it or empty depending on the rating value.</summary>
	public void UpdateDraw()
	{
		for (int i = 0; i < MaximumRating; i++)
		{
			Microsoft.Maui.Controls.Shapes.Path image = shapes[i];

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
				if (CurrentRating % 1 == 0)
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

	///<summary>Initialize the shapes and the control.</summary>
	public void InitializeShape()
	{
		if (Control is not null)
		{
			this.Control.ColumnSpacing = Spacing;
		}

		this.StrokeColor = Colors.Transparent;

		shape = Shape.PathData;
	}


	///<inheritdoc cref="BaseTemplatedView{TControl}"/>
	protected override void OnControlInitialized(Grid control)
	{
		shapes = new Microsoft.Maui.Controls.Shapes.Path[MaximumRating];


		HorizontalOptions = LayoutOptions.CenterAndExpand;

		if (this.Control is not null)
		{
			this.Control.ColumnSpacing = Spacing;
		}

		Draw();
	}
}