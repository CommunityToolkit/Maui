using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts an object that implements IComparable to an object or a boolean based on a comparison.
/// </summary>
public sealed class CompareConverter : CompareConverter<object>
{
}

/// <summary>
/// Converts an object that implements IComparable to an object or a boolean based on a comparison.
/// </summary>
public abstract class CompareConverter<TObject> : BaseConverterOneWay
{
	/// <summary>
	/// Math operator type
	/// </summary>
	[Flags]
	public enum OperatorType
	{
		/// <summary>
		/// Not Equal Operator
		/// </summary>
		NotEqual = 0,

		/// <summary>
		/// Smaller Operator
		/// </summary>
		Smaller = 1 << 0,

		/// <summary>
		/// Smaller or Equal Operator
		/// </summary>
		SmallerOrEqual = 1 << 1,

		/// <summary>
		/// Equal Operator
		/// </summary>
		Equal = 1 << 2,

		/// <summary>
		/// Greater Operator
		/// </summary>
		Greater = 1 << 3,

		/// <summary>
		/// Greater or Equal Operator
		/// </summary>
		GreaterOrEqual = 1 << 4,
	}

	enum Modes
	{
		Boolean,
		Object
	}

	Modes mode;

	/// <summary>
	/// The comparing value.
	/// </summary>
	public IComparable? ComparingValue { get; set; }

	/// <summary>
	/// The comparison operator.
	/// </summary>
	public OperatorType ComparisonOperator { get; set; }

	/// <summary>
	/// The object that corresponds to True value.
	/// </summary>
	public TObject? TrueObject { get; set; }

	/// <summary>
	/// The object that corresponds to False value.
	/// </summary>
	public TObject? FalseObject { get; set; }

	/// <summary>
	/// Converts an object that implements IComparable to a specified object or a boolean based on a comparaison result.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
	/// <param name="culture">The culture to use in the converter.  This is not implemented.</param>
	/// <returns>The object assigned to <see cref="TrueObject"/> if (value <see cref="ComparisonOperator"/> <see cref="ComparingValue"/>) equals True and <see cref="TrueObject"/> is not null, if <see cref="TrueObject"/> is null it returns true, otherwise the value assigned to <see cref="FalseObject"/>, if no value is assigned then it returns false.</returns>
	[return: NotNull]
	[MemberNotNull(nameof(ComparingValue))]
	public override object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
	{
		ArgumentNullException.ThrowIfNull(ComparingValue);
		ArgumentNullException.ThrowIfNull(ComparisonOperator);

		if (value is not IComparable)
		{
			throw new ArgumentException("is expected to implement IComparable interface.", nameof(value));
		}

		if (!Enum.IsDefined(typeof(OperatorType), ComparisonOperator))
		{
			throw new ArgumentOutOfRangeException($"is expected to be of type {nameof(OperatorType)}", nameof(ComparisonOperator));
		}

		if (!(TrueObject == null ^ FalseObject != null))
		{
			throw new ArgumentNullException(nameof(TrueObject), $"{nameof(TrueObject)} and {nameof(FalseObject)} should be either defined both or omitted both.");
		}

		if (TrueObject != null)
		{
			mode = Modes.Object;
		}

		var valueIComparable = (IComparable)value;
		var result = valueIComparable.CompareTo(ComparingValue);

		return ComparisonOperator switch
		{
			OperatorType.Smaller => EvaluateCondition(result < 0),
			OperatorType.SmallerOrEqual => EvaluateCondition(result <= 0),
			OperatorType.Equal => EvaluateCondition(result == 0),
			OperatorType.NotEqual => EvaluateCondition(result != 0),
			OperatorType.GreaterOrEqual => EvaluateCondition(result >= 0),
			OperatorType.Greater => EvaluateCondition(result > 0),
			_ => throw new NotSupportedException($"\"{ComparisonOperator}\" is not supported."),
		};
	}

	object EvaluateCondition(bool comparisonResult)
	{
		if (comparisonResult)
		{
			return mode == Modes.Object ? TrueObject! : true;
		}
		else if (mode == Modes.Object)
		{
			return FalseObject!;
		}
		else
		{
			return false;
		}
	}
}