// Ignore Spelling: color, bindable, colors

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Shapes;

namespace CommunityToolkit.Maui.Views;

/// <summary>Rating view control.</summary>
public class RatingView : TemplatedView, IRatingView
{
	/// <summary>Bindable property for <see cref="CustomShape"/> bindable property.</summary>
	public static readonly BindableProperty CustomShapeProperty = RatingViewItemElement.CustomShapeProperty;

	/// <summary>The backing store for the <see cref="EmptyColor" /> bindable property.</summary>
	public static readonly BindableProperty EmptyColorProperty = RatingViewItemElement.EmptyColorProperty;

	/// <summary>The backing store for the <see cref="FilledColor" /> bindable property.</summary>
	public static readonly BindableProperty FilledColorProperty = RatingViewItemElement.FilledColorProperty;

	/// <summary>The backing store for the <see cref="IsReadOnly" /> bindable property.</summary>
	public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(nameof(IsReadOnly), typeof(bool), typeof(RatingView), defaultValue: RatingViewDefaults.IsReadOnly, propertyChanged: OnIsReadOnlyChanged);

	/// <summary>Bindable property for <see cref="ItemPadding"/> bindable property.</summary>
	public static readonly BindableProperty ItemPaddingProperty = RatingViewItemElement.ItemPaddingProperty;

	// TODO: Add some kind of Roslyn Analyser to validate at design/build time that the MaximumRating is in bounds (1-25).
	/// <summary>The backing store for the <see cref="MaximumRating" /> bindable property.</summary>
	public static readonly BindableProperty MaximumRatingProperty = BindableProperty.Create(nameof(MaximumRating), typeof(byte), typeof(RatingView), defaultValue: RatingViewDefaults.MaximumRating, validateValue: maximumRatingValidator, propertyChanged: OnMaximumRatingChange);

