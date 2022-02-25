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
}