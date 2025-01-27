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
/// <remarks>
/// Initialize <see cref="MathOperator"/>
/// </remarks>
/// <param name="name">Name</param>
/// <param name="numericCount">Number of Numerals</param>
/// <param name="calculateFunc">Calculation Function</param>
public sealed class MathOperator(
	string name,
	int numericCount,
	Func<object?[], object?> calculateFunc)
{

	/// <summary>
	/// Name
	/// </summary>
	public string Name { get; } = name;

	/// <summary>
	/// Number of Numerals
	/// </summary>
	public int NumericCount { get; } = numericCount;

	/// <summary>
	/// Calculation Function
	/// </summary>
	public Func<object?[], object?> CalculateFunc { get; } = calculateFunc;
}