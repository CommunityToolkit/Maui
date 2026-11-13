using Microsoft.CodeAnalysis;

namespace CommunityToolkit.Maui.SourceGenerators.Helpers;

static class GeneratedTypeNames
{
	public static string GetNameWithTypeParameters(INamedTypeSymbol type) =>
		type.TypeParameters.IsEmpty ? type.Name : $"{type.Name}<{string.Join(", ", type.TypeParameters.Select(static typeParameter => typeParameter.Name))}>";

	public static string GetContainingTypeKeywords(INamedTypeSymbol type)
	{
		var keywords = new List<string>();
		for (var current = type.ContainingType; current is not null; current = current.ContainingType)
		{
			keywords.Insert(0, GetKeyword(current));
		}

		return string.Join("|", keywords);
	}

	// Hint names must be unique per generator: the namespace tells same-named types apart, '+' nests like metadata names and '-' (never part of an identifier) marks the number of type parameters.
	public static string GetHintName(INamedTypeSymbol type)
	{
		var segments = new List<string>();
		for (var current = type; current is not null; current = current.ContainingType)
		{
			segments.Insert(0, current.Arity is 0 ? current.Name : $"{current.Name}-{current.Arity}");
		}

		var namespacePrefix = type.ContainingNamespace is null || type.ContainingNamespace.IsGlobalNamespace ? string.Empty : $"{type.ContainingNamespace.ToDisplayString()}.";
		var hintName = namespacePrefix + string.Join("+", segments);

		return new(hintName.Select(static character => char.IsLetterOrDigit(character) || character is '_' or '.' or '+' or '-' ? character : '_').ToArray());
	}

	static string GetKeyword(INamedTypeSymbol type) => type.TypeKind switch
	{
		TypeKind.Struct => type.IsRecord ? "record struct" : "struct",
		TypeKind.Interface => "interface",
		_ => type.IsRecord ? "record" : "class",
	};
}