using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Converters;

enum MathTokenType
{
	Value,
	Operator,
}

sealed record MathToken(MathTokenType Type, string Text, object? Value);

sealed partial class MathExpression
{
	CultureInfo culture { get; init; } = CultureInfo.InvariantCulture;

	readonly IReadOnlyList<MathOperator> operators;

	internal MathExpression(in string expression, in IReadOnlyList<object?> arguments, CultureInfo culture)
	{
		ArgumentException.ThrowIfNullOrEmpty(expression, "Expression can't be null or empty.");
		ArgumentNullException.ThrowIfNull(arguments, "Arguments cannot be null.");

		Expression = expression.ToLower();

		List<MathOperator> operators =
		[
			new ("+", 2, x => ConvertToDouble(x[0]) + ConvertToDouble(x[1])),
			new ("-", 2, x => ConvertToDouble(x[0]) - ConvertToDouble(x[1])),
			new ("*", 2, x => ConvertToDouble(x[0]) * ConvertToDouble(x[1])),
			new ("/", 2, x => ConvertToDouble(x[0]) / ConvertToDouble(x[1])),
			new ("%", 2, x => ConvertToDouble(x[0]) % ConvertToDouble(x[1])),

			new ("and", 2, x => ConvertToBoolean(x[0]) ? x[1] : x[0]),
			new ("or", 2, x => ConvertToBoolean(x[0]) ? x[0] : x[1]),

			new ("==", 2, x => object.Equals(x[0], x[1])),
			new ("!=", 2, x => !object.Equals(x[0], x[1])),

			new ("ge", 2, x => ConvertToDouble(x[0]) >= ConvertToDouble(x[1])),
			new ("gt", 2, x => ConvertToDouble(x[0]) > ConvertToDouble(x[1])),
			new ("le", 2, x => ConvertToDouble(x[0]) <= ConvertToDouble(x[1])),
			new ("lt", 2, x => ConvertToDouble(x[0]) < ConvertToDouble(x[1])),
			new ("neg", 1, x => -ConvertToDouble(x[0])),
			new ("not", 1, x => !ConvertToBoolean(x[0])),
			new ("if", 3, x => ConvertToBoolean(x[0]) ? x[1] : x[2]),

			new ("abs", 1, x => Math.Abs(ConvertToDouble(x[0]))),
			new ("acos", 1, x => Math.Acos(ConvertToDouble(x[0]))),
			new ("asin", 1, x => Math.Asin(ConvertToDouble(x[0]))),
			new ("atan", 1, x => Math.Atan(ConvertToDouble(x[0]))),
			new ("atan2", 2, x => Math.Atan2(ConvertToDouble(x[0]), ConvertToDouble(x[1]))),
			new ("ceiling", 1, x => Math.Ceiling(ConvertToDouble(x[0]))),
			new ("cos", 1, x => Math.Cos(ConvertToDouble(x[0]))),
			new ("cosh", 1, x => Math.Cosh(ConvertToDouble(x[0]))),
			new ("exp", 1, x => Math.Exp(ConvertToDouble(x[0]))),
			new ("floor", 1, x => Math.Floor(ConvertToDouble(x[0]))),
			new ("ieeeremainder", 2, x => Math.IEEERemainder(ConvertToDouble(x[0]), ConvertToDouble(x[1]))),
			new ("log", 2, x => Math.Log(ConvertToDouble(x[0]), ConvertToDouble(x[1]))),
			new ("log10", 1, x => Math.Log10(ConvertToDouble(x[0]))),
			new ("max", 2, x => Math.Max(ConvertToDouble(x[0]), ConvertToDouble(x[1]))),
			new ("min", 2, x => Math.Min(ConvertToDouble(x[0]), ConvertToDouble(x[1]))),
			new ("pow", 2, x => Math.Pow(ConvertToDouble(x[0]), ConvertToDouble(x[1]))),
			new ("round", 2, x => Math.Round(ConvertToDouble(x[0]), ConvertToInt32(x[1]))),
			new ("sign", 1, x => Math.Sign(ConvertToDouble(x[0]))),
			new ("sin", 1, x => Math.Sin(ConvertToDouble(x[0]))),
			new ("sinh", 1, x => Math.Sinh(ConvertToDouble(x[0]))),
			new ("sqrt", 1, x => Math.Sqrt(ConvertToDouble(x[0]))),
			new ("tan", 1, x => Math.Tan(ConvertToDouble(x[0]))),
			new ("tanh", 1, x => Math.Tanh(ConvertToDouble(x[0]))),
			new ("truncate", 1, x => Math.Truncate(ConvertToDouble(x[0]))),
			new ("int", 1, x => ConvertToInt32(x[0])),
			new ("double", 1, x => ConvertToDouble(x[0])),
			new ("bool", 1, x => ConvertToBoolean(x[0])),
			new ("str", 1, x => ConvertToString(x[0])),
			new ("len", 1, x => ConvertToString(x[0])?.Length),
			new ("^", 2, x => Math.Pow(ConvertToDouble(x[0]), ConvertToDouble(x[1]))),
			new ("pi", 0, _ => Math.PI),
			new ("e", 0, _ => Math.E),
			new ("true", 0, _ => true),
			new ("false", 0, _ => false),
			new ("null", 0, _ => null),
		];

		if (arguments.Count > 0)
		{
			var firstArgument = arguments[0];
			operators.Add(new MathOperator("x", 0, _ => firstArgument));
		}

		for (var i = 0; i < arguments.Count; i++)
		{
			var currentArgument = arguments[i];
			operators.Add(new MathOperator($"x{i}", 0, _ => currentArgument));
		}

		this.operators = operators;
		this.culture = culture;
	}

