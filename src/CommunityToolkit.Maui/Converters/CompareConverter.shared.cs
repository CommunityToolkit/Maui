using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts an object that implements IComparable to an object or a boolean based on a comparison.
/// </summary>
public sealed class CompareConverter : CompareConverter<IComparable, object>
{
}

/// <summary>
/// Converts an object that implements IComparable to an object or a boolean based on a comparison.
/// </summary>
public abstract class CompareConverter<TValue, TReturnObject> : BaseConverterOneWay<TValue, object> where TValue : IComparable
{
	/// <inheritdoc/>
	public override object DefaultConvertReturnValue { get; set; } = new();

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

	/// <summary>
	/// The comparing value.
	/// </summary>
	public TValue? ComparingValue { get; set; }

	/// <summary>
	/// The comparison operator.
	/// </summary>
	public OperatorType ComparisonOperator { get; set; }

	/// <summary>
	/// The object that corresponds to True value.
	/// </summary>
	public TReturnObject? TrueObject { get; set; }

	/// <summary>
	/// The object that corresponds to False value.
	/// </summary>
	public TReturnObject? FalseObject { get; set; }

	/// <summary>
	/// Converts an object that implements IComparable to a specified object or a boolean based on a comparison result.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="culture">The culture to use in the converter.  This is not implemented.</param>
	/// <returns>The object assigned to <see cref="TrueObject"/> if (value <see cref="ComparisonOperator"/> <see cref="ComparingValue"/>) equals True and <see cref="TrueObject"/> is not null, if <see cref="TrueObject"/> is null it returns true, otherwise the value assigned to <see cref="FalseObject"/>, if no value is assigned then it returns false.</returns>
	[MemberNotNull(nameof(ComparingValue))]
	public override object ConvertFrom(TValue value, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(value);
		ArgumentNullException.ThrowIfNull(ComparingValue);
		ArgumentNullException.ThrowIfNull(ComparisonOperator);

		if (!Enum.IsDefined(typeof(OperatorType), ComparisonOperator))
		{
			throw new InvalidEnumArgumentException(nameof(ComparisonOperator), (int)ComparisonOperator, typeof(OperatorType));
		}

		if (!(TrueObject is null ^ FalseObject is not null))
		{
			throw new InvalidOperationException($"{nameof(TrueObject)} and {nameof(FalseObject)} should either be both defined both or both omitted.");
		}

		var result = value.CompareTo(ComparingValue);
		var shouldReturnObjectResult = TrueObject is not null && FalseObject is not null;

		return ComparisonOperator switch
		{
			OperatorType.Smaller => EvaluateCondition(result < 0, shouldReturnObjectResult),
			OperatorType.SmallerOrEqual => EvaluateCondition(result <= 0, shouldReturnObjectResult),
			OperatorType.Equal => EvaluateCondition(result is 0, shouldReturnObjectResult),
			OperatorType.NotEqual => EvaluateCondition(result is not 0, shouldReturnObjectResult),
			OperatorType.GreaterOrEqual => EvaluateCondition(result >= 0, shouldReturnObjectResult),
			OperatorType.Greater => EvaluateCondition(result > 0, shouldReturnObjectResult),
			_ => throw new NotSupportedException($"\"{ComparisonOperator}\" is not supported."),
		};
	}

	object EvaluateCondition(bool comparisonResult, bool shouldReturnObject) => (comparisonResult, shouldReturnObject) switch
	{
		(true, true) => TrueObject ?? throw new InvalidOperationException($"{nameof(TrueObject)} cannot be null"),
		(false, true) => FalseObject ?? throw new InvalidOperationException($"{nameof(FalseObject)} cannot be null"),
		(true, _) => true,
		_ => false
	};
}