using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>Converts the incoming value to a <see cref="bool"/> indicating whether or not the value is within a range.</summary>
public sealed class IsInRangeConverter : IsInRangeConverter<object>
{
}

/// <summary>Converts the incoming value to a <see cref="bool"/> indicating whether or not the value is within a range.</summary>
public abstract class IsInRangeConverter<TObject> : BaseConverterOneWay<IComparable, object>
{
	/// <summary>
	/// Bindable property for <see cref="MinValue"/>
	/// </summary>
	public static readonly BindableProperty MinValueProperty = BindableProperty.Create(nameof(MinValue), typeof(IComparable), typeof(IsInRangeConverter<TObject>));

	/// <summary>
	/// Bindable property for <see cref="MaxValue"/>
	/// </summary>
	public static readonly BindableProperty MaxValueProperty = BindableProperty.Create(nameof(MaxValue), typeof(IComparable), typeof(IsInRangeConverter<TObject>));

	/// <summary>
	/// Bindable property for <see cref="TrueObject"/>
	/// </summary>
	public static readonly BindableProperty TrueObjectProperty = BindableProperty.Create(nameof(TrueObject), typeof(TObject?), typeof(IsInRangeConverter<TObject>));

	/// <summary>
	/// Bindable property for <see cref="FalseObject"/>
	/// </summary>
	public static readonly BindableProperty FalseObjectProperty = BindableProperty.Create(nameof(FalseObject), typeof(TObject?), typeof(IsInRangeConverter<TObject>));

	/// <inheritdoc/>
	public override object DefaultConvertReturnValue { get; set; } = new();

	/// <summary>Minimum value.</summary>
	public IComparable? MinValue
	{
		get => (IComparable?)GetValue(MinValueProperty);
		set => SetValue(MinValueProperty, value);
	}

	/// <summary>Maximum value.</summary>
	public IComparable? MaxValue
	{
		get => (IComparable?)GetValue(MaxValueProperty);
		set => SetValue(MaxValueProperty, value);
	}

	/// <summary>The object that corresponds to True value.</summary>
	public TObject? TrueObject
	{
		get => (TObject?)GetValue(TrueObjectProperty);
		set => SetValue(TrueObjectProperty, value);
	}

	/// <summary>The object that corresponds to False value.</summary>
	public TObject? FalseObject
	{
		get => (TObject?)GetValue(FalseObjectProperty);
		set => SetValue(FalseObjectProperty, value);
	}

	/// <summary>Converts an object that implemented IComparable to a <see cref="bool"/> based on the object being within a <see cref="MinValue"/> and <see cref="MaxValue"/> range.</summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="culture">(Not Used)</param>
	/// <returns>The object assigned to <see cref="TrueObject"/> if value is between <see cref="MinValue"/> and <see cref="MaxValue"/> then <see cref="TrueObject"/> returns true, otherwise the value assigned to <see cref="FalseObject"/>.</returns>
	public override object ConvertFrom(IComparable value, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(value);

		if (MinValue is null && MaxValue is null)
		{
			throw new ArgumentException($"A value is required for {nameof(MinValue)}, or {nameof(MaxValue)}, or both");
		}

		if (!(TrueObject is null ^ FalseObject is not null))
		{
			throw new InvalidOperationException($"{nameof(TrueObject)} and {nameof(FalseObject)} should either be both defined or both omitted.");
		}

		var valueType = value.GetType();
		if (MinValue is not null && MinValue.GetType() != valueType)
		{
			throw new ArgumentException($"{nameof(value)} is expected to be of type {nameof(MinValue)}, but is {valueType}", nameof(value));
		}

		if (MaxValue is not null && MaxValue.GetType() != valueType)
		{
			throw new ArgumentException($"{nameof(value)} is expected to be of type {nameof(MaxValue)}, but is {valueType}", nameof(value));
		}

		var shouldReturnObjectResult = TrueObject is not null && FalseObject is not null;

		if (MaxValue is null)
		{
			return EvaluateCondition(value.CompareTo(MinValue) >= 0, shouldReturnObjectResult);
		}

		if (MinValue is null)
		{
			return EvaluateCondition(value.CompareTo(MaxValue) <= 0, shouldReturnObjectResult);
		}

		return EvaluateCondition(value.CompareTo(MinValue) >= 0 && value.CompareTo(MaxValue) <= 0, shouldReturnObjectResult);
	}

	object EvaluateCondition(bool comparisonResult, bool shouldReturnObject) => (comparisonResult, shouldReturnObject) switch
	{
		(true, true) => TrueObject ?? throw new InvalidOperationException($"{nameof(TrueObject)} cannot be null"),
		(false, true) => FalseObject ?? throw new InvalidOperationException($"{nameof(FalseObject)} cannot be null"),
		(true, false) => true,
		(false, false) => false
	};
}