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

	public static bool ContainsSymbolBaseType(this IEnumerable<INamedTypeSymbol> namedSymbolList, INamedTypeSymbol symbol)
	{
		INamedTypeSymbol? baseType = symbol.BaseType;

		while (baseType is not null)
		{
			var result = namedSymbolList.Any(x => x.Equals(baseType, SymbolEqualityComparer.Default));

			if (result)
			{
				return true;
			}

			baseType = baseType.BaseType;
		}

		return false;
	}
}