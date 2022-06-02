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
		type.Name + 
		(type.NullableAnnotation == NullableAnnotation.Annotated ? "?" : string.Empty) +
		type.TypeArguments.GetGenericTypeArgumentsString();

	public static string GetGenericTypeArgumentsString(this IEnumerable<ITypeSymbol> typeArguments)
	{
		string result = string.Empty;

		if (!typeArguments.Any())
		{
			return result;
		}

		result += "<";

		bool isFirstArgument = true;
		foreach (ITypeSymbol typeArg in typeArguments)
		{
			if (isFirstArgument)
			{
				isFirstArgument = false;
			}
			else
			{
				result += ", ";
			}

			switch (typeArg)
			{
				case ITypeParameterSymbol typeParameterSymbol:
					result += typeParameterSymbol.Name;
					break;
				case INamedTypeSymbol namedTypeSymbol:
					result += namedTypeSymbol.GetFullTypeString();
					break;
				default:
					//Maybe should handle this somehow, maybe inject SourceProductionContext and call ReportDiagnostic on it?
					break;
			}

		}

		result += ">";

		return result;
	}

	public static string GetWhereStatement(this INamedTypeSymbol type)
	{
		string result = string.Empty;
		if (!type.TypeParameters.Any())
		{
			return result;
		}

		foreach (var typeParameterSymbol in type.TypeParameters)
		{
			string constraints = "";

			bool isFirstConstraint = true;

			if (typeParameterSymbol.HasNotNullConstraint)
			{
				constraints += "notnull";

				isFirstConstraint = false;
			}
			else if (typeParameterSymbol.HasUnmanagedTypeConstraint)
			{
				constraints += "unmanaged";

				isFirstConstraint = false;
			}
			else if (typeParameterSymbol.HasReferenceTypeConstraint)
			{
				constraints += "class";

				isFirstConstraint = false;
			}
			else if (typeParameterSymbol.HasValueTypeConstraint)
			{
				constraints += "struct";

				isFirstConstraint = false;
			}

			foreach (INamedTypeSymbol contstraintType in typeParameterSymbol.ConstraintTypes)
			{
				if (!isFirstConstraint)
				{
					constraints += ", ";
				}
				else
				{
					isFirstConstraint = false;
				}

				constraints += contstraintType.GetFullTypeString();
			}

			if (typeParameterSymbol.HasConstructorConstraint)
			{
				if (!isFirstConstraint)
				{
					constraints += ", ";
				}
				constraints += "new()";
			}

			if (!string.IsNullOrEmpty(constraints))
			{
				result += "where " + typeParameterSymbol.Name + " : " + constraints + @"
";
			}
		}

		return result;
	}
}
