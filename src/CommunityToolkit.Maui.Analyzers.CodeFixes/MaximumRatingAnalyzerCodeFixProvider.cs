// Ignore Spelling: analyzer

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace CommunityToolkit.Maui.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MaximumRatingAnalyzerCodeFixProvider)), Shared]
public class MaximumRatingAnalyzerCodeFixProvider : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => [MaximumRatingRangeAnalyzer.DiagnosticId];

	public sealed override FixAllProvider GetFixAllProvider()
	{
		return WellKnownFixAllProviders.BatchFixer;
	}

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root is null)
		{
			return;
		}

		Diagnostic? diagnostic = context.Diagnostics.FirstOrDefault();
		if (diagnostic is null)
		{
			return;
		}

		TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

		// Find the literal expression identified by the diagnostic.
		if (root.FindToken(diagnosticSpan.Start).Parent is not LiteralExpressionSyntax literalExpression)
		{
			return;
		}

		// Register a code action that will invoke the fix.
		context.RegisterCodeFix(
			CodeAction.Create(
				title: "Set MaximumRating to valid value, between 1 and 10",
				createChangedDocument: c => MaximumRatingToValidValueAsync(context.Document, literalExpression, c),
				equivalenceKey: nameof(MaximumRatingAnalyzerCodeFixProvider)),
			diagnostic);
	}

	static async Task<Document> MaximumRatingToValidValueAsync(Document document, LiteralExpressionSyntax literalExpression, CancellationToken cancellationToken)
	{
		// Get the original value from the literal expression.
		if (!int.TryParse(literalExpression.Token.ValueText, out int originalValue))
		{
			originalValue = 1; // Fallback value if parsing fails.
		}

		int newValue = GetValidRatingValue(originalValue);
		// Create a new literal expression with the valid rating value.
		LiteralExpressionSyntax newLiteralExpression = LiteralExpression(
			SyntaxKind.NumericLiteralExpression,
			Literal(newValue));

		SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root is null)
		{
			return document;
		}

		// Replace the old literal with the new one.
		SyntaxNode newRoot = root.ReplaceNode(literalExpression, newLiteralExpression);

		return document.WithSyntaxRoot(newRoot);
	}

	static int GetValidRatingValue(int value)
	{
		return value switch
		{
			< 1 => 1,
			> 10 => 10,
			_ => value
		};
	}
}
