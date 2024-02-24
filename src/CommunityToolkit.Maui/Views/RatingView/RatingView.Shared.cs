using System.Windows.Input;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Core.Primitives.Defaults;
using Microsoft.Maui.Controls.Shapes;

namespace CommunityToolkit.Maui.Views.RatingView;

/// <summary>RatingView control.</summary>
public class RatingView : Border
{

	Microsoft.Maui.Controls.Shapes.Path[] shapes;

	string shape = string.Empty;

	Grid innerContent = new();


	/// <summary> Rating value bindable property <see cref="CurrentRating"/> </summary>
	public static readonly BindableProperty CurrentRatingProperty = BindableProperty.Create(nameof(CurrentRating), typeof(double), typeof(RatingView), defaultValue: 0.0, propertyChanged: OnBindablePropertyChanged);


	/// <summary>MaximumRating rating value Bindable property </summary>
	public static readonly BindableProperty MaximumRatingProperty = BindableProperty.Create(nameof(MaximumRating), typeof(int), typeof(RatingView), defaultValue: 5, propertyChanged: OnBindablePropertyChanged);


	/// <summary>Star size Bindable property</summary>
	public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(double), typeof(RatingView), defaultValue: 20.0, propertyChanged: OnBindablePropertyChanged);


	/// <summary>Star Color Bindable property.</summary>
	public static readonly BindableProperty FilledBackgroundColorProperty = BindableProperty.Create(nameof(FilledBackgroundColor), typeof(Color), typeof(RatingView), defaultValue: Colors.Yellow, propertyChanged: OnBindablePropertyChanged);

	///<summary></summary>
	public static readonly BindableProperty EmptyBackgroundColorProperty = BindableProperty.Create(nameof(EmptyBackgroundColor), typeof(Color), typeof(RatingView), defaultValue: Colors.White, propertyChanged: OnBindablePropertyChanged);

	///<summary></summary>
	public static readonly BindableProperty StrokeColorProperty = BindableProperty.Create(nameof(StrokeColor), typeof(Color), typeof(RatingView), defaultValue: Colors.Gray, propertyChanged: OnBindablePropertyChanged);


	///<summary></summary>
	public static readonly BindableProperty SpacingProperty = BindableProperty.Create(nameof(Spacing), typeof(double), typeof(RatingView), defaultValue: 10.0, propertyChanged: OnBindablePropertyChanged);

	///<summary></summary>
	public static readonly BindableProperty ShouldAllowRatingProperty = BindableProperty.Create(nameof(ShouldAllowRating), typeof(bool), typeof(RatingView), defaultValue: false, propertyChanged: OnBindablePropertyChanged);

	///<summary></summary>
	public static readonly BindableProperty CommandProperty = BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(RatingView), defaultValue: null, propertyChanged: OnBindablePropertyChanged);

	///<summary></summary>
	public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(RatingView), defaultValue: null, propertyChanged: OnBindablePropertyChanged);

	///<summary></summary>
	public readonly BindableProperty ShapeProperty = BindableProperty.Create(nameof(Shape), typeof(RatingShape), typeof(RatingView), propertyChanged: OnShapePropertyChanged);


	///<summary></summary>
	public double CurrentRating
	{
		get => (double)GetValue(CurrentRatingProperty);
		set => SetValue(CurrentRatingProperty, value);
	}

	///<summary></summary>
	public int MaximumRating
	{
		get => (int)GetValue(MaximumRatingProperty);
		set => SetValue(MaximumRatingProperty, value);
	}

	///<summary></summary>
	public double Size
	{
		get => (double)GetValue(SizeProperty);
		set => SetValue(SizeProperty, value);
	}

	///<summary></summary>
	public Color FilledBackgroundColor
	{
		get => (Color)GetValue(FilledBackgroundColorProperty);
		set => SetValue(FilledBackgroundColorProperty, value);
	}

	///<summary></summary>
	public Color EmptyBackgroundColor
	{
		get => (Color)GetValue(EmptyBackgroundColorProperty);
		set => SetValue(EmptyBackgroundColorProperty, value);
	}

	///<summary></summary>
	public Color StrokeColor
	{
		get => (Color)GetValue(StrokeColorProperty);
		set => SetValue(StrokeColorProperty, value);
	}

	///<summary></summary>
	public new double StrokeThickness
	{
		get => base.StrokeThickness;
		set => SetValue(StrokeThicknessProperty, value);
	}

	///<summary></summary>
	public double Spacing
	{
		get => (double)GetValue(SpacingProperty);
		set => SetValue(SpacingProperty, value);
	}

	///<summary></summary>
	public bool ShouldAllowRating
	{
		get => (bool)GetValue(ShouldAllowRatingProperty);
		set => SetValue(ShouldAllowRatingProperty, value);
	}

	///<summary></summary>
	public ICommand? Command
	{
		get => (ICommand)GetValue(CommandProperty);
		set => SetValue(CommandProperty, value);
	}

	///<summary></summary>
	public object? CommandParameter
	{
		get => GetValue(CommandParameterProperty);
		set => SetValue(CommandProperty, value);
	}

	///<summary></summary>
	public RatingShape Shape
	{
		get => (RatingShape)GetValue(ShapeProperty);
		set => SetValue(ShapeProperty, value);
	}

	///<summary></summary>
	public new View? Content { get => base.Content; internal set => base.Content = value; }

	///<summary></summary>
	public RatingView()
	{
		shapes = new Microsoft.Maui.Controls.Shapes.Path[MaximumRating];


		HorizontalOptions = LayoutOptions.CenterAndExpand;

		this.innerContent.ColumnSpacing = Spacing;

		InitializeShape();

		DrawBase();


		this.Stroke = Colors.Transparent;
	}

	static void OnBindablePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		//re-draw forms.
		((RatingView)bindable).ReDraw();
	}

	static void OnShapePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((RatingView)bindable).InitializeShape();
		((RatingView)bindable).ReDraw();
	}

	void OnShapeTapped(object? sender, TappedEventArgs? e)
	{
		var tappedShape = sender as Microsoft.Maui.Controls.Shapes.Path;


		if (tappedShape is null) { return; }

		var columnIndex = innerContent.GetColumn(tappedShape);


		if (MaximumRating > 1)
		{
			CurrentRating = columnIndex + 1;
		}

		var data = new Rating
		{
			CurrentRating = CurrentRating,
			CommandParameter = CommandParameter
		};

		if (Command is not null && Command.CanExecute(data))
		{
			Command.Execute(data);
		}
	}

	void DrawBase()
	{
		for (int i = 0; i < MaximumRating; i++)
		{
			innerContent?.ColumnDefinitions.Add(new ColumnDefinition { Width = Size });

			Microsoft.Maui.Controls.Shapes.Path image = new();
			image.Data = (Geometry)new PathGeometryConverter().ConvertFromInvariantString(shape);
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

			innerContent?.Children.Add(image);
			innerContent?.SetColumn(image, i);

			shapes[i] = image;
		}


		UpdateDraw();
	}

	void ReDraw()
	{
		innerContent?.Children.Clear();

		innerContent?.ColumnDefinitions.Clear();

		shapes = new Microsoft.Maui.Controls.Shapes.Path[MaximumRating];

		for (int i = 0; i < MaximumRating; i++)
		{
			innerContent?.ColumnDefinitions.Add(new ColumnDefinition { Width = Size });

			Microsoft.Maui.Controls.Shapes.Path image = new();
			image.Data = (Geometry)new PathGeometryConverter().ConvertFromInvariantString(shape);
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

			innerContent?.Children.Add(image);
			innerContent?.SetColumn(image, i);

			shapes[i] = image;
		}

		UpdateDraw();
	}

	void UpdateDraw()
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
					if (element is not null)
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

		Content = innerContent;
	}

	void InitializeShape()
	{
		shape = Shape.PathData;
	}
}
