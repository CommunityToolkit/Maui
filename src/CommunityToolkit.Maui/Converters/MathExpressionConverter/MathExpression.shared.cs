using System.Globalization;
using System.Text.RegularExpressions;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Converters;

enum MathTokenType
{
	Value,
	Operator,
};

record MathToken(MathTokenType type, string text, object? value);

sealed partial class MathExpression
{
	const NumberStyles numberStyle = NumberStyles.Float | NumberStyles.AllowThousands;

	static readonly IFormatProvider formatProvider = new CultureInfo("en-US");

	readonly IReadOnlyList<MathOperator> operators;
	readonly IReadOnlyList<object> arguments;

	internal static bool __bool(object? b) =>
		 b switch
		 {
			 bool x => x,
			 null => false,
			 double doubleValue => doubleValue != 0 && doubleValue != double.NaN,
			 string stringValue => !string.IsNullOrEmpty(stringValue),
			 _ => Convert.ToBoolean(b!)
		 };

	internal MathExpression(string expression, IEnumerable<object>? arguments = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(expression, "Expression can't be null or empty.");

		var argumentList = arguments?.ToList() ?? [];

		expr = expression.ToLower();

		var operators = new List<MathOperator>
		{
			new ("+", 2, x => Convert.ToDouble(x[0]) + Convert.ToDouble(x[1])),
			new ("-", 2, x => Convert.ToDouble(x[0]) - Convert.ToDouble(x[1])),
			new ("*", 2, x => Convert.ToDouble(x[0]) * Convert.ToDouble(x[1])),
			new ("/", 2, x => Convert.ToDouble(x[0]) / Convert.ToDouble(x[1])),
			new ("%", 2, x => Convert.ToDouble(x[0]) % Convert.ToDouble(x[1])),

			new ("&&", 2, x => __bool(x[0]) ? x[1] : x[0]),
			new ("||", 2, x => __bool(x[0]) ? x[0] : x[1]),

			new ("==", 2, x => object.Equals(x[0], x[1])),
			new ("!=", 2, x => !object.Equals(x[0], x[1])),

			new (">=", 2, x => Convert.ToDouble(x[0]) >= Convert.ToDouble(x[1])),
			new (">", 2, x => Convert.ToDouble(x[0]) > Convert.ToDouble(x[1])),
			new ("<=", 2, x => Convert.ToDouble(x[0]) <= Convert.ToDouble(x[1])),
			new ("<", 2, x => Convert.ToDouble(x[0]) < Convert.ToDouble(x[1])),
			new ("neg", 1, x => -Convert.ToDouble(x[0])),
			new ("not", 1, x => !__bool(x[0])),
			new ("if", 3, x => __bool(x[0]) ? x[1] : x[2]),

			new ("abs", 1, x => Math.Abs(Convert.ToDouble(x[0]))),
			new ("acos", 1, x => Math.Acos(Convert.ToDouble(x[0]))),
			new ("asin", 1, x => Math.Asin(Convert.ToDouble(x[0]))),
			new ("atan", 1, x => Math.Atan(Convert.ToDouble(x[0]))),
			new ("atan2", 2, x => Math.Atan2(Convert.ToDouble(x[0]), Convert.ToDouble(x[1]))),
			new ("ceiling", 1, x => Math.Ceiling(Convert.ToDouble(x[0]))),
			new ("cos", 1, x => Math.Cos(Convert.ToDouble(x[0]))),
			new ("cosh", 1, x => Math.Cosh(Convert.ToDouble(x[0]))),
			new ("exp", 1, x => Math.Exp(Convert.ToDouble(x[0]))),
			new ("floor", 1, x => Math.Floor(Convert.ToDouble(x[0]))),
			new ("ieeeremainder", 2, x => Math.IEEERemainder(Convert.ToDouble(x[0]), Convert.ToDouble(x[1]))),
			new ("log", 2, x => Math.Log(Convert.ToDouble(x[0]), Convert.ToDouble(x[1]))),
			new ("log10", 1, x => Math.Log10(Convert.ToDouble(x[0]))),
			new ("max", 2, x => Math.Max(Convert.ToDouble(x[0]), Convert.ToDouble(x[1]))),
			new ("min", 2, x => Math.Min(Convert.ToDouble(x[0]), Convert.ToDouble(x[1]))),
			new ("pow", 2, x => Math.Pow(Convert.ToDouble(x[0]), Convert.ToDouble(x[1]))),
			new ("round", 2, x => Math.Round(Convert.ToDouble(x[0]), Convert.ToInt32(x[1]))),
			new ("sign", 1, x => Math.Sign(Convert.ToDouble(x[0]))),
			new ("sin", 1, x => Math.Sin(Convert.ToDouble(x[0]))),
			new ("sinh", 1, x => Math.Sinh(Convert.ToDouble(x[0]))),
			new ("sqrt", 1, x => Math.Sqrt(Convert.ToDouble(x[0]))),
			new ("tan", 1, x => Math.Tan(Convert.ToDouble(x[0]))),
			new ("tanh", 1, x => Math.Tanh(Convert.ToDouble(x[0]))),
			new ("truncate", 1, x => Math.Truncate(Convert.ToDouble(x[0]))),
			new ("int", 1, x => Convert.ToInt32(x[0])),
			new ("double", 1, x => Convert.ToDouble(x[0])),
			new ("bool", 1, x => Convert.ToBoolean(x[0])),
			new ("str", 1, x => x[0]?.ToString()),
			new ("len", 1, x => x[0]?.ToString()?.Length),
			new ("^", 2, x => Math.Pow(Convert.ToDouble(x[0]), Convert.ToDouble(x[1]))),
			new ("pi", 0, _ => Math.PI),
			new ("e", 0, _ => Math.E),
			new ("true", 0, _ => true),
			new ("false", 0, _ => false),
			new ("null", 0, _ => null),
		};

		if (argumentList.Count > 0)
		{
			operators.Add(new MathOperator("x", 0, _ => argumentList[0]));
		}

		for (var i = 0; i < argumentList.Count; i++)
		{
			var index = i;
			operators.Add(new MathOperator($"x{i}", 0, _ => argumentList[index]));
		}

		this.operators = operators;
		this.arguments = argumentList;
	}

