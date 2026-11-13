using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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

	// The exact name of a type: the namespace tells same-named types apart, '+' nests like metadata names and '-' (never part of an identifier) marks the number of type parameters.
	public static string GetTypeIdentity(INamedTypeSymbol type)
	{
		var segments = new List<string>();
		for (var current = type; current is not null; current = current.ContainingType)
		{
			segments.Insert(0, current.Arity is 0 ? current.Name : $"{current.Name}-{current.Arity}");
		}

		var namespacePrefix = type.ContainingNamespace is null || type.ContainingNamespace.IsGlobalNamespace ? string.Empty : $"{type.ContainingNamespace.ToDisplayString()}.";
		return namespacePrefix + string.Join("+", segments);
	}

	// Roslyn compares hint names without regard to case, so Foo and foo (or two identities that sanitize alike) need a suffix; only these get one, which keeps ordinary names readable.
	public static IReadOnlyDictionary<string, string> GetUniqueHintNames(IEnumerable<string> typeIdentities)
	{
		var sanitizedByIdentity = typeIdentities.Distinct(StringComparer.Ordinal).ToDictionary(static identity => identity, GetSanitizedHintName, StringComparer.Ordinal);
		var collidingNames = new HashSet<string>(
			sanitizedByIdentity.Values
				.GroupBy(static name => name, StringComparer.OrdinalIgnoreCase)
				.Where(static group => group.Count() > 1)
				.Select(static group => group.Key),
			StringComparer.OrdinalIgnoreCase);

		return sanitizedByIdentity.ToDictionary(
			static pair => pair.Key,
			pair => collidingNames.Contains(pair.Value) ? $"{pair.Value}-{GetStableHash(pair.Key)}" : pair.Value,
			StringComparer.Ordinal);
	}

	static string GetSanitizedHintName(string typeIdentity) =>
		new(typeIdentity.Select(static character => SyntaxFacts.IsIdentifierPartCharacter(character) || character is '.' or '+' or '-' ? character : '_').ToArray());

	// FNV-1a, because string.GetHashCode is randomized per process and generated names must not change between builds.
	public static string GetStableHash(string value)
	{
		const uint offsetBasis = 2166136261;
		const uint prime = 16777619;

		var hash = offsetBasis;
		foreach (var character in value)
		{
			hash = (hash ^ character) * prime;
		}

		return hash.ToString("x8", CultureInfo.InvariantCulture);
	}

	static string GetKeyword(INamedTypeSymbol type) => type.TypeKind switch
	{
		TypeKind.Struct => type.IsRecord ? "record struct" : "struct",
		TypeKind.Interface => "interface",
		_ => type.IsRecord ? "record" : "class",
	};
}