	/// <summary>The backing store for the <see cref="RatingChangedCommand"/> bindable property.</summary>
	public static readonly BindableProperty RatingChangedCommandProperty = BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(RatingView));

	/// <summary>The backing store for the <see cref="RatingFill" /> bindable property.</summary>
	public static readonly BindableProperty RatingFillProperty = BindableProperty.Create(nameof(RatingFill), typeof(RatingFillElement), typeof(RatingView), defaultValue: RatingFillElement.Shape, propertyChanged: OnUpdateRatingDraw);

	/// <summary>The backing store for the <see cref="Rating" /> bindable property.</summary>
	public static readonly BindableProperty RatingProperty = BindableProperty.Create(nameof(Rating), typeof(double), typeof(RatingView), defaultValue: RatingViewDefaults.DefaultRating, validateValue: ratingValidator, propertyChanged: OnRatingChanged);

	/// <summary>The backing store for the <see cref="ShapeBorderColor" /> bindable property.</summary>
	public static readonly BindableProperty ShapeBorderColorProperty = RatingViewItemElement.ShapeBorderColorProperty;

	/// <summary>The backing store for the <see cref="ShapeBorderThickness" /> bindable property.</summary>
	public static readonly BindableProperty ShapeBorderThicknessProperty = RatingViewItemElement.ShapeBorderThicknessProperty;

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
		AddChildrenToControl(0, MaximumRating);
	}

	/// <summary>Occurs when <see cref="Rating"/> is changed.</summary>
	public event EventHandler<RatingChangedEventArgs> RatingChanged
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	///<summary>The control to be displayed</summary>
	public HorizontalStackLayout? Control { get; set; }

	///<summary>Defines the shape to be drawn.</summary>
	public string CustomShape
	{
		get => (string)GetValue(CustomShapeProperty);
		set => SetValue(CustomShapeProperty, value);
	}

	/// <summary>Gets or sets a value of the empty rating color property.</summary>
	public Color EmptyColor
	{
		get => (Color)GetValue(EmptyColorProperty);
		set => SetValue(EmptyColorProperty, value);
	}

	/// <summary>Gets or sets a value of the filled rating color property.</summary>
	public Color FilledColor
	{
		get => (Color)GetValue(FilledColorProperty);
		set => SetValue(FilledColorProperty, value);
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
		get => (double)GetValue(SizeProperty);
		set => SetValue(SizeProperty, value);
	}

	/// <summary>Gets or sets a value indicating the maximum rating.</summary>
	public byte MaximumRating
	{
		get => (byte)GetValue(MaximumRatingProperty);
		set => SetValue(MaximumRatingProperty, Math.Clamp(value, (byte)1, RatingViewDefaults.MaximumRatings));
	}

	/// <summary>Gets or sets a value indicating the rating.</summary>
	public double Rating
	{
		get => (double)GetValue(RatingProperty);
		set => SetValue(RatingProperty, Math.Clamp(value, 0.0, RatingViewDefaults.MaximumRatings));
	}

	/// <summary>Command that is triggered when the value of <see cref="Rating"/> is changed.</summary>
	public ICommand? RatingChangedCommand
	{
		get => (ICommand?)GetValue(RatingChangedCommandProperty);
		set => SetValue(RatingChangedCommandProperty, value);
	}

	/// <summary>Gets or sets a value indicating which element of the rating to fill.</summary>
	public RatingFillElement RatingFill
	{
		get => (RatingFillElement)GetValue(RatingFillProperty);
		set => SetValue(RatingFillProperty, value);
	}

	///<summary>Gets or sets a value indicating the rating shape.</summary>
	public RatingViewShape Shape
	{
		get => (RatingViewShape)GetValue(ShapeProperty);
		set => SetValue(ShapeProperty, value);
	}

	/// <summary>Gets or sets a value indicating the rating shape border color.</summary>
	public Color ShapeBorderColor
	{
		get => (Color)GetValue(ShapeBorderColorProperty);
		set => SetValue(ShapeBorderColorProperty, value);
	}

	///<summary>Gets or sets a value indicating the rating shape border thickness.</summary>
	public double ShapeBorderThickness
	{
		get => (double)GetValue(ShapeBorderThicknessProperty);
		set => SetValue(ShapeBorderThicknessProperty, value);
	}

	///<summary>Gets or sets a value indicating the space between rating items.</summary>
	public double Spacing
	{
		get => (double)GetValue(SpacingProperty);
		set => SetValue(SpacingProperty, value);
	}

	Color IRatingViewShape.EmptyColorDefaultValueCreator()
	{
		return RatingViewDefaults.EmptyColor;
	}

	Color IRatingViewShape.FilledColorDefaultValueCreator()
	{
		return RatingViewDefaults.FilledColor;
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
		if (Shape is not RatingViewShape.Custom)
		{
			return;
		}

		string newShapePathData;
		if (string.IsNullOrEmpty(newValue))
		{
			Shape = RatingViewDefaults.Shape;
			newShapePathData = Core.Primitives.RatingViewShape.Star.PathData;
		}
		else
		{
			newShapePathData = newValue!;
		}

		ChangeRatingItemShape(newShapePathData);
	}

	/// <summary>Change the rating item empty color.</summary>
	/// <param name="oldValue">Old rating item empty color.</param>
	/// <param name="newValue">new rating item empty color.</param>
	void IRatingViewShape.OnEmptyColorPropertyChanged(Color oldValue, Color newValue)
	{
		UpdateRatingDraw();
	}

	/// <summary>Change the rating item filled color.</summary>
	/// <param name="oldValue">Old rating item filled color.</param>
	/// <param name="newValue">new rating item filled color.</param>
	void IRatingViewShape.OnFilledColorPropertyChanged(Color oldValue, Color newValue)
	{
		UpdateRatingDraw();
	}

	/// <summary>Change the rating item padding.</summary>
	/// <param name="oldValue">Old rating item padding.</param>
	/// <param name="newValue">New rating item padding.</param>
	void IRatingViewShape.OnItemPaddingPropertyChanged(Thickness oldValue, Thickness newValue)
	{
		Debug.Assert(Control is not null);
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
		Debug.Assert(Control is not null);
		for (int element = 0; element < Control.Count; element++)
		{
			Border border = (Border)Control.Children[element];
			Debug.Assert(border.Content is not null);
			((Microsoft.Maui.Controls.Shapes.Path)border.Content.GetVisualTreeDescendants()[0]).Stroke = newValue;
		}
	}

	/// <summary>Change the rating item shape border thickness.</summary>
	/// <param name="oldValue">Old rating item shape border thickness.</param>
	/// <param name="newValue">New rating item shape border thickness.</param>
	void IRatingViewShape.OnItemShapeBorderThicknessChanged(double oldValue, double newValue)
	{
		Debug.Assert(Control is not null);
		for (int element = 0; element < Control!.Count; element++)
		{
			Border border = (Border)Control.Children[element];
			Debug.Assert(border.Content is not null);
			((Microsoft.Maui.Controls.Shapes.Path)border.Content.GetVisualTreeDescendants()[0]).StrokeThickness = newValue;
		}
	}

	/// <summary>Change the rating item shape.</summary>
	/// <param name="oldValue">Old rating item shape.</param>
	/// <param name="newValue">New rating item shape.</param>
	void IRatingViewShape.OnItemShapePropertyChanged(RatingViewShape oldValue, RatingViewShape newValue)
	{
		ChangeRatingItemShape(GetShapePathData(newValue));
	}

	/// <summary>Change the rating item shape size.</summary>
	/// <param name="oldValue">Old rating item shape size.</param>
	/// <param name="newValue">New rating item shape size.</param>
	void IRatingViewShape.OnItemShapeSizeChanged(double oldValue, double newValue)
	{
		Debug.Assert(Control is not null);
		for (int element = 0; element < Control.Count; element++)
		{
			Border border = (Border)Control.Children[element];
			Debug.Assert(border.Content is not null);
			Microsoft.Maui.Controls.Shapes.Path rating = (Microsoft.Maui.Controls.Shapes.Path)border.Content.GetVisualTreeDescendants()[0];
			rating.WidthRequest = newValue;
			rating.HeightRequest = newValue;
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

	static Border CreateChild(string shape, Thickness itemPadding, double shapeBorderThickness, double itemShapeSize, Brush shapeBorderColor, Color itemColor)
	{
		Border shapeBorder = new()
		{
			BackgroundColor = itemColor,
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

	static ReadOnlyCollection<VisualElement> GetVisualTreeDescendantsWithBorderAndShape(VisualElement root, bool isShapeFill)
	{
		List<VisualElement> result = [];
		HorizontalStackLayout stackLayout = (HorizontalStackLayout)root.GetVisualTreeDescendants().OfType<VisualElement>().First();
		foreach (IView? child in stackLayout.Children)
		{
			Debug.Assert(child is not null);
			if (isShapeFill)
			{
				result.Add((Shape)((Border)child).Content!);
			}
			else
			{
				result.Add((Border)child);
			}
		}

		return result.AsReadOnly();
	}

	/// <summary>Validator for the <see cref="MaximumRating"/>.</summary>
	/// <remarks>The value must be between 1 and <see cref="RatingViewDefaults.MaximumRatings" />.</remarks>
	/// <param name="bindable">The bindable object.</param>
	/// <param name="value">Value to validate.</param>
	/// <returns>True, if the value is within range; Otherwise false.</returns>
	static bool maximumRatingValidator(BindableObject bindable, object value)
	{
		return (byte)value is >= 1 and <= RatingViewDefaults.MaximumRatings;
	}

	static void OnIsReadOnlyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		RatingView ratingView = (RatingView)bindable;
		ratingView.OnPropertyChanged(nameof(ratingView.IsReadOnly));
		ratingView.HandleIsReadOnlyChanged();
	}

	/// <summary>Maximum rating has changed.</summary>
	/// <remarks>If the current rating is greater than the maximum, then change the current rating to the new value and raise the event as appropriate.</remarks>
	/// <param name="bindable">RatingView object.</param>
	/// <param name="oldValue">Old maximum rating value.</param>
	/// <param name="newValue">New maximum rating value.</param>
	static void OnMaximumRatingChange(BindableObject bindable, object oldValue, object newValue)
	{
		RatingView ratingView = (RatingView)bindable;
		byte newMaximumRatingValue = (byte)newValue;
		if (newMaximumRatingValue < (byte)oldValue)
		{
			Debug.Assert(ratingView is not null);
			HorizontalStackLayout? element = ratingView.Control;
			Debug.Assert(element is not null);
			for (int lastElement = element.Count - 1; lastElement >= newMaximumRatingValue; lastElement--)
			{
				element.RemoveAt(lastElement);
			}

			ratingView.UpdateRatingDraw();
		}

		if (newMaximumRatingValue > (byte)oldValue)
		{
			ratingView.AddChildrenToControl((byte)oldValue - 1, newMaximumRatingValue - 1);
		}

		if (newMaximumRatingValue >= ratingView.Rating)
		{
			return;
		}

		ratingView.Rating = newMaximumRatingValue;
		if (ratingView.IsReadOnly)
		{
			return;
		}

		weakEventManager.HandleEvent(ratingView, new RatingChangedEventArgs(newMaximumRatingValue), nameof(MaximumRating));
	}

	static void OnRatingChanged(BindableObject bindable, object oldValue, object newValue)
	{
		RatingView ratingView = (RatingView)bindable;
		double newValueDouble = (double)newValue;
		ratingView.UpdateRatingDraw();
		if (ratingView.IsReadOnly)
		{
			return;
		}

		ratingView.OnRatingChangedEvent(new RatingChangedEventArgs(newValueDouble));
		if (ratingView.RatingChangedCommand?.CanExecute(newValueDouble) ?? false)
		{
			ratingView.RatingChangedCommand.Execute(newValueDouble);
		}
	}

	static void OnSpacingChanged(BindableObject bindable, object oldValue, object newValue)
	{
		RatingView ratingView = (RatingView)bindable;
		Debug.Assert(ratingView.Control is not null);
		ratingView.Control.Spacing = (double)newValue;
	}

	static void OnUpdateRatingDraw(BindableObject bindable, object oldValue, object newValue)
	{
		((RatingView)bindable).UpdateRatingDraw();
	}

	/// <summary>Partial fill linear gradient brush, for shapes/items that are partial and not filled or empty.</summary>
	/// <param name="filledColor">Offset start partial fill color.</param>
	/// <param name="partialFill">Partial fill color.</param>
	/// <param name="emptyColor">Empty fill color.</param>
	/// <returns>A <see cref="LinearGradientBrush"/> for a partially filled element.</returns>
	static LinearGradientBrush PartialFill(Color filledColor, double partialFill, Color emptyColor)
	{
		return new(
			[
					new GradientStop(filledColor, 0),
				new GradientStop(filledColor, (float)partialFill),
				new GradientStop(emptyColor, (float)partialFill),
				],
			new Point(0, 0), new Point(1, 0));
	}

	/// <summary>Validator for the <see cref="Rating"/>.</summary>
	/// <remarks>The value must be between 0.00 and <see cref="RatingViewDefaults.MaximumRatings" />.</remarks>
	/// <param name="bindable">The bindable object.</param>
	/// <param name="value">Value to validate.</param>
	/// <returns>True, if the value is within range; Otherwise false.</returns>
	static bool ratingValidator(BindableObject bindable, object value)
	{
		return (double)value is >= 0.0 and <= RatingViewDefaults.MaximumRatings;
	}

	/// <summary>Updates each rating item colors based on the rating value (filled, partially filled, empty).</summary>
	/// <remarks>At this time, since .NET MAUI doesn't have direct partial fill, we approximate with a gradient.</remarks>
	/// <param name="ratingItems">A list of rating item visual elements to update.</param>
	/// <param name="rating">Current rating value (e.g., 3.7)</param>
	/// <param name="filledColor">Color of a filled item.</param>
	/// <param name="emptyColor">Color of an empty item.</param>
	/// <param name="backgroundColor">Color of the item.</param>
	static void UpdateRatingItemColors(ReadOnlyCollection<VisualElement> ratingItems, double rating, Color filledColor, Color emptyColor, Color backgroundColor)
	{
		int fullShapes = (int)Math.Floor(rating); // Determine the number of fully filled shapes
		double partialFill = rating - fullShapes; // Determine the fraction for the partially filled shape (if any)
		backgroundColor ??= Colors.Transparent;
		for (int i = 0; i < ratingItems.Count; i++)
		{
			Border border = (Border)ratingItems[i];
			Debug.Assert(border.Content is not null);
			((Shape)border.Content).Fill = emptyColor;
			if (i < fullShapes)
			{
				ratingItems[i].Background = new SolidColorBrush(filledColor);  // Fully filled shape
			}
			else if (i == fullShapes && partialFill > 0)
			{
				ratingItems[i].Background = PartialFill(filledColor, partialFill, backgroundColor); // Partial fill
			}
			else
			{
				ratingItems[i].Background = new SolidColorBrush(backgroundColor);  // Empty fill
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
		int fullShapes = (int)Math.Floor(rating); // Determine the number of fully filled shapes
		double partialFill = rating - fullShapes; // Determine the fraction for the partially filled shape (if any)
		for (int i = 0; i < ratingItems.Count; i++)
		{
			Shape ratingShape = (Shape)ratingItems[i];
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
				ratingShape.Fill = emptyColor;  // Empty fill
			}
		}
	}

	/// <summary>Add the required children to the control.</summary>
	/// <param name="minimum">Minimum position to add a child.</param>
	/// <param name="maximum">Maximum position to add a child</param>
	void AddChildrenToControl(int minimum, int maximum)
	{
		Debug.Assert(Control is not null);
		Control.Spacing = Spacing;
		string shape = GetShapePathData(Shape);
		for (int i = minimum; i < maximum; i++)
		{
			Border child = CreateChild(shape, ItemPadding, ShapeBorderThickness, ItemShapeSize, ShapeBorderColor, this.BackgroundColor);
			if (!IsReadOnly)
			{
				TapGestureRecognizer tapGestureRecognizer = new();
				tapGestureRecognizer.Tapped += OnItemTapped;
				child.GestureRecognizers.Add(tapGestureRecognizer);
			}

			Control.Children.Add(child);
		}

		UpdateRatingDraw();
	}

	void ChangeRatingItemShape(string shape)
	{
		Debug.Assert(Control is not null);
		for (int element = 0; element < Control.Count; element++)
		{
			Border border = (Border)Control.Children[element];
			Debug.Assert(border.Content is not null);
			((Microsoft.Maui.Controls.Shapes.Path)border.Content.GetVisualTreeDescendants()[0]).Data = (Geometry?)new PathGeometryConverter().ConvertFromInvariantString(shape);
		}
	}

	string GetShapePathData(RatingViewShape shape)
	{
		return shape switch
		{
			RatingViewShape.Circle => Core.Primitives.RatingViewShape.Circle.PathData,
			RatingViewShape.Custom => CustomShape ?? Core.Primitives.RatingViewShape.Star.PathData,
			RatingViewShape.Dislike => Core.Primitives.RatingViewShape.Dislike.PathData,
			RatingViewShape.Heart => Core.Primitives.RatingViewShape.Heart.PathData,
			RatingViewShape.Like => Core.Primitives.RatingViewShape.Like.PathData,
			_ => Core.Primitives.RatingViewShape.Star.PathData,
		};
	}

	/// <summary>Ensure the VisualElement is read only.</summary>
	/// <remarks>Remove any added gesture recognisers if not enabled; Otherwise, where not already present.</remarks>
	void HandleIsReadOnlyChanged()
	{
		for (int i = 0; i < Control?.Children.Count; i++)
		{
			Border child = (Border)Control.Children[i];
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
		Debug.Assert(Control is not null);
		Debug.Assert(sender is not null);
		int itemIndex = Control.Children.IndexOf((Border)sender);
		Rating = MaximumRating > 1 ? itemIndex + 1 : WhenMaximumOneToggleRating();
	}

	void OnRatingChangedEvent(RatingChangedEventArgs e)
	{
		weakEventManager.HandleEvent(this, e, nameof(RatingChanged));
	}

	/// <summary>Update the drawing of the controls ratings.</summary>
	void UpdateRatingDraw()
	{
		bool isShapeFill = RatingFill is RatingFillElement.Shape;
		ReadOnlyCollection<VisualElement> visualElements = GetVisualTreeDescendantsWithBorderAndShape((VisualElement)Control!.GetVisualTreeDescendants()[0], isShapeFill);
		if (isShapeFill)
		{
			UpdateRatingShapeColors(visualElements, Rating, FilledColor, EmptyColor);
		}
		else
		{
			UpdateRatingItemColors(visualElements, Rating, FilledColor, EmptyColor, BackgroundColor);
		}
	}

	int WhenMaximumOneToggleRating()
	{
		return Rating.Equals(0.0) ? 1 : 0;
	}
}

/// <summary>Rating view fill element.</summary>
public enum RatingFillElement
{
	/// <summary>Fill the rating shape.</summary>
	Shape,

	/// <summary>Fill the rating item.</summary>
	Item
}