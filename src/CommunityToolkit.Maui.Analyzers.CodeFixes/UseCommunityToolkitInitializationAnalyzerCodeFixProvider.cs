using System.Collections.Immutable;
using System.Composition;
using System.Linq.Expressions;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.ComTypes;
using CommunityToolkit.Maui.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;

namespace CommunityToolkit.Maui.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseCommunityToolkitInitializationAnalyzerCodeFixProvider)), Shared]
public class UseCommunityToolkitInitializationAnalyzerCodeFixProvider : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(UseCommunityToolkitInitializationAnalyzer.DiagnosticId);

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

		// TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
		var diagnostic = context.Diagnostics.First();
		var diagnosticSpan = diagnostic.Location.SourceSpan;

		// Find the type declaration identified by the diagnostic.
		var declaration = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First() ?? throw new InvalidOperationException();

		// Register a code action that will invoke the fix.
		context.RegisterCodeFix(
			CodeAction.Create(
				title: CodeFixResources.AddUseCommunityToolkit,
				createChangedDocument: c => AddUseCommunityToolkit(context.Document, declaration, c),
				equivalenceKey: nameof(CodeFixResources.AddUseCommunityToolkit)),
			diagnostic);
	}

	async Task<Document> AddUseCommunityToolkit(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
	{
		var updatedInvocationExpression =
			SyntaxFactory.InvocationExpression(
				SyntaxFactory.MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression, invocationExpression, SyntaxFactory.IdentifierName("UseMauiCommunityToolkit")));	

		var root = await document.GetSyntaxRootAsync();
		if (root is null)
		{
			return document;
		}

		var newRoot = root.ReplaceNode(invocationExpression, updatedInvocationExpression);

		var newDocument = document.WithSyntaxRoot(newRoot);

		return newDocument;

	}
}
