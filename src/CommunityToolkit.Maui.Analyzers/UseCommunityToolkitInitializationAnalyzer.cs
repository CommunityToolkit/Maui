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
		context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.SimpleMemberAccessExpression);
	}

	static void AnalyzeNode(SyntaxNodeAnalysisContext context)
	{
		var memberAccessExpression = (MemberAccessExpressionSyntax)context.Node;

		if (memberAccessExpression.Name is not GenericNameSyntax genericName)
		{
			return;
		}

		if (genericName.Arity != 1 || genericName.Identifier.Text != useMauiAppMethodName)
		{
			return;
		}

		var root = memberAccessExpression.SyntaxTree.GetRoot();
		bool found = false;

		foreach (var item in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
		{
			if (item.Name is IdentifierNameSyntax identifierName && identifierName.Identifier.Text == useMauiCommunityToolkitMethodName)
			{
				found = true;
				break;
			}
		}

		if (!found)
		{
			var diagnostic = Diagnostic.Create(rule, genericName.GetLocation());
			context.ReportDiagnostic(diagnostic);
		}
	}
}