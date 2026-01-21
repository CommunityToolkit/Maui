using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommunityToolkit.Maui.MediaElement.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UseCommunityToolkitMediaElementEnableForegroundServiceAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "MCTME002";

	const string category = "Usage";
	const string useMauiCommunityToolkitMediaElementMethodName = "UseMauiCommunityToolkitMediaElement";

	static readonly LocalizableString title = new LocalizableResourceString(nameof(Resources.EnableForegroundServiceErrorTitle), Resources.ResourceManager, typeof(Resources));
	static readonly LocalizableString messageFormat = new LocalizableResourceString(nameof(Resources.EnableForegroundServiceMessageFormat), Resources.ResourceManager, typeof(Resources));
	static readonly LocalizableString description = new LocalizableResourceString(nameof(Resources.EnableForegroundServiceErrorMessage), Resources.ResourceManager, typeof(Resources));

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
			&& memberAccessExpression.Name.Identifier.ValueText == useMauiCommunityToolkitMediaElementMethodName)
		{
		// Check if the enableForegroundService parameter is missing
		if (!HasEnableForegroundServiceParameter(invocationExpression, context.SemanticModel))
		{
			// Report diagnostic on just the method call. Try to match compiler spans:
			// - When there are no arguments, span the method name only (exclude the leading dot and parentheses)
			// - Otherwise, span from the dot up to the end of the argument list
			var spanStart = memberAccessExpression.OperatorToken.SpanStart;
			var spanEnd = invocationExpression.ArgumentList is null
				? invocationExpression.Span.End
				: invocationExpression.ArgumentList.Span.End;

			if (invocationExpression.ArgumentList is { Arguments.Count: 0 })
			{
				spanStart = memberAccessExpression.Name.SpanStart;
				spanEnd = memberAccessExpression.Name.Span.End;
			}

			var location = Location.Create(
				invocationExpression.SyntaxTree,
				Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(spanStart, spanEnd));
			var diagnostic = Diagnostic.Create(rule, location);
			context.ReportDiagnostic(diagnostic);
		}
		}
	}

	static bool HasEnableForegroundServiceParameter(InvocationExpressionSyntax invocationExpression, SemanticModel? semanticModel)
	{
		if (semanticModel is null)
		{
			return true; // Assume it's correct if we can't analyze
		}

		// Get the symbol information for the invocation
		var symbolInfo = semanticModel.GetSymbolInfo(invocationExpression);
		
		// Try to get the method symbol from either the resolved symbol or candidate symbols
		IMethodSymbol? methodSymbol = symbolInfo.Symbol as IMethodSymbol;
		if (methodSymbol is null && symbolInfo.CandidateSymbols.Length > 0)
		{
			// When there's a compiler error, check candidate symbols
			// Look for a method with the enableForegroundService parameter
			methodSymbol = symbolInfo.CandidateSymbols
				.OfType<IMethodSymbol>()
				.FirstOrDefault(m => m.Parameters.Any(p => p.Name == "enableForegroundService"));
		}

			if (methodSymbol is null)
			{
				return true; // Can't determine, assume correct
			}

		// Check if the method has an 'enableForegroundService' parameter
		var enableForegroundServiceParam = methodSymbol.Parameters.FirstOrDefault(static p => p.Name == "enableForegroundService");
		if (enableForegroundServiceParam is null)
		{
			return true; // Method doesn't have this parameter, not our concern
		}

		// Get the argument list
		var argumentList = invocationExpression.ArgumentList;
		if (argumentList is null || argumentList.Arguments.Count == 0)
		{
			return false;
		}

		// Check if any argument is provided for enableForegroundService
		foreach (var argument in argumentList.Arguments)
		{
			// Check for named argument: enableForegroundService: true/false
			if (argument.NameColon?.Name.Identifier.ValueText == "enableForegroundService")
			{
				return true;
			}

			// Check for positional argument (first parameter)
			if (argument.NameColon is null)
			{
				// Get the parameter that this argument maps to
				var argumentIndex = argumentList.Arguments.IndexOf(argument);
				if (argumentIndex < methodSymbol.Parameters.Length)
				{
				var parameter = methodSymbol.Parameters[argumentIndex];
				if (parameter.Name == "enableForegroundService")
				{
					// Check if the argument is actually a bool or a compatible type
					// If it's a lambda (SimpleLambdaExpression or ParenthesizedLambdaExpression),
					// it's likely the user forgot about enableForegroundService
					if (argument.Expression is LambdaExpressionSyntax)
					{
						return false; // Lambda passed where bool expected - missing enableForegroundService
					}
					return true;
				}
				}
			}
		}

		return false; // enableForegroundService parameter exists but was not provided
	}
}
