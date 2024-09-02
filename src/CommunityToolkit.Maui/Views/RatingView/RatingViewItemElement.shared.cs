// Ignore Spelling: bindable, color
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Views;

static class RatingViewItemElement
{
	/// <summary>Bindable property for attached property <c>CustomShape</c>.</summary>
	public static readonly BindableProperty CustomShapeProperty = BindableProperty.Create(nameof(IRatingViewShape.CustomShape), typeof(string), typeof(IRatingViewShape), defaultValue: null, propertyChanged: OnCustomShapePropertyChanged);

	/// <summary>Bindable property for attached property <c>PaddingBottom</c>.</summary>
	public static readonly BindableProperty ItemPaddingBottomProperty = BindableProperty.Create("PaddingBottom", typeof(double), typeof(IRatingViewShape), default(double), propertyChanged: OnItemPaddingBottomChanged);

	/// <summary>Bindable property for attached property <c>PaddingLeft</c>.</summary>
	public static readonly BindableProperty ItemPaddingLeftProperty = BindableProperty.Create("PaddingLeft", typeof(double), typeof(IRatingViewShape), default(double), propertyChanged: OnItemPaddingLeftChanged);

	/// <summary>Bindable property for <see cref="IRatingViewShape.ItemPadding"/>.</summary>
	public static readonly BindableProperty ItemPaddingProperty = BindableProperty.Create(nameof(IRatingViewShape.ItemPadding), typeof(Thickness), typeof(IRatingViewShape), default(Thickness), propertyChanged: OnItemPaddingPropertyChanged, defaultValueCreator: ItemPaddingDefaultValueCreator);

	/// <summary>Bindable property for attached property <c>PaddingRight</c>.</summary>
	public static readonly BindableProperty ItemPaddingRightProperty = BindableProperty.Create("PaddingRight", typeof(double), typeof(IRatingViewShape), default(double), propertyChanged: OnItemPaddingRightChanged);

	/// <summary>Bindable property for attached property <c>PaddingTop</c>.</summary>
	public static readonly BindableProperty ItemPaddingTopProperty = BindableProperty.Create("PaddingTop", typeof(double), typeof(IRatingViewShape), default(double), propertyChanged: OnItemPaddingTopChanged);

	/// <summary>Bindable property for attached property <c>Shape</c>.</summary>
	public static readonly BindableProperty ShapeProperty = BindableProperty.Create(nameof(IRatingViewShape.Shape), typeof(RatingViewShape), typeof(IRatingViewShape), defaultValue: RatingViewShape.Star, propertyChanged: OnItemShapePropertyChanged, defaultValueCreator: ShapeDefaultValueCreator);

	/// <summary>Bindable property for attached property <c>ShapeBorderColor</c>.</summary>
	public static readonly BindableProperty ShapeBorderColorProperty = BindableProperty.Create(nameof(IRatingViewShape.ShapeBorderColor), typeof(Color), typeof(IRatingViewShape), defaultValue: RatingViewDefaults.ShapeBorderColor, propertyChanged: OnItemShapeBorderColorChanged, defaultValueCreator: ItemShapeBorderColorDefaultValueCreator);

	/// <summary>Bindable property for attached property <c>ShapeBorderThickness</c>.</summary>
	public static readonly BindableProperty ShapeBorderThicknessProperty = BindableProperty.Create(nameof(IRatingViewShape.ShapeBorderThickness), typeof(double), typeof(IRatingViewShape), defaultValue: RatingViewDefaults.ShapeBorderThickness, propertyChanged: OnItemShapeBorderThicknessChanged, defaultValueCreator: ItemShapeBorderThicknessDefaultValueCreator);