	static ReadOnlyDictionary<string, string> BinaryMappingDictionary { get; } = new Dictionary<string, string>
	{
		{ "<", "lt" },
		{ "<=", "le" },
		{ ">", "gt" },
		{ ">=", "ge" },
		{ "&&", "and" },
		{ "||", "or" }
	}.AsReadOnly();

	static ReadOnlyDictionary<string, string> UnaryMappingDictionary { get; } = new Dictionary<string, string>
	{
		{ "-", "neg" },
		{ "!", "not" }
	}.AsReadOnly();

	string Expression { get; }

	List<MathToken> RPN { get; } = [];

	int ExpressionIndex { get; set; }

	Match PatternMatch { get; set; } = Match.Empty;

	public object? CalculateResult()
	{
		if (!ParseExpression())
		{
			throw new ArgumentException("Math Expression Invalid. Failed to parse math expression.");
		}

		var stack = new Stack<object?>();

		foreach (var token in RPN)
		{
			if (token.Type is MathTokenType.Value)
			{
				stack.Push(token.Value);
				continue;
			}

			var mathOperator = operators.FirstOrDefault(x => x.Name == token.Text) ?? throw new ArgumentException($"Math Expression Invalid. Can't find operator or value with name \"{token.Text}\".");

			if (mathOperator.NumericCount is 0)
			{
				stack.Push(mathOperator.CalculateFunc([]));
				continue;
			}

			var operatorNumericCount = mathOperator.NumericCount;

			if (stack.Count < operatorNumericCount)
			{
				throw new ArgumentException($"Math Expression Invalid. Insufficient parameters to operator \"{mathOperator.Name}\".");
			}

			bool containsNullArgument = false;
			List<object?> args = [];

			for (var j = 0; j < operatorNumericCount; j++)
			{
				object? val = stack.Pop();
				args.Add(val);
				containsNullArgument = containsNullArgument || val is null;
			}

			args.Reverse();

			containsNullArgument = mathOperator.Name switch
			{
				"if" => args[0] is null,
				"and" or "or" or "==" or "!=" => false,
				_ => containsNullArgument
			};

			stack.Push(!containsNullArgument ? mathOperator.CalculateFunc([.. args]) : null);
		}

		return stack.Count switch
		{
			0 => throw new InvalidOperationException($"Math Expression Invalid. Stack is unexpectedly empty."),
			> 1 => throw new InvalidOperationException($"Math Expression Invalid. Stack unexpectedly contains multiple items ({stack.Count}) items when it should contain only the final result."),
			_ => stack.Pop()
		};
	}

