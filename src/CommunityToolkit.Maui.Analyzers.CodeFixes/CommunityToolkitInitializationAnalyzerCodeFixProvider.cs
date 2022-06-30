using System.Collections.Immutable;
using System.Composition;
using CommunityToolkit.Maui.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;

namespace CommunityToolkit.Maui.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CommunityToolkitInitializationAnalyzerCodeFixProvider)), Shared]
public class CommunityToolkitInitializationAnalyzerCodeFixProvider : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(CommunityToolkitInitializationAnalyzer.DiagnosticId);

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

		// TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
		var diagnostic = context.Diagnostics.First();
		var diagnosticSpan = diagnostic.Location.SourceSpan;

		// Find the type declaration identified by the diagnostic.
		var declaration = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First() ?? throw new InvalidOperationException();

		// Register a code action that will invoke the fix.
		context.RegisterCodeFix(
			CodeAction.Create(
				title: CodeFixResources.AddUseCommunityToolkit,
				createChangedSolution: c => AddUseCommunityToolkit(context.Document, declaration, c),
				equivalenceKey: nameof(CodeFixResources.AddUseCommunityToolkit)),
			diagnostic);
	}

	async Task<Solution> AddUseCommunityToolkit(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
	{
		// Compute new uppercase name.
		var identifierToken = typeDecl.Identifier;
		var newText = identifierToken.Text.Replace(";", ".UseMauiCommunityToolkit();");

		// Get the symbol representing the type to be renamed.
		var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
		var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken) ?? throw new InvalidOperationException();

		// Produce a new solution that has all references to that type renamed, including the declaration.
		var originalSolution = document.Project.Solution;
		var optionSet = originalSolution.Workspace.Options;
		var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, newText, optionSet, cancellationToken).ConfigureAwait(false);

		// Return the new solution with the now-uppercase type name.
		return newSolution;
	}
}
