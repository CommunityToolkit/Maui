using System.Text;
using Microsoft.CodeAnalysis;

namespace CommunityToolkit.Maui.SourceGenerators.Extensions;

static class NamespaceSymbolExtensions
{
	public static IEnumerable<INamedTypeSymbol> GetNamedTypeSymbols(this INamespaceSymbol namespaceSymbol)
	{
		var stack = new Stack<INamespaceSymbol>();
		stack.Push(namespaceSymbol);

		while (stack.Count > 0)
		{
			var namespaceSymbolFromStack = stack.Pop();

			foreach (var member in namespaceSymbolFromStack.GetMembers())
			{
				if (member is INamespaceSymbol memberAsNamespace)
				{
					stack.Push(memberAsNamespace);
				}
				else if (member is INamedTypeSymbol memberAsNamedTypeSymbol)
				{
					yield return memberAsNamedTypeSymbol;
				}
			}
		}
	}
	
	public static string GetFullTypeString(this INamedTypeSymbol type) => 
		$"{type.Name}{(type.NullableAnnotation == NullableAnnotation.Annotated ? "?" : string.Empty)}{type.TypeArguments.GetGenericTypeArgumentsString()}";

	public static string GetGenericTypeArgumentsString(this IEnumerable<ITypeSymbol> typeArguments)
	{
		if (!typeArguments.Any())
		{
			return string.Empty;
		}

		StringBuilder result = new StringBuilder("<");
		
		bool isFirstArgument = true;
		foreach (ITypeSymbol typeArg in typeArguments)
		{
			if (isFirstArgument)
			{
				isFirstArgument = false;
			}
			else
			{
				result.Append(", ");
			}

			switch (typeArg)
			{
				case ITypeParameterSymbol typeParameterSymbol:
					result.Append(typeParameterSymbol.Name);
					break;
				case INamedTypeSymbol namedTypeSymbol:
					result.Append(namedTypeSymbol.GetFullTypeString());
					break;
				default:
					//Maybe should handle this somehow, maybe inject SourceProductionContext and call ReportDiagnostic on it?
					break;
			}

		}

		result.Append(">");

		return result.ToString();
	}

	public static string GetWhereStatement(this INamedTypeSymbol type)
	{
		
		if (!type.TypeParameters.Any())
		{
			return string.Empty;
		}

		StringBuilder result = new StringBuilder();
		StringBuilder constraints = new StringBuilder();
		foreach (var typeParameterSymbol in type.TypeParameters)
		{
			
			bool isFirstConstraint = true;
			
			if (typeParameterSymbol.HasNotNullConstraint)
			{
				constraints.Append("notnull");

				isFirstConstraint = false;
			}
			else if (typeParameterSymbol.HasUnmanagedTypeConstraint)
			{
				constraints.Append("unmanaged");

				isFirstConstraint = false;
			}
			else if (typeParameterSymbol.HasReferenceTypeConstraint)
			{
				constraints.Append("class");

				if (typeParameterSymbol.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated)
				{
					constraints.Append("?");
				}

				isFirstConstraint = false;
			}
			else if (typeParameterSymbol.HasValueTypeConstraint)
			{
				constraints.Append("struct");

				isFirstConstraint = false;
			}

			foreach (INamedTypeSymbol contstraintType in typeParameterSymbol.ConstraintTypes)
			{
				if (!isFirstConstraint)
				{
					constraints.Append(", ");
				}
				else
				{
					isFirstConstraint = false;
				}

				constraints.Append(contstraintType.GetFullTypeString());
			}

			if (typeParameterSymbol.HasConstructorConstraint)
			{
				if (!isFirstConstraint)
				{
					constraints.Append(", ");
				}
				constraints.Append("new()");
			}

			if (constraints.Length > 0)
			{
				result.Append($"where " + typeParameterSymbol.Name + " : " + constraints + @"
"); 
				constraints.Clear();
			}
		}

		return result.ToString();
	}
	
}
