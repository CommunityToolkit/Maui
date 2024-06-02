using System.Collections.Immutable;
using System.Composition;
using CommunityToolkit.Maui.CameraView.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace CommunityToolkit.Maui.Camera.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseCommunityToolkitCameraViewInitializationAnalyzerCodeFixProvider)), Shared]
public class UseCommunityToolkitCameraViewInitializationAnalyzerCodeFixProvider : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(UseCommunityToolkitCameraInitializationAnalyzer.DiagnosticId);

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

		var diagnostic = context.Diagnostics.First();
		var diagnosticSpan = diagnostic.Location.SourceSpan;

		// Find the type declaration identified by the diagnostic.
		var declaration = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().Last() ?? throw new InvalidOperationException();

		// Register a code action that will invoke the fix.
		context.RegisterCodeFix(
			CodeAction.Create(
				title: CodeFixResources.Initialize__NET_MAUI_Community_Toolkit_CameraView_Before_UseMauiApp,
				createChangedDocument: c => AddUseCommunityToolkit(context.Document, declaration, c),
				equivalenceKey: nameof(CodeFixResources.Initialize__NET_MAUI_Community_Toolkit_CameraView_Before_UseMauiApp)),
			diagnostic);
	}

	static async Task<Document> AddUseCommunityToolkit(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root is null)
		{
			return document;
		}

		var updatedInvocationExpression =
			InvocationExpression(
				MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression, invocationExpression, IdentifierName("UseMauiCommunityToolkitCamera")));

		var mauiCommunityToolkitUsingStatement =
			UsingDirective(
				QualifiedName(
					IdentifierName("CommunityToolkit"),
					IdentifierName("Maui")));

		var newRoot = root.ReplaceNode(invocationExpression, updatedInvocationExpression);

		if (newRoot is CompilationUnitSyntax compilationSyntax)
		{
			newRoot = compilationSyntax.AddUsings(mauiCommunityToolkitUsingStatement).NormalizeWhitespace();
		}

		var newDocument = document.WithSyntaxRoot(newRoot);

		return newDocument;
	}
}