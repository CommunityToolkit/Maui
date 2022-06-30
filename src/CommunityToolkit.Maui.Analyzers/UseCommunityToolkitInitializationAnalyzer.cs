using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
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
	static readonly LocalizableString title = new LocalizableResourceString(nameof(Resources.InitializationErrorTitle), Resources.ResourceManager, typeof(Resources));
	static readonly LocalizableString messageFormat = new LocalizableResourceString(nameof(Resources.IniitalizationMessageFormat), Resources.ResourceManager, typeof(Resources));
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

		if (invocationExpression.ToString().Contains("UseMauiApp<")
			&& !invocationExpression.ToString().Contains(".UseMauiCommunityToolkit("))
		{
			var memberAccessExpressions = invocationExpression.ChildNodes().OfType<MemberAccessExpressionSyntax>();
			foreach(var expression in memberAccessExpressions)//.OfType<GenericNameSyntax>())
			{
				var descendantNodes = expression.DescendantNodes(_ => true);
				expression.Expressione
			}
			
			var diagnostic = Diagnostic.Create(rule, invocationExpression.GetLocation());
			context.ReportDiagnostic(diagnostic);
		}
	}
}

