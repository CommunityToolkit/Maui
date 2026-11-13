using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CommunityToolkit.Maui.SourceGenerators.Helpers;

/// <summary>
/// Collects the diagnostic IDs that the user attached to the symbols a generator names in its output.
/// </summary>
sealed class UserDiagnosticIdCollector
{
	const string experimentalAttributeName = "System.Diagnostics.CodeAnalysis.ExperimentalAttribute";
	const string obsoleteAttributeName = "System.ObsoleteAttribute";
	const string obsoleteDiagnosticIdArgumentName = "DiagnosticId";

	readonly HashSet<string> diagnosticIds = new(StringComparer.Ordinal);
	readonly HashSet<ISymbol> visitedSymbols = new(SymbolEqualityComparer.Default);

	public EquatableArray<string> ToEquatableArray()
	{
		var sortedIds = diagnosticIds.ToArray();
		Array.Sort(sortedIds, StringComparer.Ordinal);
		return ImmutableArray.Create(sortedIds).AsEquatableArray();
	}

	public void AddSymbolAndContainers(ISymbol? symbol)
	{
		if (symbol is null || !visitedSymbols.Add(symbol))
		{
			return;
		}

		for (var current = symbol; current is not null; current = current.ContainingSymbol)
		{
			AddAttributes(current);
		}

		AddAttributes(symbol.ContainingModule);
		AddAttributes(symbol.ContainingAssembly);
	}

	public void AddType(ITypeSymbol? type)
	{
		switch (type)
		{
			case null:
				return;
			case IArrayTypeSymbol arrayType:
				AddType(arrayType.ElementType);
				return;
			case IPointerTypeSymbol pointerType:
				AddType(pointerType.PointedAtType);
				return;
			case ITypeParameterSymbol typeParameter:
				foreach (var constraintType in typeParameter.ConstraintTypes)
				{
					AddType(constraintType);
				}
				return;
			case INamedTypeSymbol namedType:
				AddSymbolAndContainers(namedType.OriginalDefinition);
				AddType(namedType.ContainingType);
				foreach (var typeArgument in namedType.TypeArguments)
				{
					AddType(typeArgument);
				}
				return;
		}
	}

	public void AddTypeParameterConstraints(INamedTypeSymbol type)
	{
		foreach (var typeParameter in type.TypeParameters)
		{
			AddType(typeParameter);
		}
	}

	public void AddMethodsNamed(INamedTypeSymbol declaringType, string methodName)
	{
		if (methodName is "null" or "")
		{
			return;
		}

		for (INamedTypeSymbol? current = declaringType; current is not null; current = current.BaseType)
		{
			foreach (var member in current.GetMembers(methodName))
			{
				AddSymbolAndContainers(member);
			}
		}
	}

	public void AddSymbolsReferencedBy(ExpressionSyntax expression, SemanticModel semanticModel)
	{
		foreach (var node in expression.DescendantNodesAndSelf())
		{
			var symbolInfo = semanticModel.GetSymbolInfo(node);
			AddSymbolAndContainers(symbolInfo.Symbol);
			AddType(semanticModel.GetTypeInfo(node).Type);
		}
	}

	public void AddTypesReferencedBy(TypedConstant constant)
	{
		switch (constant.Kind)
		{
			case TypedConstantKind.Array:
				foreach (var value in constant.Values)
				{
					AddTypesReferencedBy(value);
				}
				break;
			case TypedConstantKind.Type:
				AddType(constant.Value as ITypeSymbol);
				break;
			case TypedConstantKind.Enum:
				AddType(constant.Type);
				break;
		}
	}

	void AddAttributes(ISymbol? symbol)
	{
		if (symbol is null)
		{
			return;
		}

		foreach (var attribute in symbol.GetAttributes())
		{
			var attributeName = attribute.AttributeClass?.ToDisplayString();

			if (attributeName == experimentalAttributeName)
			{
				AddIfString(attribute.ConstructorArguments.FirstOrDefault());
			}
			else if (attributeName == obsoleteAttributeName)
			{
				foreach (var namedArgument in attribute.NamedArguments)
				{
					if (namedArgument.Key == obsoleteDiagnosticIdArgumentName)
					{
						AddIfString(namedArgument.Value);
					}
				}
			}
		}
	}

	void AddIfString(TypedConstant constant)
	{
		if (constant.Value is string diagnosticId && diagnosticId.Length > 0)
		{
			diagnosticIds.Add(diagnosticId);
		}
	}
}