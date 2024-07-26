// Ignore Spelling: Bindable
using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>Converts the incoming value to a <see cref="bool"/> indicating whether or not the value is within a range.</summary>
public sealed class IsInRangeConverter : IsInRangeConverter<IComparable, object>;

/// <summary>Converts the incoming value to a <see cref="bool"/> indicating whether or not the value is within a range.</summary>
public abstract class IsInRangeConverter<TValue, TReturnObject> : BaseConverterOneWay<TValue, object> where TValue : IComparable
{
	/// <inheritdoc/>
	public override object DefaultConvertReturnValue { get; set; } = new();

	/// <summary>If supplied this value will be returned when the converter receives an input value that is <b>outside</b> of the <see cref="MinValue" /> and <see cref="MaxValue" />s.</summary>
	public TReturnObject? FalseObject { get; set; }

	/// <summary>The upper bounds of the range to compare against when determining whether the input value to the convert is within range.</summary>
	public TValue? MaxValue { get; set; }

	/// <summary>Minimum value.</summary>
	public TValue? MinValue { get; set; }

	/// <summary>If supplied this value will be returned when the converter receives an input value that is <b>inside</b> (inclusive) of the <see cref="MinValue" /> and <see cref="MaxValue" />s.</summary>
	public TReturnObject? TrueObject { get; set; }

	/// <summary>Converts an object that implemented IComparable to a <see cref="bool"/> based on the object being within a <see cref="MinValue"/> and <see cref="MaxValue"/> range.</summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="culture">(Not Used)</param>
	/// <returns>The object assigned to <see cref="TrueObject"/> if value is between <see cref="MinValue"/> and <see cref="MaxValue"/> returns true, otherwise the value assigned to <see cref="FalseObject"/>.</returns>
	public override object ConvertFrom(TValue value, CultureInfo? culture)
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

		Type valueType = value.GetType();
		if (MinValue is not null && !CanChangeType(MinValue, valueType))
		{
			throw new InvalidOperationException($"{nameof(MinValue)} is expected to be of a matching type to {nameof(value)}, but is {MinValue.GetType()}");
		}

		if (MaxValue is not null && !CanChangeType(MaxValue, valueType))
		{
			throw new InvalidOperationException($"{nameof(MaxValue)} is expected to be of a matching type to {nameof(value)}, but is {MaxValue.GetType()}");
		}

		bool shouldReturnObjectResult = TrueObject is not null && FalseObject is not null;
		if (MaxValue is null)
		{
			return EvaluateCondition(value.CompareTo(MinValue) >= 0, shouldReturnObjectResult);
		}

		return MinValue is null
			? EvaluateCondition(value.CompareTo(MaxValue) <= 0, shouldReturnObjectResult)
			: EvaluateCondition(value.CompareTo(MinValue) >= 0 && value.CompareTo(MaxValue) <= 0, shouldReturnObjectResult);
	}

	/// <summary>Can the object type be changed to the target type.</summary>
	/// <param name="value">Source object</param>
	/// <param name="targetType">Target type.</param>
	/// <returns>true if the object type can be converted to the target type; otherwise false.</returns>
	static bool CanChangeType(object value, Type targetType)
	{
		try
		{
			_ = Convert.ChangeType(value, targetType);
			return true; // Conversion succeeded
		}
		catch (Exception)
		{
			return false; // Conversion failed
		}
	}

	/// <summary>Evaluates a condition based on the given comparison result and returns an object.</summary>
	/// <param name="comparisonResult">The result of the comparison.</param>
	/// <param name="shouldReturnObject">Indicates whether an object should be returned.</param>
	/// <returns>The result of the evaluation.</returns>
	object EvaluateCondition(bool comparisonResult, bool shouldReturnObject)
	{
		return (comparisonResult, shouldReturnObject) switch
		{
			(true, true) => TrueObject ?? throw new InvalidOperationException($"{nameof(TrueObject)} cannot be null"),
			(false, true) => FalseObject ?? throw new InvalidOperationException($"{nameof(FalseObject)} cannot be null"),
			(true, false) => true,
			(false, false) => false
		};
	}
}