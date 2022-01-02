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
	Constant,
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
	/// <param name="precedence">Math Operator Preference</param>
	/// <param name="calculateFunc">Calculation Function</param>
	public MathOperator(
		string name,
		int numericCount,
		MathOperatorPrecedence precedence,
		Func<double[], double> calculateFunc)
	{
		Name = name;
		CalculateFunc = calculateFunc;
		Precedence = precedence;
		NumericCount = numericCount;
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
	/// Math Operator Precedence
	/// </summary>
	public MathOperatorPrecedence Precedence { get; }

	/// <summary>
	/// Calculation Function
	/// </summary>
	public Func<double[], double> CalculateFunc { get; }
}