	/// <summary>Bindable property for attached property <c>Size</c>.</summary>
	public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(IRatingViewShape.ItemShapeSize), typeof(double), typeof(IRatingViewShape), defaultValue: RatingViewDefaults.ItemShapeSize, propertyChanged: OnItemShapeSizeChanged, defaultValueCreator: ItemShapeSizeDefaultValueCreator);

	/// <summary>Bindable property for attached property <c>EmptyBackgroundColor</c>.</summary>
	public static readonly BindableProperty EmptyBackgroundColorProperty = BindableProperty.Create(nameof(IRatingViewShape.EmptyBackgroundColor), typeof(Color), typeof(IRatingViewShape), defaultValue: RatingViewDefaults.EmptyBackgroundColor, propertyChanged: OnEmptyBackgroundColorPropertyChanged, defaultValueCreator: EmptyBackgroundColorDefaultValueCreator);

	/// <summary>Bindable property for attached property <c>FilledBackgroundColor</c>.</summary>
	public static readonly BindableProperty FilledBackgroundColorProperty = BindableProperty.Create(nameof(IRatingViewShape.FilledBackgroundColor), typeof(Color), typeof(IRatingViewShape), defaultValue: RatingViewDefaults.FilledBackgroundColor, propertyChanged: OnFilledBackgroundColorPropertyChanged, defaultValueCreator: FilledBackgroundColorDefaultValueCreator);

	static void OnEmptyBackgroundColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IRatingViewShape)bindable).OnEmptyBackgroundColorPropertyChanged((Color)oldValue, (Color)newValue);
	}

	static void OnFilledBackgroundColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IRatingViewShape)bindable).OnFilledBackgroundColorPropertyChanged((Color)oldValue, (Color)newValue);
	}

	static void OnItemShapeSizeChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IRatingViewShape)bindable).OnItemShapeSizeChanged((double)oldValue, (double)newValue);
	}

	static void OnCustomShapePropertyChanged(BindableObject bindable, object? oldValue, object? newValue)
	{
		((IRatingViewShape)bindable).OnCustomShapePropertyChanged((string?)oldValue, (string?)newValue);
	}

	static void OnItemPaddingBottomChanged(BindableObject bindable, object oldValue, object newValue)
	{
		Thickness padding = (Thickness)bindable.GetValue(ItemPaddingProperty);
		padding.Bottom = (double)newValue;
		bindable.SetValue(ItemPaddingProperty, padding);
	}

	static void OnItemPaddingLeftChanged(BindableObject bindable, object oldValue, object newValue)
	{
		Thickness padding = (Thickness)bindable.GetValue(ItemPaddingProperty);
		padding.Left = (double)newValue;
		bindable.SetValue(ItemPaddingProperty, padding);
	}

	static void OnItemPaddingPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IRatingViewShape)bindable).OnItemPaddingPropertyChanged((Thickness)oldValue, (Thickness)newValue);
	}

	static void OnItemPaddingRightChanged(BindableObject bindable, object oldValue, object newValue)
	{
		Thickness padding = (Thickness)bindable.GetValue(ItemPaddingProperty);
		padding.Right = (double)newValue;
		bindable.SetValue(ItemPaddingProperty, padding);
	}

	static void OnItemPaddingTopChanged(BindableObject bindable, object oldValue, object newValue)
	{
		Thickness padding = (Thickness)bindable.GetValue(ItemPaddingProperty);
		padding.Top = (double)newValue;
		bindable.SetValue(ItemPaddingProperty, padding);
	}

	static void OnItemShapePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IRatingViewShape)bindable).OnItemShapePropertyChanged((RatingViewShape)oldValue, (RatingViewShape)newValue);
	}

	static object ItemShapeSizeDefaultValueCreator(BindableObject bindable)
	{
		return ((IRatingViewShape)bindable).ItemShapeSizeDefaultValueCreator();
	}

	static object EmptyBackgroundColorDefaultValueCreator(BindableObject bindable)
	{
		return ((IRatingViewShape)bindable).EmptyBackgroundColorDefaultValueCreator();
	}

	static object FilledBackgroundColorDefaultValueCreator(BindableObject bindable)
	{
		return ((IRatingViewShape)bindable).FilledBackgroundColorDefaultValueCreator();
	}

	static object ShapeDefaultValueCreator(BindableObject bindable)
	{
		return ((IRatingViewShape)bindable).ShapeDefaultValueCreator();
	}

	static object ItemPaddingDefaultValueCreator(BindableObject bindable)
	{
		return ((IRatingViewShape)bindable).ItemPaddingDefaultValueCreator();
	}

	static object ItemShapeBorderColorDefaultValueCreator(BindableObject bindable)
	{
		return ((IRatingViewShape)bindable).ItemShapeBorderColorDefaultValueCreator();
	}

	static void OnItemShapeBorderColorChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IRatingViewShape)bindable).OnItemShapeBorderColorChanged((Color)oldValue, (Color)newValue);
	}

	static object ItemShapeBorderThicknessDefaultValueCreator(BindableObject bindable)
	{
		return ((IRatingViewShape)bindable).ItemShapeBorderThicknessDefaultValueCreator();
	}

	static void OnItemShapeBorderThicknessChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IRatingViewShape)bindable).OnItemShapeBorderThicknessChanged((double)oldValue, (double)newValue);
	}
}