	internal string expr { get; }

	internal int exprIndex { get; set; } = 0;

	internal Match patternMatch { get; set; } = Match.Empty;

	internal List<MathToken> rpn { get; } = new();

	public object? Calculate()
	{
		if (!ParseExpression())
		{
			throw new ArgumentException("Invalid math expression.");
		}

		var stack = new Stack<object?>();

		foreach (var token in rpn)
		{
			if (token.type == MathTokenType.Value)
			{
				stack.Push(token.value);
				continue;
			}

			var mathOperator = operators.FirstOrDefault(x => x.Name == token.text) ??
				throw new ArgumentException($"Invalid math expression. Can't find operator or value with name \"{token.text}\".");

			if (mathOperator.NumericCount == 0)
			{
				stack.Push(mathOperator.CalculateFunc([]));
				continue;
			}

			var operatorNumericCount = mathOperator.NumericCount;

			if (stack.Count < operatorNumericCount)
			{
				throw new ArgumentException("Invalid math expression.");
			}

			bool nullGuard = false;
			var args = new List<object?>();
			for (var j = 0; j < operatorNumericCount; j++)
			{
				object? val = stack.Pop();
				args.Add(val);
				nullGuard = nullGuard || (val is null);
			}

			args.Reverse();

			switch (mathOperator.Name)
			{
				case "if":
				case "&&":
				case "||":
				case "==":
				case "!=":
					nullGuard = false;
					break;
			}

			stack.Push(!nullGuard ? mathOperator.CalculateFunc([.. args]) : null);
		}

		if (stack.Count != 1)
		{
			throw new ArgumentException("Invalid math expression.");
		}

		return stack.Pop();
	}

	bool ParseExpression()
	{
		exprIndex = 0;
		rpn.Clear();
		return ParseExpr() && exprIndex == expr.Length;
	}

	bool ParseExpr()
	{
		return ParseConditional();
	}

	[GeneratedRegex("""^(\?)""")]
	private static partial Regex ConditionalStart();

	[GeneratedRegex("""^(\:)""")]
	private static partial Regex ConditionalElse();

	bool ParseConditional()
	{
		if (!ParseLogicalOR())
		{
			return false;
		}

		if (!ParsePattern(ConditionalStart()))
		{
			return true;
		}

		if (!ParseLogicalOR())
		{
			return false;
		}

		if (!ParsePattern(ConditionalElse()))
		{
			return false;
		}

		if (!ParseLogicalOR())
		{
			return false;
		}

		rpn.Add(new MathToken(MathTokenType.Operator, "if", null));
		return true;
	}

	[GeneratedRegex("""^(\|\||or)""")]
	private static partial Regex LogicalOROperator();

	bool ParseLogicalOR() => ParseBinaryOperators(LogicalOROperator(), ParseLogicalAnd);

	[GeneratedRegex("""^(\&\&|and)""")]
	private static partial Regex LogicalAndOperator();

	bool ParseLogicalAnd() => ParseBinaryOperators(LogicalAndOperator(), ParseEquality);

	[GeneratedRegex("""^(==|!=|eq|ne)""")]
	private static partial Regex EqualityOperators();

	bool ParseEquality() => ParseBinaryOperators(EqualityOperators(), ParseCompare);

	[GeneratedRegex("""^(\<\=|\>\=|\<|\>|le|ge|lt|gt)""")]
	private static partial Regex CompareOperators();

	bool ParseCompare() => ParseBinaryOperators(CompareOperators(), ParseSum);

	[GeneratedRegex("""^(\+|\-)""")]
	private static partial Regex SumOperators();

