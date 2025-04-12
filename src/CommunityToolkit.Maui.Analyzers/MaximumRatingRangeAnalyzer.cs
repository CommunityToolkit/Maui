// Ignore Spelling: Analyzer

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommunityToolkit.Maui.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MaximumRatingRangeAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "MCT002";
	const string title = "Invalid MaximumRating value";
	const string messageFormat = "The value of MaximumRating must be between {0} and {1}";
	const string description = "Ensures that the MaximumRating property of RatingView is within a valid range.";
	const string category = "Usage";
	const int minValue = 1;
	const int maxValue = 10;

	static readonly DiagnosticDescriptor rule = new(
		DiagnosticId,
		title,
		messageFormat,
		category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: description
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [rule];

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterSyntaxNodeAction(AnalyzePropertyAssignment, SyntaxKind.SimpleAssignmentExpression);
	}

	static void AnalyzePropertyAssignment(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is AssignmentExpressionSyntax { Left: IdentifierNameSyntax { Identifier.Text: "MaximumRating" } leftIdentifier, Right: LiteralExpressionSyntax { Token.Value: int value } } assignmentExpression)
		{
			var semanticModel = context.SemanticModel;
			var propertySymbol = semanticModel.GetSymbolInfo(leftIdentifier).Symbol as IPropertySymbol;

			if (propertySymbol?.ContainingType.Name == "RatingView" && (value < minValue || value > maxValue))
			{
				var diagnostic = Diagnostic.Create(rule, assignmentExpression.GetLocation(), minValue, maxValue);
				context.ReportDiagnostic(diagnostic);
			}
		}
	}
}