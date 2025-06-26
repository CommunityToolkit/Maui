using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommunityToolkit.Maui.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UseCommunityToolkitInitializationAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "MCT001";
	const string category = "Initialization";
	const string useMauiAppMethodName = "UseMauiApp";
	const string useMauiCommunityToolkitMethodName = "UseMauiCommunityToolkit";

	static readonly LocalizableString title = new LocalizableResourceString(nameof(Resources.InitializationErrorTitle), Resources.ResourceManager, typeof(Resources));
	static readonly LocalizableString messageFormat = new LocalizableResourceString(nameof(Resources.InitalizationMessageFormat), Resources.ResourceManager, typeof(Resources));
	static readonly LocalizableString description = new LocalizableResourceString(nameof(Resources.InitializationErrorMessage), Resources.ResourceManager, typeof(Resources));
	static readonly DiagnosticDescriptor rule = new(DiagnosticId, title, messageFormat, category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: description);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [rule];

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
	}

	static void AnalyzeNode(SyntaxNodeAnalysisContext context)
	{
		if(context.Node is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: useMauiAppMethodName } } invocationExpression)
		{
			var methodDeclaration = invocationExpression
				.Ancestors()
				.OfType<MethodDeclarationSyntax>()
				.FirstOrDefault();

			if(methodDeclaration is not null && !HasUseMauiCommunityToolkitCall(methodDeclaration))
			{
				var diagnostic = Diagnostic.Create(rule, invocationExpression.GetLocation());
				context.ReportDiagnostic(diagnostic);
			}
		}
	}

	static bool HasUseMauiCommunityToolkitCall(MethodDeclarationSyntax methodDeclaration)
	{
		// Use a visitor to traverse the syntax tree efficiently
		var visitor = new UseMauiCommunityToolkitVisitor();
		visitor.Visit(methodDeclaration);
		return visitor.Found;
	}

	class UseMauiCommunityToolkitVisitor : CSharpSyntaxWalker
	{
		public bool Found { get; private set; }

		public UseMauiCommunityToolkitVisitor() : base(SyntaxWalkerDepth.StructuredTrivia)
		{
			// Ensure we visit trivia (including preprocessor directives)
		}

		public override void VisitInvocationExpression(InvocationExpressionSyntax node)
		{
			if(Found)
			{
				return; // Early exit if already found
			}

			// Check if this is a call to UseMauiCommunityToolkit
			if(node.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Name.Identifier.ValueText == useMauiCommunityToolkitMethodName)
			{
				Found = true;
				return;
			}

			base.VisitInvocationExpression(node);
		}
	}
}