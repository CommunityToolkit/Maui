namespace CommunityToolkit.Maui.Analyzers;

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

public class MaximumRatingCodeFixProvider : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => [MaximumRatingRangeAnalyzer.DiagnosticId];

	public sealed override FixAllProvider GetFixAllProvider()
	{
		return WellKnownFixAllProviders.BatchFixer;
	}

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		Diagnostic diagnostic = context.Diagnostics[0];
		TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;
		SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		LiteralExpressionSyntax? node = root?.FindNode(diagnosticSpan) as LiteralExpressionSyntax;
		if (root is not null && node?.Token.Value is not null)
		{
			int validValue = GetValidRatingValue((int)node.Token.Value);

			LiteralExpressionSyntax newLiteral = SyntaxFactory.LiteralExpression(
				SyntaxKind.NumericLiteralExpression,
				SyntaxFactory.Literal(validValue));

			SyntaxNode newRoot = root.ReplaceNode(node, newLiteral);
			Document newDocument = context.Document.WithSyntaxRoot(newRoot);
			context.RegisterCodeFix(
				CodeAction.Create(
					title: $"Set MaximumRating to {validValue}, between 1 and 10",
					createChangedDocument: _ => Task.FromResult(newDocument),
					equivalenceKey: nameof(MaximumRatingCodeFixProvider)),
				diagnostic);
		}
	}

	static int GetValidRatingValue(int value)
	{
		return value < 1 ? 1 : value > 10 ? 10 : value;
	}
}
