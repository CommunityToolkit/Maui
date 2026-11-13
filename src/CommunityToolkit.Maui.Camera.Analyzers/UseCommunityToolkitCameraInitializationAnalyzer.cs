using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommunityToolkit.Maui.Camera.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UseCommunityToolkitCameraInitializationAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "MCTC001";

	const string category = "Initialization";
	const string useMauiAppMethodName = "UseMauiApp";
	const string useMauiCommunityToolkitCameraMethodName = "UseMauiCommunityToolkitCamera";

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
		if (context.Node is InvocationExpressionSyntax invocationExpression
			&& invocationExpression.Expression is MemberAccessExpressionSyntax memberAccessExpression
			&& memberAccessExpression.Name.Identifier.ValueText == useMauiAppMethodName)
		{
			var methodDeclaration = invocationExpression
				.Ancestors()
				.OfType<MethodDeclarationSyntax>()
				.FirstOrDefault();

			if (methodDeclaration is not null && !HasUseMauiCommunityToolkitCameraCall(methodDeclaration))
			{
				var diagnostic = Diagnostic.Create(rule, invocationExpression.GetLocation());
				context.ReportDiagnostic(diagnostic);
			}
		}
	}

	static bool HasUseMauiCommunityToolkitCameraCall(MethodDeclarationSyntax methodDeclaration)
	{
		// Check syntax nodes first (handles active code)
		var hasInSyntaxTree = methodDeclaration
			.DescendantNodes()
			.OfType<InvocationExpressionSyntax>()
			.Any(static invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess
				&& memberAccess.Name.Identifier.ValueText == useMauiCommunityToolkitCameraMethodName);

		if (hasInSyntaxTree)
		{
			return true;
		}

		// Check trivia (comments, preprocessor directives, disabled code)
		return methodDeclaration
			.DescendantTrivia()
			.Any(static trivia => trivia.IsKind(SyntaxKind.DisabledTextTrivia)
				&& trivia.ToString().Contains(useMauiCommunityToolkitCameraMethodName, StringComparison.Ordinal));
	}
}