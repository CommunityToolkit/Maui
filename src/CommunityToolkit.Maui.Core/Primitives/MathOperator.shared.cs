namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Precedence for Math Opertator
/// </summary>
public enum MathOperatorPrecedence
{
	/// <summary>Lowest</summary>
	Lowest,
	/// <summary>Low</summary>
	Low,
	/// <summary>Medium</summary>
	Medium,
	/// <summary>High</summary>
	High,
	/// <summary>Constant</summary>
	Constant
}

/// <summary>
/// Math Operator
/// </summary>
public sealed class MathOperator
{
	/// <summary>
	/// Initialize <see cref="MathOperator"/>
	/// </summary>
	/// <param name="name">Name</param>
	/// <param name="numericCount">Number of Numerals</param>
	/// <param name="calculateFunc">Calculation Function</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> or <paramref name="calculateFunc"/> is null.</exception>
	public MathOperator(
		string name,
		int numericCount,
		Func<object?[], object?> calculateFunc)
	{
		ArgumentNullException.ThrowIfNull(name);
		ArgumentNullException.ThrowIfNull(calculateFunc);

		Name = name;
		NumericCount = numericCount;
		CalculateFunc = calculateFunc;
	}

	/// <summary>
	/// Name
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Number of Numerals
	/// </summary>
	public int NumericCount { get; }

	/// <summary>
	/// Calculation Function
	/// </summary>
	public Func<object?[], object?> CalculateFunc { get; }
}