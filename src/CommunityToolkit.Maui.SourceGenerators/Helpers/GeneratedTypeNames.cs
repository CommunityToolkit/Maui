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

	// Hint names cannot contain the angle brackets of a containing type's type parameters.
	public static string ToHintNameSegment(string typeName) =>
		new(typeName.Select(static character => char.IsLetterOrDigit(character) || character is '_' or '.' ? character : '_').ToArray());

	static string GetKeyword(INamedTypeSymbol type) => type.TypeKind switch
	{
		TypeKind.Struct => type.IsRecord ? "record struct" : "struct",
		TypeKind.Interface => "interface",
		_ => type.IsRecord ? "record" : "class",
	};
}