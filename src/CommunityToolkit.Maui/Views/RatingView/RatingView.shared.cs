// Ignore Spelling: color, bindable

using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Shapes;

namespace CommunityToolkit.Maui.Views;

/// <summary>Rating view control.</summary>
public class RatingView : TemplatedView, IContentView, IRatingViewShape
{
	/// <summary>Bindable property for <see cref="CustomShape"/> bindable property.</summary>
	public static readonly BindableProperty CustomShapeProperty = RatingViewItemElement.CustomShapeProperty;

	// TODO: Add BindableProperty to allow the developer to define and style the Item Border, then adjust the control creation accordingly.

	// TODO: Move to RatingViewItemElement
	/// <summary>The backing store for the <see cref="EmptyBackgroundColor" /> bindable property.</summary>
	public static readonly BindableProperty EmptyBackgroundColorProperty = BindableProperty.Create(nameof(EmptyBackgroundColor), typeof(Color), typeof(RatingView), defaultValue: RatingViewDefaults.EmptyBackgroundColor, propertyChanged: OnUpdateRatingDraw);

	// TODO: Move to RatingViewItemElement
	/// <summary>The backing store for the <see cref="FilledBackgroundColor" /> bindable property.</summary>
	public static readonly BindableProperty FilledBackgroundColorProperty = BindableProperty.Create(nameof(FilledBackgroundColor), typeof(Color), typeof(RatingView), defaultValue: RatingViewDefaults.FilledBackgroundColor, propertyChanged: OnUpdateRatingDraw);

	/// <summary>The backing store for the <see cref="IsEnabled" /> bindable property.</summary>
	public new static readonly BindableProperty IsEnabledProperty = BindableProperty.Create(nameof(IsEnabled), typeof(bool), typeof(RatingView), defaultValue: RatingViewDefaults.IsEnabled, propertyChanged: OnIsEnabledChanged);

	// TODO: Add some kind of Roslyn Analizer to validate at design/build time that the maximumrating is in bounds (1-25).
	/// <summary>The backing store for the <see cref="MaximumRating" /> bindable property.</summary>
	public static readonly BindableProperty MaximumRatingProperty = BindableProperty.Create(nameof(MaximumRating), typeof(byte), typeof(RatingView), defaultValue: RatingViewDefaults.MaximumRating, validateValue: maximumRatingValidator, propertyChanged: OnMaximumRatingChange);

	/// <summary>The backing store for the <see cref="RatingFill" /> bindable property.</summary>
	public static readonly BindableProperty RatingFillProperty = BindableProperty.Create(nameof(RatingFill), typeof(RatingFillElement), typeof(RatingView), defaultValue: RatingFillElement.Shape, propertyChanged: OnUpdateRatingDraw);

	/// <summary>The backing store for the <see cref="Rating" /> bindable property.</summary>
	public static readonly BindableProperty RatingProperty = BindableProperty.Create(nameof(Rating), typeof(double), typeof(RatingView), defaultValue: RatingViewDefaults.DefaultRating, propertyChanged: OnUpdateRatingDraw);

	/// <summary>The backing store for the <see cref="ShapeBorderColor" /> bindable property.</summary>
	public static readonly BindableProperty ShapeBorderColorProperty = RatingViewItemElement.ShapeBorderColorProperty;

	/// <summary>The backing store for the <see cref="ShapeBorderThickness" /> bindable property.</summary>
	public static readonly BindableProperty ShapeBorderThicknessProperty = RatingViewItemElement.ShapeBorderThicknessProperty;

	/// <summary>Bindable property for <see cref="ItemPadding"/> bindable property.</summary>
	public static readonly BindableProperty ShapePaddingProperty = RatingViewItemElement.ItemPaddingProperty;

	/// <summary>The backing store for the <see cref="ItemShapeSize" /> bindable property.</summary>
	public static readonly BindableProperty SizeProperty = RatingViewItemElement.SizeProperty;

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

	/// <summary>Defines the shape padding thickness.</summary>
	public Thickness ItemPadding
	{
		get => (Thickness)GetValue(ShapePaddingProperty);
		set => SetValue(ShapePaddingProperty, value);
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
			ClearAndReDraw();
		}
	}

	void IRatingViewShape.OnItemPaddingPropertyChanged(Thickness oldValue, Thickness newValue)
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

	void IRatingViewShape.OnItemShapeBorderColorChanged(Color oldValue, Color newValue)
	{
		if (newValue is null)
		{
			return;
		}

		if (Control is HorizontalStackLayout stack)
		{
			for (int column = 0; column < stack.Count; column++)
			{
				Microsoft.Maui.Controls.Shapes.Path rating = (Microsoft.Maui.Controls.Shapes.Path)((Border)stack.Children[column]).Content!.GetVisualTreeDescendants()[0];
				rating.Stroke = newValue;
				shapes[column].Stroke = newValue;
			}
		}
	}

	void IRatingViewShape.OnItemShapeBorderThicknessChanged(double oldValue, double newValue)
	{
		if (Control is HorizontalStackLayout stack)
		{
			for (int column = 0; column < stack.Count; column++)
			{
				Microsoft.Maui.Controls.Shapes.Path rating = (Microsoft.Maui.Controls.Shapes.Path)((Border)stack.Children[column]).Content!.GetVisualTreeDescendants()[0];
				rating.StrokeThickness = newValue;
				shapes[column].StrokeThickness = newValue;
			}
		}
	}

	void IRatingViewShape.OnItemShapePropertyChanged(RatingViewShape oldValue, RatingViewShape newValue)
	{
		if (Control is null)
		{
			return;
		}

		ClearAndReDraw();
	}

	void IRatingViewShape.OnItemShapeSizeChanged(double oldValue, double newValue)
	{
		if (Control is HorizontalStackLayout stack)
		{
			for (int column = 0; column < stack.Count; column++)
			{
				Microsoft.Maui.Controls.Shapes.Path rating = (Microsoft.Maui.Controls.Shapes.Path)((Border)stack.Children[column]).Content!.GetVisualTreeDescendants()[0];
				rating.WidthRequest = newValue;
				rating.HeightRequest = newValue;
				rating.Margin = ItemPadding;
				shapes[column].WidthRequest = newValue;
				shapes[column].HeightRequest = newValue;
			}
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
				Padding = ItemPadding,
				Margin = 0,
				StrokeThickness = 0
			};
			Microsoft.Maui.Controls.Shapes.Path image = new()
			{
				Aspect = Stretch.Uniform,
				Data = (Geometry?)new PathGeometryConverter().ConvertFromInvariantString(shape),
				HeightRequest = ItemShapeSize,
				Margin = ItemPadding,
				Stroke = ShapeBorderColor,
				StrokeLineCap = PenLineCap.Round,
				StrokeLineJoin = PenLineJoin.Round,
				StrokeThickness = ShapeBorderThickness,
				WidthRequest = ItemShapeSize,
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
				image.Stroke = ShapeBorderColor;
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

				image.Stroke = ShapeBorderColor;
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

			element.Stroke = ShapeBorderColor;
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