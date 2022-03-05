using CommunityToolkit.Maui.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static CommunityToolkit.Maui.Analyzers.Constants;

namespace CommunityToolkit.Maui.Analyzer
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class CheckForNullArgumentAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "CheckForNullArgumentAnalyzer";

		// You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
		// See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization

		static readonly LocalizableString title = new LocalizableResourceString(nameof(Resources.NullArgumentAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
		static readonly LocalizableString messageFormat = new LocalizableResourceString(nameof(Resources.NullArgumentAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
		static readonly LocalizableString description = new LocalizableResourceString(nameof(Resources.NullArgumentAnalyzerDescription), Resources.ResourceManager, typeof(Resources));


		static readonly DiagnosticDescriptor rule = new(DiagnosticId, title, messageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			// TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
			// See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
			context.RegisterCodeBlockAction(MethodAnalyzer);
		}

		static void MethodAnalyzer(CodeBlockAnalysisContext context)
		{
			var method = (MethodDeclarationSyntax)context.CodeBlock;
			var parameters = method.ParameterList.Parameters;

			if (parameters.Count is 0)
			{
				return;
			}

			var body = method.Body;
			var throwStatementSyntaxCollection = body.DescendantNodesAndSelf().OfType<ThrowStatementSyntax>();
			var throwExpressionSyntaxCollection = body.DescendantNodesAndSelf().OfType<ThrowExpressionSyntax>();
			if (!throwStatementSyntaxCollection.Any() && !throwExpressionSyntaxCollection.Any())
			{
				return;
			}

			CheckForIfEqualStatements(throwStatementSyntaxCollection, parameters, context);
		}

		static void CheckForIfEqualStatements(IEnumerable<SyntaxNode> throwStatementSyntaxCollection, SeparatedSyntaxList<ParameterSyntax> parameters, CodeBlockAnalysisContext context)
		{
			foreach (var item in throwStatementSyntaxCollection)
			{
				var descendantNodes = item.Parent.Parent.DescendantNodesAndSelf();
				var patternExpression = descendantNodes.OfType<IsPatternExpressionSyntax>().FirstOrDefault();

				if (patternExpression is not null)
				{
					CheckForNullPatternMatching(parameters, patternExpression, context);
				}
			}
		}

		static void CheckForNullPatternMatching(SeparatedSyntaxList<ParameterSyntax> parameters, IsPatternExpressionSyntax patternExpression, CodeBlockAnalysisContext context)
		{
			foreach (var parameter in parameters)
			{
				var leftToken = patternExpression.Expression.GetFirstToken();
				var right = patternExpression.Pattern.GetFirstToken();

				if (leftToken.ValueText == parameter.Identifier.ValueText && right.IsKind(SyntaxKind.NullKeyword))
				{
					var diagnostic = Diagnostic.Create(rule, patternExpression.Parent.GetLocation(), leftToken.ValueText);
					context.ReportDiagnostic(diagnostic);
				}
			}
		}
	}
}
