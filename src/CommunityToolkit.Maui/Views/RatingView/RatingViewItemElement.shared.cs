// Ignore Spelling: bindable, color
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Views;

static class RatingViewItemElement
{
	/// <summary>Bindable property for attached property <c>CustomShape</c>.</summary>
	public static readonly BindableProperty CustomShapeProperty = BindableProperty.Create(nameof(IRatingViewShape.CustomShape), typeof(string), typeof(IRatingViewShape), defaultValue: null, propertyChanged: OnCustomShapePropertyChanged);

	/// <summary>Bindable property for <see cref="IRatingViewShape.ItemPadding"/>.</summary>
	public static readonly BindableProperty ItemPaddingProperty = BindableProperty.Create(nameof(IRatingViewShape.ItemPadding), typeof(Thickness), typeof(IRatingViewShape), default(Thickness), propertyChanged: OnItemPaddingPropertyChanged, defaultValueCreator: ItemPaddingDefaultValueCreator);

	/// <summary>Bindable property for attached property <c>Shape</c>.</summary>
	public static readonly BindableProperty ShapeProperty = BindableProperty.Create(nameof(IRatingViewShape.Shape), typeof(RatingViewShape), typeof(IRatingViewShape), defaultValue: RatingViewShape.Star, propertyChanged: OnItemShapePropertyChanged, defaultValueCreator: ShapeDefaultValueCreator);

	/// <summary>Bindable property for attached property <c>ShapeBorderColor</c>.</summary>
	public static readonly BindableProperty ShapeBorderColorProperty = BindableProperty.Create(nameof(IRatingViewShape.ShapeBorderColor), typeof(Color), typeof(IRatingViewShape), defaultValue: RatingViewDefaults.ShapeBorderColor, propertyChanged: OnItemShapeBorderColorChanged, defaultValueCreator: ItemShapeBorderColorDefaultValueCreator);

	/// <summary>Bindable property for attached property <c>ShapeBorderThickness</c>.</summary>
	public static readonly BindableProperty ShapeBorderThicknessProperty = BindableProperty.Create(nameof(IRatingViewShape.ShapeBorderThickness), typeof(double), typeof(IRatingViewShape), defaultValue: RatingViewDefaults.ShapeBorderThickness, propertyChanged: OnItemShapeBorderThicknessChanged, defaultValueCreator: ItemShapeBorderThicknessDefaultValueCreator);

	/// <summary>Bindable property for attached property <c>Size</c>.</summary>
	public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(IRatingViewShape.ItemShapeSize), typeof(double), typeof(IRatingViewShape), defaultValue: RatingViewDefaults.ItemShapeSize, propertyChanged: OnItemShapeSizeChanged, defaultValueCreator: ItemShapeSizeDefaultValueCreator);

	/// <summary>Bindable property for attached property <c>EmptyColor</c>.</summary>
	public static readonly BindableProperty EmptyColorProperty = BindableProperty.Create(nameof(IRatingViewShape.EmptyColor), typeof(Color), typeof(IRatingViewShape), defaultValue: RatingViewDefaults.EmptyColor, propertyChanged: OnEmptyColorPropertyChanged, defaultValueCreator: EmptyColorDefaultValueCreator);

	/// <summary>Bindable property for attached property <c>FilledColor</c>.</summary>
	public static readonly BindableProperty FilledColorProperty = BindableProperty.Create(nameof(IRatingViewShape.FilledColor), typeof(Color), typeof(IRatingViewShape), defaultValue: RatingViewDefaults.FilledColor, propertyChanged: OnFilledColorPropertyChanged, defaultValueCreator: FilledColorDefaultValueCreator);

	static void OnEmptyColorPropertyChanged(BindableObject bindable, object oldValue, object newValue) => ((IRatingViewShape)bindable).OnEmptyColorPropertyChanged((Color)oldValue, (Color)newValue);

	static void OnFilledColorPropertyChanged(BindableObject bindable, object oldValue, object newValue) => ((IRatingViewShape)bindable).OnFilledColorPropertyChanged((Color)oldValue, (Color)newValue);

	static void OnItemShapeSizeChanged(BindableObject bindable, object oldValue, object newValue) => ((IRatingViewShape)bindable).OnItemShapeSizeChanged((double)oldValue, (double)newValue);

	static void OnCustomShapePropertyChanged(BindableObject bindable, object? oldValue, object? newValue) => ((IRatingViewShape)bindable).OnCustomShapePropertyChanged((string?)oldValue, (string?)newValue);

	static void OnItemPaddingPropertyChanged(BindableObject bindable, object oldValue, object newValue) => ((IRatingViewShape)bindable).OnItemPaddingPropertyChanged((Thickness)oldValue, (Thickness)newValue);

	static void OnItemShapePropertyChanged(BindableObject bindable, object oldValue, object newValue) => ((IRatingViewShape)bindable).OnItemShapePropertyChanged((RatingViewShape)oldValue, (RatingViewShape)newValue);

	static object ItemShapeSizeDefaultValueCreator(BindableObject bindable) => ((IRatingViewShape)bindable).ItemShapeSizeDefaultValueCreator();

	static object EmptyColorDefaultValueCreator(BindableObject bindable) => ((IRatingViewShape)bindable).EmptyColorDefaultValueCreator();

	static object FilledColorDefaultValueCreator(BindableObject bindable) => ((IRatingViewShape)bindable).FilledColorDefaultValueCreator();

	static object ShapeDefaultValueCreator(BindableObject bindable) => ((IRatingViewShape)bindable).ShapeDefaultValueCreator();

	static object ItemPaddingDefaultValueCreator(BindableObject bindable) => ((IRatingViewShape)bindable).ItemPaddingDefaultValueCreator();

	static object ItemShapeBorderColorDefaultValueCreator(BindableObject bindable) => ((IRatingViewShape)bindable).ItemShapeBorderColorDefaultValueCreator();

	static void OnItemShapeBorderColorChanged(BindableObject bindable, object oldValue, object newValue) => ((IRatingViewShape)bindable).OnItemShapeBorderColorChanged((Color)oldValue, (Color)newValue);

	static object ItemShapeBorderThicknessDefaultValueCreator(BindableObject bindable) => ((IRatingViewShape)bindable).ItemShapeBorderThicknessDefaultValueCreator();

	static void OnItemShapeBorderThicknessChanged(BindableObject bindable, object oldValue, object newValue) => ((IRatingViewShape)bindable).OnItemShapeBorderThicknessChanged((double)oldValue, (double)newValue);
}