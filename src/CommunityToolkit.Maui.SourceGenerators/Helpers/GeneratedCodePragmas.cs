using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;

namespace CommunityToolkit.Maui.SourceGenerators.Helpers;

static class GeneratedCodePragmas
{
	// Generated code names the user's members and types, which the user may deprecate, hide or mark experimental, and cannot annotate generated members; IL codes are never listed.
	// CS9258: the generated getter means the `field` keyword even when the user's class has a member named `field`.
	public const string BindablePropertyWarnings = "CS0108, CS0109, CS0612, CS0618, CS9258";

	public const string AttachedBindablePropertyWarnings = "CS0108, CS0109, CS0612, CS0618";

	public const string TextColorToWarnings = "CS0612, CS0618";

	public static string Create(string fixedWarnings, IEnumerable<string> userDiagnosticIds)
	{
		var fixedWarningSet = new HashSet<string>(fixedWarnings.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);

		var suppressedUserIds = userDiagnosticIds
			.Where(id => IsValidPragmaToken(id) && !IsTrimOrAotWarning(id) && !fixedWarningSet.Contains(id))
			.Distinct(StringComparer.Ordinal)
			.OrderBy(static id => id, StringComparer.Ordinal);

		return $"#pragma warning disable {string.Join(", ", new[] { fixedWarnings }.Concat(suppressedUserIds))}";
	}

	public static bool IsValidPragmaToken(string id)
	{
		if (id.Length is 0 || !SyntaxFacts.IsIdentifierStartCharacter(id[0]))
		{
			return false;
		}

		// The C# lexer drops format characters (such as U+200B) from pragma IDs, so "IL\u200B2026" would disable IL2026.
		return id.All(static character => SyntaxFacts.IsIdentifierPartCharacter(character) && CharUnicodeInfo.GetUnicodeCategory(character) is not UnicodeCategory.Format);
	}

	public static bool IsTrimOrAotWarning(string id) => id.Length > 2 && id.StartsWith("IL", StringComparison.Ordinal) && id.Skip(2).All(static character => character is >= '0' and <= '9');
}