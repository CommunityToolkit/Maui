using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommunityToolkit.Maui.CameraView.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UseCommunityToolkitCameraInitializationAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "MCTME001";

	const string category = "Initialization";
	static readonly LocalizableString title = new LocalizableResourceString(nameof(Resources.InitializationErrorTitle), Resources.ResourceManager, typeof(Resources));
	static readonly LocalizableString messageFormat = new LocalizableResourceString(nameof(Resources.InitalizationMessageFormat), Resources.ResourceManager, typeof(Resources));
	static readonly LocalizableString description = new LocalizableResourceString(nameof(Resources.InitializationErrorMessage), Resources.ResourceManager, typeof(Resources));

	static readonly DiagnosticDescriptor rule = new(DiagnosticId, title, messageFormat, category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: description);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(rule);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ExpressionStatement);
	}

	static void AnalyzeNode(SyntaxNodeAnalysisContext context)
	{
		var expressionStatement = (ExpressionStatementSyntax)context.Node;
		var root = expressionStatement.SyntaxTree.GetRoot();

		if (HasUseMauiCommunityToolkit(root))
		{
			return;
		}

		if (CheckIfItIsUseMauiMethod(expressionStatement))
		{
			var expression = GetInvocationExpressionSyntax(expressionStatement);
			var diagnostic = Diagnostic.Create(rule, expression.GetLocation());
			context.ReportDiagnostic(diagnostic);
		}
	}

	static bool CheckIfItIsUseMauiMethod(ExpressionStatementSyntax expressionStatement) =>
		expressionStatement.DescendantNodes()
							.OfType<GenericNameSyntax>()
							.Any(x => x.Identifier.ValueText.Equals("UseMauiApp", StringComparison.Ordinal)
										&& x.TypeArgumentList.Arguments.Count is 1);

	static bool HasUseMauiCommunityToolkit(SyntaxNode root)
	{
		foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
		{
			if (method.DescendantNodes().OfType<ExpressionStatementSyntax>().Any(x => x.DescendantNodes().Any(x => x.ToString().Contains(".UseMauiCommunityToolkitCamera("))))
			{
				return true;
			}
		}

		return false;
	}

	static InvocationExpressionSyntax GetInvocationExpressionSyntax(SyntaxNode parent)
	{
		foreach (var child in parent.ChildNodes())
		{
			if (child is InvocationExpressionSyntax expressionSyntax)
			{
				return expressionSyntax;
			}
			else
			{
				var expression = GetInvocationExpressionSyntax(child);

				if (expression is not null)
				{
					return expression;
				}
			}
		}

		throw new InvalidOperationException("Wow, this shouldn't happen, please open a bug here: https://github.com/CommunityToolkit/Maui/issues/new/choose");
	}
}