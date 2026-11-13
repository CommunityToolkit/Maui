using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace CommunityToolkit.Maui.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseCommunityToolkitInitializationAnalyzerCodeFixProvider)), Shared]
public class UseCommunityToolkitInitializationAnalyzerCodeFixProvider : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = [UseCommunityToolkitInitializationAnalyzer.DiagnosticId];

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

		var diagnostic = context.Diagnostics.First();
		var diagnosticSpan = diagnostic.Location.SourceSpan;

		// Find the type declaration identified by the diagnostic.
		var declaration = root?.FindNode(diagnosticSpan, getInnermostNodeForTie: true) as InvocationExpressionSyntax ?? throw new InvalidOperationException();

		// Register a code action that will invoke the fix.
		context.RegisterCodeFix(
			CodeAction.Create(
				title: CodeFixResources.Initialize__NET_MAUI_Community_Toolkit_Before_UseMauiApp,
				createChangedDocument: c => AddUseCommunityToolkit(context.Document, declaration, c),
				equivalenceKey: nameof(CodeFixResources.Initialize__NET_MAUI_Community_Toolkit_Before_UseMauiApp)),
			diagnostic);
	}

	static async Task<Document> AddUseCommunityToolkit(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root is null)
		{
			return document;
		}

		// Don't call NormalizeWhitespace(): it reformats the user's entire document (https://github.com/CommunityToolkit/Maui/issues/1720)
		var endOfLine = root.DescendantTrivia().FirstOrDefault(static trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia));
		if (endOfLine.IsKind(SyntaxKind.None))
		{
			endOfLine = ElasticCarriageReturnLineFeed;
		}

		var newRoot = root.ReplaceNode(invocationExpression, CreateUseMauiCommunityToolkitInvocation(invocationExpression, endOfLine));

		if (newRoot is CompilationUnitSyntax compilationSyntax)
		{
			var mauiCommunityToolkitUsingDirective = UsingDirective(ParseName("CommunityToolkit.Maui"))
				.WithUsingKeyword(Token(TriviaList(), SyntaxKind.UsingKeyword, TriviaList(Space)))
				.WithSemicolonToken(Token(TriviaList(), SyntaxKind.SemicolonToken, compilationSyntax.Usings.Count is 0 ? TriviaList(endOfLine, endOfLine) : TriviaList(endOfLine)));

			newRoot = compilationSyntax.AddUsings(mauiCommunityToolkitUsingDirective);
		}

		return document.WithSyntaxRoot(newRoot);
	}

	static InvocationExpressionSyntax CreateUseMauiCommunityToolkitInvocation(InvocationExpressionSyntax useMauiAppInvocation, SyntaxTrivia endOfLine)
	{
		var expression = useMauiAppInvocation.WithoutTrailingTrivia();
		var dotTokenLeadingTrivia = TriviaList();
		var trailingTrivia = useMauiAppInvocation.GetTrailingTrivia();

		// Follow the user's style when the builder chain has one call per line, e.g. `builder\n\t.UseMauiApp<App>()\n\t.ConfigureFonts(...)` in the .NET MAUI template
		if (useMauiAppInvocation.Parent is MemberAccessExpressionSyntax { OperatorToken: var nextDotToken }
			&& trailingTrivia.Any(SyntaxKind.EndOfLineTrivia))
		{
			var indentation = nextDotToken.LeadingTrivia.LastOrDefault();

			expression = useMauiAppInvocation;
			dotTokenLeadingTrivia = indentation.IsKind(SyntaxKind.WhitespaceTrivia) ? TriviaList(indentation) : TriviaList();
			trailingTrivia = TriviaList(endOfLine);
		}

		// Every token gets explicit trivia because the elastic trivia added by SyntaxFactory lets the code action's formatter re-indent the surrounding code
		return InvocationExpression(
			MemberAccessExpression(
				SyntaxKind.SimpleMemberAccessExpression,
				expression,
				Token(dotTokenLeadingTrivia, SyntaxKind.DotToken, TriviaList()),
				IdentifierName(Identifier(TriviaList(), "UseMauiCommunityToolkit", TriviaList()))),
			ArgumentList(
				Token(TriviaList(), SyntaxKind.OpenParenToken, TriviaList()),
				SeparatedList<ArgumentSyntax>(),
				Token(TriviaList(), SyntaxKind.CloseParenToken, trailingTrivia)));
	}
}