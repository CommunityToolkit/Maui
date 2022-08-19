using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>Converts the incoming value to a <see cref="bool"/> indicating whether or not the value is within a range.</summary>
public sealed class IsInRangeConverter : IsInRangeConverter<object>
{
}

/// <summary>Converts the incoming value to a <see cref="bool"/> indicating whether or not the value is within a range.</summary>
public abstract class IsInRangeConverter<TObject> : BaseConverterOneWay<IComparable, object>
{
	/// <summary>Minimum value.</summary>
	public IComparable? Min { get; set; }

	/// <summary>Maximum value.</summary>
	public IComparable? Max { get; set; }

	/// <summary>The object that corresponds to True value.</summary>
	public TObject? TrueObject { get; set; }

	/// <summary>The object that corresponds to False value.</summary>
	public TObject? FalseObject { get; set; }

	/// <summary>Converts an object that implemented IComparable to a <see cref="bool"/> based on the object being within a <see cref="Min"/> and <see cref="Max"/> range.</summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="culture">(Not Used)</param>
	/// <returns>The object assigned to <see cref="TrueObject"/> if value is between <see cref="Min"/> and <see cref="Max"/> then <see cref="TrueObject"/> returns true, otherwise the value assigned to <see cref="FalseObject"/>.</returns>
	public override object ConvertFrom(IComparable value, CultureInfo? culture)
	{
		ArgumentNullException.ThrowIfNull(value);
		ArgumentNullException.ThrowIfNull((Min is null && Max is null) ? null : true, nameof(Min));
		if (!(TrueObject is null ^ FalseObject is not null))
		{
			throw new InvalidOperationException($"{nameof(TrueObject)} and {nameof(FalseObject)} should either be both defined or both omitted.");
		}

		if (Min is not null && Min.GetType() != value.GetType())
		{
			throw new ArgumentOutOfRangeException(nameof(value), $" is expected to be of type {nameof(Min)}");
		}

		if (Max is not null && Max.GetType() != value.GetType())
		{
			throw new ArgumentOutOfRangeException(nameof(value), $" is expected to be of type {nameof(Max)}");
		}

		bool shouldReturnObjectResult = TrueObject is not null && FalseObject is not null;

		if (Max is null)
		{
			return EvaluateCondition(value.CompareTo(Min) >= 0, shouldReturnObjectResult);
		}

		if (Min is null)
		{
			return EvaluateCondition(value.CompareTo(Max) <= 0, shouldReturnObjectResult);
		}

		return EvaluateCondition(value.CompareTo(Min) >= 0 && value.CompareTo(Max) <= 0, shouldReturnObjectResult);
	}

	object EvaluateCondition(bool comparisonResult, bool shouldReturnObject) => (comparisonResult, shouldReturnObject) switch
	{
		(true, true) => TrueObject ?? throw new InvalidOperationException($"{nameof(TrueObject)} cannot be null"),
		(false, true) => FalseObject ?? throw new InvalidOperationException($"{nameof(FalseObject)} cannot be null"),
		(true, _) => true,
		_ => false
	};
}