	bool ParseSum() => ParseBinaryOperators(SumOperators(), ParseProduct);

	[GeneratedRegex("""^(\*|\/|\%)""")]
	private static partial Regex ProductOperators();

	bool ParseProduct() => ParseBinaryOperators(ProductOperators(), ParsePower);

	[GeneratedRegex("""^(\^)""")]
	private static partial Regex PowerOperator();

	bool ParsePower() => ParseBinaryOperators(PowerOperator(), ParsePrimary);

	[GeneratedRegex("""^(\-|\!)""")]
	private static partial Regex UnaryOperators();

	static Dictionary<string, string> unaryMapping { get; } = new Dictionary<string, string>()
	{
		{ "-", "neg" },
		{ "!", "not" }
	};

	bool ParseBinaryOperators(Regex BinaryOperators, Func<bool> ParseNext)
	{
		if (!ParseNext())
		{
			return false;
		}
		int index = exprIndex;
		while (ParsePattern(BinaryOperators))
		{
			string _operator = patternMatch.Groups[1].Value;
			if (!ParseNext())
			{
				exprIndex = index;
				return false;
			}
			rpn.Add(new MathToken(MathTokenType.Operator, _operator, null));
			index = exprIndex;
		}
		return true;
	}

	[GeneratedRegex("""^(\-?\d+\.\d+|\-?\d+)""")]
	private static partial Regex NumberPattern();

	[GeneratedRegex("""^["]([^"]*)["]""")]
	private static partial Regex StringPattern();

	[GeneratedRegex("""^(\w+)""")]
	private static partial Regex Constants();

	[GeneratedRegex("""^(\()""")]
	private static partial Regex ParenStart();

	[GeneratedRegex("""^(\))""")]
	private static partial Regex ParenEnd();

	bool ParsePrimary()
	{
		if (ParsePattern(NumberPattern()))
		{
			string _number = patternMatch.Groups[1].Value;
			rpn.Add(new MathToken(MathTokenType.Value, _number, double.Parse(_number)));
			return true;
		}

		if (ParsePattern(StringPattern()))
		{
			string _string = patternMatch.Groups[1].Value;
			rpn.Add(new MathToken(MathTokenType.Value, _string, _string));
			return true;
		}

		if (ParseFunction())
		{
			return true;
		}

		if (ParsePattern(Constants()))
		{
			string _constant = patternMatch.Groups[1].Value;
			rpn.Add(new MathToken(MathTokenType.Operator, _constant, null));
			return true;
		}

		int index = exprIndex;
		if (ParsePattern(ParenStart()))
		{
			if (!ParseExpr())
			{
				exprIndex = index;
				return false;
			}
			if (!ParsePattern(ParenEnd()))
			{
				exprIndex = index;
				return false;
			}
			return true;
		}

		index = exprIndex;
		if (ParsePattern(UnaryOperators()))
		{
			string _operator = patternMatch.Groups[1].Value;
			if (unaryMapping.ContainsKey(_operator))
			{
				_operator = unaryMapping[_operator];
			}
			if (!ParsePrimary())
			{
				exprIndex = index;
				return false;
			}
			rpn.Add(new MathToken(MathTokenType.Operator, _operator, null));
			return true;
		}

		return false;
	}

	[GeneratedRegex("""^(\w+)\(""")]
	private static partial Regex FunctionStart();

	[GeneratedRegex("""^(\,)""")]
	private static partial Regex Comma();

	[GeneratedRegex("""^(\))""")]
	private static partial Regex FunctionEnd();

	bool ParseFunction()
	{
		int index = exprIndex;
		if (!ParsePattern(FunctionStart()))
		{
			return false;
		}

		string text = patternMatch.Groups[0].Value;
		string functionName = patternMatch.Groups[1].Value;

		if (!ParseExpr())
		{
			exprIndex = index;
			return false;
		}

		while (ParsePattern(Comma()))
		{
			if (!ParseExpr())
			{
				exprIndex = index;
				return false;
			}
			index = exprIndex;
		}

		if (!ParsePattern(FunctionEnd()))
		{
			exprIndex = index;
			return false;
		}

		rpn.Add(new MathToken(MathTokenType.Operator, functionName, null));

		return true;
	}

	[GeneratedRegex("""^\s*""")]
	private static partial Regex Whitespace();

	public bool ParsePattern(Regex regex)
	{
		var whitespaceMatch = Whitespace().Match(expr.Substring(exprIndex));
		if (whitespaceMatch.Success)
		{
			exprIndex += whitespaceMatch.Length;
		}

		patternMatch = regex.Match(expr.Substring(exprIndex));
		if (!patternMatch.Success)
		{
			return false;
		}
		exprIndex += patternMatch.Length;

		whitespaceMatch = Whitespace().Match(expr.Substring(exprIndex));
		if (whitespaceMatch.Success)
		{
			exprIndex += whitespaceMatch.Length;
		}

		return true;
	}
}
