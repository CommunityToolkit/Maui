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

	/// <summary>
	/// Find all generic type constraints for <see cref="INamedTypeSymbol"/> and return the results as a complete string
	/// </summary>
	/// <param name="type"></param>
	/// <returns>A text string containing all Generic Type Constraints for <see cref="INamedTypeSymbol"/>, eg "where T : ITextStyle, notnull". Returns <see cref="string.Empty"/> if no generic type constrants found</returns>
	public static string GetGenericTypeConstraintsAsString(this INamedTypeSymbol type)
	{
		if (!type.TypeParameters.Any())
		{
			return string.Empty;
		}

		StringBuilder result = new();
		StringBuilder constraints = new();

		foreach (var typeParameterSymbol in type.TypeParameters)
		{
			if (typeParameterSymbol.HasNotNullConstraint)
			{
				constraints.Append("notnull");
			}
			else if (typeParameterSymbol.HasUnmanagedTypeConstraint)
			{
				constraints.Append("unmanaged");
			}
			else if (typeParameterSymbol.HasReferenceTypeConstraint)
			{
				constraints.Append("class");

				if (typeParameterSymbol.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated)
				{
					constraints.Append("?");
				}
			}
			else if (typeParameterSymbol.HasValueTypeConstraint)
			{
				constraints.Append("struct");
			}

			foreach (INamedTypeSymbol contstraintType in typeParameterSymbol.ConstraintTypes.Cast<INamedTypeSymbol>())
			{
				if (constraints.Length > 0)
				{
					constraints.Append(", ");
				}

				var symbolDisplayFormat = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
																	miscellaneousOptions: SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
				constraints.Append(contstraintType.ToDisplayString(symbolDisplayFormat));
			}

			if (typeParameterSymbol.HasConstructorConstraint)
			{
				if (constraints.Length > 0)
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

	public static bool ContainsSymbolBaseType(this IEnumerable<INamedTypeSymbol> namedSymbolList, INamedTypeSymbol symbol)
	{
		INamedTypeSymbol? baseType = symbol.BaseType;

		while (baseType is not null)
		{
			var doesListContainBaseType = namedSymbolList.Any(x => x.Equals(baseType, SymbolEqualityComparer.Default));

			if (doesListContainBaseType)
			{
				return true;
			}

			baseType = baseType.BaseType;
		}

		return false;
	}
}