	[GeneratedRegex("""^(\w+)\(""")]
	private static partial Regex EvaluateFunctionStart();

	[GeneratedRegex("""^(\,)""")]
	private static partial Regex EvaluateComma();

	[GeneratedRegex("""^(\))""")]
	private static partial Regex EvaluateFunctionEnd();

	[GeneratedRegex("""^(\?)""")]
	private static partial Regex EvaluateConditionalStart();

	[GeneratedRegex("""^(\:)""")]
	private static partial Regex EvaluateConditionalElse();

	[GeneratedRegex("""^(\|\||or)""")]
	private static partial Regex EvaluateLogicalOROperator();

	[GeneratedRegex("""^(\&\&|and)""")]
	private static partial Regex EvaluateLogicalAndOperator();

	[GeneratedRegex("""^(==|!=|eq|ne)""")]
	private static partial Regex EvaluateEqualityOperators();

	[GeneratedRegex("""^(\<\=|\>\=|\<|\>|le|ge|lt|gt)""")]
	private static partial Regex EvaluateCompareOperators();

	[GeneratedRegex("""^(\+|\-)""")]
	private static partial Regex EvaluateSumOperators();

	[GeneratedRegex("""^(\*|\/|\%)""")]
	private static partial Regex EvaluateProductOperators();

	[GeneratedRegex("""^(\^)""")]
	private static partial Regex EvaluatePowerOperator();

	[GeneratedRegex("""^(\-|\!)""")]
	private static partial Regex EvaluateUnaryOperators();

	[GeneratedRegex("""^(\-?\d+[\.,]\d+|\-?\d+)""")]
	private static partial Regex EvaluateNumberPattern();

	[GeneratedRegex("""^["]([^"]*)["]""")]
	private static partial Regex EvaluateStringPattern();

	[GeneratedRegex("""^(\w+)""")]
	private static partial Regex EvaluateConstants();

	[GeneratedRegex("""^(\()""")]
	private static partial Regex EvaluateParenStart();

	[GeneratedRegex("""^(\))""")]
	private static partial Regex EvaluateParenEnd();

	[GeneratedRegex("""^\s*""")]
	private static partial Regex EvaluateWhitespace();

	bool ConvertToBoolean(object? b) => b switch
	{
		bool x => x,
		null => false,
		double doubleValue => doubleValue != 0 && !double.IsNaN(doubleValue),
		string stringValue => !string.IsNullOrEmpty(stringValue),
		_ => Convert.ToBoolean(b, culture)
	};

	double ConvertToDouble(object? x) => Convert.ToDouble(x, culture);

	int ConvertToInt32(object? x) => Convert.ToInt32(x, culture);

	string? ConvertToString(object? x) => Convert.ToString(x, culture);

	bool ParsePattern(Regex regex)
	{
		var whitespaceMatch = EvaluateWhitespace().Match(Expression[ExpressionIndex..]);
		if (whitespaceMatch.Success)
		{
			ExpressionIndex += whitespaceMatch.Length;
		}

		PatternMatch = regex.Match(Expression[ExpressionIndex..]);
		if (!PatternMatch.Success)
		{
			return false;
		}
		ExpressionIndex += PatternMatch.Length;

		whitespaceMatch = EvaluateWhitespace().Match(Expression[ExpressionIndex..]);
		if (whitespaceMatch.Success)
		{
			ExpressionIndex += whitespaceMatch.Length;
		}

		return true;
	}

	bool ParseExpression()
	{
		ExpressionIndex = 0;
		RPN.Clear();
		return ParseExpr() && ExpressionIndex == Expression.Length;
	}

	bool ParseExpr()
	{
		return ParseConditional();
	}

	bool ParseConditional()
	{
		if (!ParseLogicalOR())
		{
			return false;
		}

		if (!ParsePattern(EvaluateConditionalStart()))
		{
			return true;
		}

		if (!ParseLogicalOR())
		{
			return false;
		}

		if (!ParsePattern(EvaluateConditionalElse()))
		{
			return false;
		}

		if (!ParseLogicalOR())
		{
			return false;
		}

		RPN.Add(new MathToken(MathTokenType.Operator, "if", null));
		return true;
	}

