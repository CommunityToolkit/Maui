using System;
using Microsoft.CodeAnalysis;

namespace CommunityToolkit.Maui.SourceGenerators.Extensions;

static class NamedTypeSymbolExtensions
{
	public static bool ImplementsInterfaceOrBaseClass(this INamedTypeSymbol typeSymbol, in string typeToCheck)
	{
		if (typeSymbol is null)
		{
			return false;
		}

		if (typeSymbol.MetadataName == typeToCheck)
		{
			return true;
		}

		if (typeSymbol.BaseType?.MetadataName == typeToCheck)
		{
			return true;
		}

		foreach (var interfaceTypeSymbol in typeSymbol.AllInterfaces)
		{
			if (interfaceTypeSymbol.MetadataName == typeToCheck)
			{
				return true;
			}
		}

		return false;
	}
}