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
	public static string? TryResolve(ExpressionSyntax expression, SemanticModel semanticModel)
	{
		// Try symbolic resolution first, preserves readable identifiers like float.Epsilon
		var resolved = TryResolveExpression(expression, semanticModel);
		if (resolved is not null)
		{
			return resolved;
		}

		// Fall back to constant folding, handles nameof(), some compiler-reduced expressions
		var constantValue = semanticModel.GetConstantValue(expression);
		if (constantValue.HasValue)
		{
			var typeInfo = semanticModel.GetTypeInfo(expression);
			return FormatConstantValue(constantValue.Value, typeInfo.Type);
		}

		return null;
	}

	static string? TryResolveExpression(ExpressionSyntax expression, SemanticModel semanticModel)
	{
		switch (expression)
		{
			case LiteralExpressionSyntax literal:
				return literal.Token.Text;

			case PrefixUnaryExpressionSyntax prefixUnary:
				var operand = TryResolveExpression(prefixUnary.Operand, semanticModel);
				if (operand is null)
				{
					return null;
				}
				return $"{prefixUnary.OperatorToken.Text}{operand}";

			case MemberAccessExpressionSyntax memberAccess:
				return TryResolveMemberAccess(memberAccess, semanticModel);

			case IdentifierNameSyntax identifier:
				return TryResolveIdentifier(identifier, semanticModel);

			case PredefinedTypeSyntax:
				// Keywords like int, double, string, valid as-is in generated code
				return expression.ToString();

			case CastExpressionSyntax castExpression:
				var innerExpr = TryResolveExpression(castExpression.Expression, semanticModel);
				if (innerExpr is null)
				{
					return null;
				}
				var castType = semanticModel.GetTypeInfo(castExpression.Type).Type;
				if (castType is null)
				{
					return null;
				}
				var qualifiedCastType = castType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
				return $"({qualifiedCastType}){innerExpr}";

			case ParenthesizedExpressionSyntax paren:
				var inner = TryResolveExpression(paren.Expression, semanticModel);
				return inner is not null ? $"({inner})" : null;

			case BinaryExpressionSyntax binary:
				var left = TryResolveExpression(binary.Left, semanticModel);
				var right = TryResolveExpression(binary.Right, semanticModel);
				if (left is null || right is null)
				{
					return null;
				}
				return $"{left} {binary.OperatorToken.Text} {right}";

			case DefaultExpressionSyntax defaultExpr:
				if (defaultExpr.Type is not null)
				{
					var defType = semanticModel.GetTypeInfo(defaultExpr.Type).Type;
					if (defType is not null)
					{
						return $"default({defType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})";
					}
				}
				return "default";

			case InvocationExpressionSyntax invocation:
				return TryResolveInvocation(invocation, semanticModel);

			case ObjectCreationExpressionSyntax objectCreation:
				var objectCreationType = semanticModel.GetTypeInfo(objectCreation).Type;
				if (objectCreationType is null || !objectCreationType.IsValueType)
				{
					return null;
				}
				return TryResolveObjectCreation(objectCreation, semanticModel);

			default:
				// Collection expressions, lambdas, etc., cannot resolve
				return null;
		}
	}

	static string? TryResolveMemberAccess(MemberAccessExpressionSyntax memberAccess, SemanticModel semanticModel)
	{
		var receiver = TryResolveExpression(memberAccess.Expression, semanticModel);
		if (receiver is null)
		{
			return null;
		}

		return $"{receiver}.{memberAccess.Name.Identifier.Text}";
	}

	static string? TryResolveIdentifier(IdentifierNameSyntax identifier, SemanticModel semanticModel)
	{
		var symbolInfo = semanticModel.GetSymbolInfo(identifier);
		var symbol = symbolInfo.Symbol;

		if (symbol is null)
		{
			return null;
		}

		// For type references (e.g. the "ValidationBehaviorDefaults" in "ValidationBehaviorDefaults.Flags")
		if (symbol is INamedTypeSymbol typeSymbol)
		{
			return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		}

		// For namespace references (e.g. "System" in "System.TimeSpan.Zero")
		if (symbol is INamespaceSymbol namespaceSymbol)
		{
			return namespaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		}

		// For static field/property references used as standalone identifiers
		if (symbol is IFieldSymbol { IsStatic: true } or IPropertySymbol { IsStatic: true })
		{
			var containingType = symbol.ContainingType;
			if (containingType is not null)
			{
				return $"{containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{symbol.Name}";
			}
		}

		return null;
	}

	static string? TryResolveInvocation(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
	{
		var methodExpr = TryResolveExpression(invocation.Expression, semanticModel);
		if (methodExpr is null)
		{
			return null;
		}

		// Resolve type arguments if present
		var symbolInfo = semanticModel.GetSymbolInfo(invocation);
		if (symbolInfo.Symbol is IMethodSymbol methodSymbol && methodSymbol.IsGenericMethod)
		{
			var typeArgs = string.Join(", ", methodSymbol.TypeArguments.Select(
				t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
			// Replace the method name with the generic version
			var lastDot = methodExpr.LastIndexOf('.');
			if (lastDot >= 0)
			{
				methodExpr = $"{methodExpr[..lastDot]}.{methodSymbol.Name}<{typeArgs}>";
			}
			else
			{
				methodExpr = $"{methodSymbol.Name}<{typeArgs}>";
			}
		}

		var args = new List<string>(invocation.ArgumentList.Arguments.Count);
		foreach (var arg in invocation.ArgumentList.Arguments)
		{
			var resolved = TryResolveExpression(arg.Expression, semanticModel);
			if (resolved is null)
			{
				return null;
			}
			args.Add(resolved);
		}

		return $"{methodExpr}({string.Join(", ", args)})";
	}

	static string? TryResolveObjectCreation(ObjectCreationExpressionSyntax objectCreation, SemanticModel semanticModel)
	{
		var typeInfo = semanticModel.GetTypeInfo(objectCreation);
		if (typeInfo.Type is null)
		{
			return null;
		}

		var qualifiedTypeName = typeInfo.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

		if (objectCreation.ArgumentList is null || objectCreation.ArgumentList.Arguments.Count is 0)
		{
			return $"new {qualifiedTypeName}()";
		}

		var args = new List<string>(objectCreation.ArgumentList.Arguments.Count);
		foreach (var arg in objectCreation.ArgumentList.Arguments)
		{
			var resolved = TryResolveExpression(arg.Expression, semanticModel);
			if (resolved is null)
			{
				return null;
			}
			args.Add(resolved);
		}

		return $"new {qualifiedTypeName}({string.Join(", ", args)})";
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
