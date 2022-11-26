using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>Converts the incoming value to a <see cref="bool"/> indicating whether or not the value is within a range.</summary>
public sealed class IsInRangeConverter : IsInRangeConverter<object>
{
}

/// <summary>Converts the incoming value to a <see cref="bool"/> indicating whether or not the value is within a range.</summary>
public abstract class IsInRangeConverter<TObject> : BaseConverterOneWay<IComparable, object>
{
	/// <inheritdoc/>
	public override object DefaultConvertReturnValue { get; set; } = new();

	/// <summary>Minimum value.</summary>
	public IComparable? MinValue { get; set; }

	/// <summary>Maximum value.</summary>
	public IComparable? MaxValue { get; set; }

	/// <summary>The object that corresponds to True value.</summary>
	public TObject? TrueObject { get; set; }

	/// <summary>The object that corresponds to False value.</summary>
	public TObject? FalseObject { get; set; }

	/// <summary>Converts an object that implemented IComparable to a <see cref="bool"/> based on the object being within a <see cref="MinValue"/> and <see cref="MaxValue"/> range.</summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="culture">(Not Used)</param>
	/// <returns>The object assigned to <see cref="TrueObject"/> if value is between <see cref="MinValue"/> and <see cref="MaxValue"/> then <see cref="TrueObject"/> returns true, otherwise the value assigned to <see cref="FalseObject"/>.</returns>
	public override object ConvertFrom(IComparable value, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(value);
		ArgumentNullException.ThrowIfNull((MinValue is null && MaxValue is null) ? null : true, nameof(MinValue));
		if (!(TrueObject is null ^ FalseObject is not null))
		{
			throw new InvalidOperationException($"{nameof(TrueObject)} and {nameof(FalseObject)} should either be both defined or both omitted.");
		}

		if (MinValue is not null && MinValue.GetType() != value.GetType())
		{
			throw new ArgumentOutOfRangeException(nameof(value), $" is expected to be of type {nameof(MinValue)}");
		}

		if (MaxValue is not null && MaxValue.GetType() != value.GetType())
		{
			throw new ArgumentOutOfRangeException(nameof(value), $" is expected to be of type {nameof(MaxValue)}");
		}

		bool shouldReturnObjectResult = TrueObject is not null && FalseObject is not null;

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
		(true, _) => true,
		_ => false
	};
}