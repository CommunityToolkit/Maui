using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CommunityToolkit.Maui.SourceGenerators.Helpers;

/// <summary>
/// Resolves property initializer expressions to fully qualified C# code suitable for use
/// as the <c>defaultValue</c> parameter in <c>BindableProperty.Create</c>.
/// </summary>
static class InitializerExpressionResolver
{
	/// <summary>
	/// Attempts to resolve an initializer expression to a fully qualified C# expression string.
	/// Returns <c>null</c> if the expression cannot be resolved (e.g. collection expressions, complex lambdas).
	/// </summary>
	public static bool TryResolve(ExpressionSyntax expression, SemanticModel semanticModel, [NotNullWhen(true)] out string? resolvedExpressionString)
	{
		// Try symbolic resolution first, preserves readable identifiers like float.Epsilon
		if (TryResolveExpression(expression, semanticModel, out var resolved))
		{
			resolvedExpressionString = resolved;
			return true;
		}

		// Fall back to constant folding, handles nameof(), some compiler-reduced expressions
		var constantValue = semanticModel.GetConstantValue(expression);
		if (constantValue.HasValue)
		{
			var typeInfo = semanticModel.GetTypeInfo(expression);
			resolvedExpressionString = FormatConstantValue(constantValue.Value, typeInfo.Type);
			return true;
		}

		resolvedExpressionString = null;
		return false;
	}

	static bool TryResolveExpression(ExpressionSyntax expression, SemanticModel semanticModel, [NotNullWhen(true)] out string? resolvedExpressionString)
	{
		switch (expression)
		{
			case LiteralExpressionSyntax literal:
				if (literal.IsKind(SyntaxKind.DefaultLiteralExpression))
				{
					var defaultLiteralType = semanticModel.GetTypeInfo(literal).Type;
					if (defaultLiteralType is not null)
					{
						resolvedExpressionString = $"default({defaultLiteralType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})";
						return true;
					}
				}

				resolvedExpressionString = literal.Token.Text;
				return true;

			case PrefixUnaryExpressionSyntax prefixUnary:
				if (!TryResolveExpression(prefixUnary.Operand, semanticModel, out var operand))
				{
					resolvedExpressionString = null;
					return false;
				}

				resolvedExpressionString = $"{prefixUnary.OperatorToken.Text}{operand}";
				return true;

			case MemberAccessExpressionSyntax memberAccess:
				return TryResolveMemberAccess(memberAccess, semanticModel, out resolvedExpressionString);

			case IdentifierNameSyntax identifier:
				return TryResolveIdentifier(identifier, semanticModel, out resolvedExpressionString);

			case PredefinedTypeSyntax:
				// Keywords like int, double, string, valid as-is in generated code
				resolvedExpressionString = expression.ToString();
				return true;

			case CastExpressionSyntax castExpression:
				if (!TryResolveExpression(castExpression.Expression, semanticModel, out var innerExpression))
				{
					resolvedExpressionString = null;
					return false;
				}

				var castType = semanticModel.GetTypeInfo(castExpression.Type).Type;
				if (castType is null)
				{
					resolvedExpressionString = null;
					return false;
				}

				var qualifiedCastType = castType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
				resolvedExpressionString = $"({qualifiedCastType}){innerExpression}";

				return true;

			case ParenthesizedExpressionSyntax paren:
				if (!TryResolveExpression(paren.Expression, semanticModel, out var inner))
				{
					resolvedExpressionString = null;
					return false;
				}

				resolvedExpressionString = $"({inner})";
				return true;

			case BinaryExpressionSyntax binary:
				if (!TryResolveExpression(binary.Left, semanticModel, out var left) || !TryResolveExpression(binary.Right, semanticModel, out var right))
				{
					resolvedExpressionString = null;
					return false;
				}

				resolvedExpressionString = $"{left} {binary.OperatorToken.Text} {right}";
				return true;

			case DefaultExpressionSyntax defaultExpr:
				if (defaultExpr.Type is not null)
				{
					var defType = semanticModel.GetTypeInfo(defaultExpr.Type).Type;
					if (defType is not null)
					{
						resolvedExpressionString = $"default({defType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})";
						return true;
					}
				}

				resolvedExpressionString = "default";
				return true;

			case InvocationExpressionSyntax:
				// Invocations (e.g. Guid.NewGuid(), TimeSpan.FromSeconds(5)) may return different
				// values per call; resolve only if GetConstantValue can fold them (e.g. nameof()).
				resolvedExpressionString = null;
				return false;

			case ObjectCreationExpressionSyntax objectCreation:
				var objectCreationType = semanticModel.GetTypeInfo(objectCreation).Type;
				if (objectCreationType is null || !objectCreationType.IsValueType)
				{
					resolvedExpressionString = null;
					return false;
				}

				// Could incorrectly drop the initializer block if present, so early exit in that case
				// to avoid this. If required can add a traversal to resolve the initializer block,
				// but probably a rare case so not worth the complexity for now.
				if (objectCreation.Initializer is not null)
				{
					resolvedExpressionString = null;
					return false;
				}

				return TryResolveObjectCreation(objectCreation, semanticModel, out resolvedExpressionString);

			default:
				// Collection expressions, lambdas, etc., cannot resolve
				resolvedExpressionString = null;
				return false;
		}
	}

