// Ignore Spelling: bindable

using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Views;

static class RatingViewItemElement
{
	/// <summary>Bindable property for attached property <c>CustomShape</c>.</summary>
	public static readonly BindableProperty CustomShapeProperty = BindableProperty.Create(nameof(IRatingViewShape.CustomShape), typeof(string), typeof(IRatingViewShape), defaultValue: null, propertyChanged: OnCustomShapePropertyChanged);

	/// <summary>Bindable property for attached property <c>PaddingBottom</c>.</summary>
	public static readonly BindableProperty ShapePaddingBottomProperty = BindableProperty.Create("PaddingBottom", typeof(double), typeof(IRatingViewShape), default(double), propertyChanged: OnShapePaddingBottomChanged);

	/// <summary>Bindable property for attached property <c>PaddingLeft</c>.</summary>
	public static readonly BindableProperty ShapePaddingLeftProperty = BindableProperty.Create("PaddingLeft", typeof(double), typeof(IRatingViewShape), default(double), propertyChanged: OnShapePaddingLeftChanged);

	/// <summary>Bindable property for <see cref="IRatingViewShape.ShapePadding"/>.</summary>
	public static readonly BindableProperty ShapePaddingProperty = BindableProperty.Create(nameof(IRatingViewShape.ShapePadding), typeof(Thickness), typeof(IRatingViewShape), default(Thickness), propertyChanged: OnShapePaddingPropertyChanged, defaultValueCreator: ShapePaddingDefaultValueCreator);

	/// <summary>Bindable property for attached property <c>PaddingRight</c>.</summary>
	public static readonly BindableProperty ShapePaddingRightProperty = BindableProperty.Create("PaddingRight", typeof(double), typeof(IRatingViewShape), default(double), propertyChanged: OnShapePaddingRightChanged);

	/// <summary>Bindable property for attached property <c>PaddingTop</c>.</summary>
	public static readonly BindableProperty ShapePaddingTopProperty = BindableProperty.Create("PaddingTop", typeof(double), typeof(IRatingViewShape), default(double), propertyChanged: OnShapePaddingTopChanged);

	/// <summary>Bindable property for attached property <c>Shape</c>.</summary>
	public static readonly BindableProperty ShapeProperty = BindableProperty.Create(nameof(IRatingViewShape.Shape), typeof(RatingViewShape), typeof(IRatingViewShape), defaultValue: RatingViewShape.Star, propertyChanged: OnShapePropertyChanged, defaultValueCreator: ShapeDefaultValueCreator);

	static void OnCustomShapePropertyChanged(BindableObject bindable, object? oldValue, object? newValue)
	{
		((IRatingViewShape)bindable).OnCustomShapePropertyChanged((string?)oldValue, (string?)newValue);
	}

	static void OnShapePaddingBottomChanged(BindableObject bindable, object oldValue, object newValue)
	{
		Thickness padding = (Thickness)bindable.GetValue(ShapePaddingProperty);
		padding.Bottom = (double)newValue;
		bindable.SetValue(ShapePaddingProperty, padding);
	}

	static void OnShapePaddingLeftChanged(BindableObject bindable, object oldValue, object newValue)
	{
		Thickness padding = (Thickness)bindable.GetValue(ShapePaddingProperty);
		padding.Left = (double)newValue;
		bindable.SetValue(ShapePaddingProperty, padding);
	}

	static void OnShapePaddingPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IRatingViewShape)bindable).OnShapePaddingPropertyChanged((Thickness)oldValue, (Thickness)newValue);
	}

	static void OnShapePaddingRightChanged(BindableObject bindable, object oldValue, object newValue)
	{
		Thickness padding = (Thickness)bindable.GetValue(ShapePaddingProperty);
		padding.Right = (double)newValue;
		bindable.SetValue(ShapePaddingProperty, padding);
	}

	static void OnShapePaddingTopChanged(BindableObject bindable, object oldValue, object newValue)
	{
		Thickness padding = (Thickness)bindable.GetValue(ShapePaddingProperty);
		padding.Top = (double)newValue;
		bindable.SetValue(ShapePaddingProperty, padding);
	}

	static void OnShapePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IRatingViewShape)bindable).OnShapePropertyChanged((RatingViewShape)oldValue, (RatingViewShape)newValue);
	}

	static object ShapeDefaultValueCreator(BindableObject bindable)
	{
		return ((IRatingViewShape)bindable).ShapeDefaultValueCreator();
	}

	static object ShapePaddingDefaultValueCreator(BindableObject bindable)
	{
		return ((IRatingViewShape)bindable).ShapePaddingDefaultValueCreator();
	}
}