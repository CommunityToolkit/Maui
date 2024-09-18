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

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(rule);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
	}

	static void AnalyzeNode(SyntaxNodeAnalysisContext context)
	{
		var invocationExpression = (InvocationExpressionSyntax)context.Node;

		if (invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccessExpression)
		{
			return;
		}

		if (memberAccessExpression.Name.Identifier.ValueText == useMauiCommunityToolkitMethodName)
		{
			var root = invocationExpression.SyntaxTree.GetRoot();

			if (memberAccessExpression.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault() is MethodDeclarationSyntax methodDeclaration)
			{
				var useMauiCommunityToolkitCall = methodDeclaration.DescendantNodes()
					.OfType<InvocationExpressionSyntax>()
					.Any(static n => n.Expression is MemberAccessExpressionSyntax m && m.Name.Identifier.ValueText == useMauiCommunityToolkitMethodName);

				if (!useMauiCommunityToolkitCall)
				{
					var diagnostic = Diagnostic.Create(rule, invocationExpression.GetLocation());
					context.ReportDiagnostic(diagnostic);
				}
			}
		}
	}
}