	static bool TryResolveMemberAccess(MemberAccessExpressionSyntax memberAccess, SemanticModel semanticModel, [NotNullWhen(true)] out string? memberAccessString)
	{
		// Only resolve member accesses that are provably safe to evaluate once and share
		// across all instances (type/namespace navigation, const fields, static readonly fields).
		// Reject static properties (e.g. DateTimeOffset.UtcNow) which may return different
		// values per call or have side effects.
		var symbol = semanticModel.GetSymbolInfo(memberAccess).Symbol;
		switch (symbol)
		{
			case INamedTypeSymbol:
			case INamespaceSymbol:
				break; // Type/namespace navigation, always safe
			case IFieldSymbol { IsConst: true }:
				break; // Compile-time constants (including enum members)
			case IFieldSymbol { IsStatic: true, IsReadOnly: true }:
				break; // Static readonly fields (e.g. TimeSpan.Zero)
			default:
				memberAccessString = null;
				return false; // Reject properties, non-readonly fields, methods, etc.
		}

		if (!TryResolveExpression(memberAccess.Expression, semanticModel, out var receiver))
		{
			memberAccessString = null;
			return false;
		}

		memberAccessString = $"{receiver}.{memberAccess.Name.Identifier.Text}";
		return true;
	}

	static bool TryResolveIdentifier(IdentifierNameSyntax identifier, SemanticModel semanticModel, [NotNullWhen(true)] out string? identifierString)
	{
		var symbolInfo = semanticModel.GetSymbolInfo(identifier);
		var symbol = symbolInfo.Symbol;

		if (symbol is null)
		{
			identifierString = null;
			return false;
		}

		// For type references (e.g. the "ValidationBehaviorDefaults" in "ValidationBehaviorDefaults.Flags")
		if (symbol is INamedTypeSymbol typeSymbol)
		{
			identifierString = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
			return true;
		}

		// For namespace references (e.g. "System" in "System.TimeSpan.Zero")
		if (symbol is INamespaceSymbol namespaceSymbol)
		{
			identifierString = namespaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
			return true;
		}

		// For const or static readonly field references used as standalone identifiers
		if (symbol is IFieldSymbol { IsConst: true } or IFieldSymbol { IsStatic: true, IsReadOnly: true })
		{
			var containingType = symbol.ContainingType;
			if (containingType is not null)
			{
				identifierString = $"{containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{symbol.Name}";
				return true;
			}
		}

		identifierString = null;
		return false;
	}

	static bool TryResolveObjectCreation(ObjectCreationExpressionSyntax objectCreation, SemanticModel semanticModel, [NotNullWhen(true)] out string? objectCreationString)
	{
		var typeInfo = semanticModel.GetTypeInfo(objectCreation);
		if (typeInfo.Type is null)
		{
			objectCreationString = null;
			return false;
		}

		var qualifiedTypeName = typeInfo.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

		if (objectCreation.ArgumentList is null || objectCreation.ArgumentList.Arguments.Count is 0)
		{
			objectCreationString = $"new {qualifiedTypeName}()";
			return true;
		}

		var args = new List<string>(objectCreation.ArgumentList.Arguments.Count);
		foreach (var arg in objectCreation.ArgumentList.Arguments)
		{
			if (TryResolveExpression(arg.Expression, semanticModel, out var resolved))
			{
				args.Add(resolved);
			}
			else
			{
				objectCreationString = null;
				return false;
			}
		}

		objectCreationString = $"new {qualifiedTypeName}({string.Join(", ", args)})";
		return true;
	}

	static string FormatConstantValue(object? value, ITypeSymbol? typeSymbol)
	{
		if (value is null)
		{
			return "null";
		}

		// For enum types, cast the underlying value to the enum type
		if (typeSymbol is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType)
		{
			var qualifiedType = enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
			return $"({qualifiedType}){FormatPrimitiveValue(value)}";
		}

		return FormatPrimitiveValue(value);
	}

	static string FormatPrimitiveValue(object value) => value switch
	{
		bool b => b ? "true" : "false",
		char c => $"'{EscapeChar(c)}'",
		string s => $"\"{EscapeString(s)}\"",
		float f => float.IsPositiveInfinity(f) ? "float.PositiveInfinity"
			: float.IsNegativeInfinity(f) ? "float.NegativeInfinity"
			: float.IsNaN(f) ? "float.NaN"
			: global::System.FormattableString.Invariant($"{f}f"),
		double d => double.IsPositiveInfinity(d) ? "double.PositiveInfinity"
			: double.IsNegativeInfinity(d) ? "double.NegativeInfinity"
			: double.IsNaN(d) ? "double.NaN"
			: FormattableString.Invariant($"{d}d"),
		decimal m => FormattableString.Invariant($"{m}m"),
		byte b => b.ToString(),
		sbyte sb => sb.ToString(),
		short s => s.ToString(),
		ushort us => us.ToString(),
		int i => i.ToString(),
		uint u => $"{u}u",
		long l => $"{l}L",
		ulong ul => $"{ul}UL",
		_ => value.ToString() ?? "null"
	};

	static string EscapeChar(char c) => c switch
	{
		'\'' => @"\'",
		'\\' => @"\\",
		'\0' => @"\0",
		'\n' => @"\n",
		'\r' => @"\r",
		'\t' => @"\t",
		_ => c.ToString()
	};

	static string EscapeString(string s) => s
		.Replace("\\", @"\\")
		.Replace("\"", @"\""")
		.Replace("\n", @"\n")
		.Replace("\r", @"\r")
		.Replace("\t", @"\t")
		.Replace("\0", @"\0");
}