	bool ParseLogicalOR() => ParseBinaryOperators(EvaluateLogicalOROperator(), ParseLogicalAnd);

	bool ParseLogicalAnd() => ParseBinaryOperators(EvaluateLogicalAndOperator(), ParseEquality);

	bool ParseEquality() => ParseBinaryOperators(EvaluateEqualityOperators(), ParseCompare);

	bool ParseCompare() => ParseBinaryOperators(EvaluateCompareOperators(), ParseSum);

	bool ParseSum() => ParseBinaryOperators(EvaluateSumOperators(), ParseProduct);

	bool ParseProduct() => ParseBinaryOperators(EvaluateProductOperators(), ParsePower);

	bool ParsePower() => ParseBinaryOperators(EvaluatePowerOperator(), ParsePrimary);

	bool ParseBinaryOperators(Regex BinaryOperators, Func<bool> ParseNext)
	{
		if (!ParseNext())
		{
			return false;
		}
		int index = ExpressionIndex;
		while (ParsePattern(BinaryOperators))
		{
			string _operator = PatternMatch.Groups[1].Value;
			if (BinaryMappingDictionary.TryGetValue(_operator, out var value))
			{
				_operator = value;
			}
			if (!ParseNext())
			{
				ExpressionIndex = index;
				return false;
			}
			RPN.Add(new MathToken(MathTokenType.Operator, _operator, null));
			index = ExpressionIndex;
		}
		return true;
	}

	bool ParsePrimary()
	{
		if (ParsePattern(EvaluateNumberPattern()))
		{
			string _number = PatternMatch.Groups[1].Value;
			RPN.Add(new MathToken(MathTokenType.Value, _number, double.Parse(_number, culture)));
			return true;
		}

		if (ParsePattern(EvaluateStringPattern()))
		{
			string _string = PatternMatch.Groups[1].Value;
			RPN.Add(new MathToken(MathTokenType.Value, _string, _string));
			return true;
		}

		if (ParseFunction())
		{
			return true;
		}

		if (ParsePattern(EvaluateConstants()))
		{
			string _constant = PatternMatch.Groups[1].Value;
			RPN.Add(new MathToken(MathTokenType.Operator, _constant, null));
			return true;
		}

		int index = ExpressionIndex;
		if (ParsePattern(EvaluateParenStart()))
		{
			if (!ParseExpr())
			{
				ExpressionIndex = index;
				return false;
			}
			if (!ParsePattern(EvaluateParenEnd()))
			{
				ExpressionIndex = index;
				return false;
			}
			return true;
		}

		index = ExpressionIndex;
		if (ParsePattern(EvaluateUnaryOperators()))
		{
			string _operator = PatternMatch.Groups[1].Value;
			if (UnaryMappingDictionary.TryGetValue(_operator, out var value))
			{
				_operator = value;
			}
			if (!ParsePrimary())
			{
				ExpressionIndex = index;
				return false;
			}
			RPN.Add(new MathToken(MathTokenType.Operator, _operator, null));
			return true;
		}

		return false;
	}

	bool ParseFunction()
	{
		int index = ExpressionIndex;
		if (!ParsePattern(EvaluateFunctionStart()))
		{
			return false;
		}

		string text = PatternMatch.Groups[0].Value;
		string functionName = PatternMatch.Groups[1].Value;

		if (!ParseExpr())
		{
			ExpressionIndex = index;
			return false;
		}

		while (ParsePattern(EvaluateComma()))
		{
			if (!ParseExpr())
			{
				ExpressionIndex = index;
				return false;
			}
			index = ExpressionIndex;
		}

		if (!ParsePattern(EvaluateFunctionEnd()))
		{
			ExpressionIndex = index;
			return false;
		}

		RPN.Add(new MathToken(MathTokenType.Operator, functionName, null));

		return true;
	}
}