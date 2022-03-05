using CommunityToolkit.Maui.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using static CommunityToolkit.Maui.Analyzers.Constants;

namespace CommunityToolkit.Maui.Analyzer
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class CheckForNotImplementedExceptionAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "CheckForNotImplementedExceptionAnalyzer";

		static readonly LocalizableString title = new LocalizableResourceString(nameof(Resources.NotImplementedAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
		static readonly LocalizableString messageFormat = new LocalizableResourceString(nameof(Resources.NotImplementedMessageFormat), Resources.ResourceManager, typeof(Resources));
		static readonly LocalizableString description = new LocalizableResourceString(nameof(Resources.NotImplementedAnalyzerDescription), Resources.ResourceManager, typeof(Resources));

		const string notImplementExceptionString = "NotImplementedException";

		static readonly DiagnosticDescriptor rule = new(DiagnosticId, title, messageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			//context.RegisterSyntaxNodeAction()
			Debugger.Launch();

			context.RegisterSyntaxTreeAction(SyntaxTreeAnalyzer);
		}

		static void SyntaxTreeAnalyzer(SyntaxTreeAnalysisContext context)
		{
			var root = context.Tree.GetRoot(context.CancellationToken);
			var nodes = root.DescendantNodesAndSelf();

			var throwStatementSyntaxCollection = nodes.OfType<ThrowStatementSyntax>();
			var throwExpressionSyntaxCollection = nodes.OfType<ThrowExpressionSyntax>();

			if (!throwStatementSyntaxCollection.Any() && !throwExpressionSyntaxCollection.Any())
			{
				return;
			}

			LookForNotImplementedException(throwStatementSyntaxCollection, context);
			LookForNotImplementedException(throwExpressionSyntaxCollection, context);
		}

		static void LookForNotImplementedException(IEnumerable<ThrowExpressionSyntax> throwExpressionSyntaxCollection, SyntaxTreeAnalysisContext context)
		{
			foreach (var item in throwExpressionSyntaxCollection)
			{
				if (item.Expression.DescendantTokens().Any(x => x.ValueText == notImplementExceptionString))
				{
					var diagnostic = Diagnostic.Create(rule, item.GetLocation());
					context.ReportDiagnostic(diagnostic);
				}
			}
		}

		static void LookForNotImplementedException(IEnumerable<ThrowStatementSyntax> throwExceptionsCollection, SyntaxTreeAnalysisContext context)
		{
			foreach (var item in throwExceptionsCollection)
			{
				if (item.Expression.DescendantTokens().Any(x => x.ValueText == notImplementExceptionString))
				{
					var diagnostic = Diagnostic.Create(rule, item.GetLocation());
					context.ReportDiagnostic(diagnostic);
				}
			}
		}
	}
}
