using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommunityToolkit.Maui.MediaElement.Analyzers;

/// <summary>
/// Analyzer that detects when Android Foreground Service is enabled for MediaElement
/// and reports missing AndroidManifest.xml configuration.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AndroidMediaElementServiceConfigurationAnalyzer : DiagnosticAnalyzer
{
	/// <summary>
	/// Diagnostic ID for missing Android manifest configuration when service is enabled.
	/// </summary>
	public const string DiagnosticId = $"MCTME002";

	const string category = "Usage";

	const string mediaElementOptionsClassName = "MediaElementOptions";
	const string isAndroidForegroundServiceEnabledProperty = "IsAndroidForegroundServiceEnabled";
	const string setDefaultAndroidForegroundServiceEnabled = "SetDefaultAndroidForegroundServiceEnabled";
	const string useMauiCommunityToolkitMediaElement = "UseMauiCommunityToolkitMediaElement";

	static readonly LocalizableString title = new LocalizableResourceString(nameof(Resources.AndroidMediaElementServiceConfigurationTitle), Resources.ResourceManager, typeof(Resources));
	static readonly LocalizableString messageFormat = new LocalizableResourceString(nameof(Resources.AndroidMediaElementServiceConfigurationFormat), Resources.ResourceManager, typeof(Resources));
	static readonly LocalizableString description = new LocalizableResourceString(nameof(Resources.AndroidMediaElementServiceConfigurationErrorMessage), Resources.ResourceManager, typeof(Resources));

	static readonly DiagnosticDescriptor rule = new(DiagnosticId, title, messageFormat, category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: description);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [rule];

	public override void Initialize(AnalysisContext context)
	{
		// Don't analyze generated code
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		// Register to analyze class declarations (for MediaElementOptions)
		context.RegisterSymbolAction(AnalyzeClass, SymbolKind.NamedType);

		// Register to analyze method invocations (for SetDefaultAndroidForegroundServiceEnabled calls)
		context.RegisterSyntaxNodeAction(AnalyzeMethodInvocation, SyntaxKind.InvocationExpression);
	}

	static void AnalyzeClass(SymbolAnalysisContext context)
	{
		if (context.Symbol is not INamedTypeSymbol namedType ||
			!namedType.Name.Contains(mediaElementOptionsClassName, StringComparison.Ordinal))
		{
			return;
		}

		// Check if the class has the IsAndroidForegroundServiceEnabled property

		if (namedType.GetMembers(isAndroidForegroundServiceEnabledProperty)
			.FirstOrDefault() is not IPropertySymbol property)
		{
			return;
		}

		// Only static property initializers should trigger the diagnostic
		if (!property.IsStatic)
		{
			return;
		}

		// Check if the property has an initializer with true value
		var hasPropertyInitializer = namedType.DeclaringSyntaxReferences
			.SelectMany(syntaxRef => GetPropertyInitializers(syntaxRef))
			.Any(init => IsInitializedToTrue(init));

		if (hasPropertyInitializer)
		{
			var diagnostic = Diagnostic.Create(
				rule,
				property.Locations[0]);

			context.ReportDiagnostic(diagnostic);
		}
	}

	static void AnalyzeMethodInvocation(SyntaxNodeAnalysisContext context)
	{
		var invocation = (InvocationExpressionSyntax)context.Node;

		// Check if this is a call to SetDefaultAndroidForegroundServiceEnabled
		var methodName = GetMethodName(invocation);

		if (methodName != setDefaultAndroidForegroundServiceEnabled &&
			methodName != useMauiCommunityToolkitMediaElement)
		{
			return;
		}

		var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

		if (methodName == setDefaultAndroidForegroundServiceEnabled &&
			methodSymbol?.ContainingType?.Name != mediaElementOptionsClassName)
		{
			return;
		}

		// Check if true is being passed as argument
		if (methodName == setDefaultAndroidForegroundServiceEnabled &&
			invocation.ArgumentList.Arguments.Count > 0)
		{
			var firstArg = invocation.ArgumentList.Arguments[0].Expression;
			if (firstArg.Kind() == SyntaxKind.TrueLiteralExpression)
			{
				var diagnostic = Diagnostic.Create(
					rule,
					firstArg.GetLocation());

				context.ReportDiagnostic(diagnostic);
			}
		}
	}

	static string? GetMethodName(InvocationExpressionSyntax invocation)
	{
		if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
		{
			return memberAccess.Name.Identifier.Text;
		}

		if (invocation.Expression is IdentifierNameSyntax identifier)
		{
			return identifier.Identifier.Text;
		}

		return null;
	}

	static IEnumerable<ExpressionSyntax> GetPropertyInitializers(SyntaxReference syntaxRef)
	{
		var syntax = syntaxRef.GetSyntax();

		if (syntax is not ClassDeclarationSyntax classDecl)
		{
			yield break;
		}

		foreach (var member in classDecl.Members)
		{
			if (member is PropertyDeclarationSyntax propDecl &&
				propDecl.Identifier.Text == isAndroidForegroundServiceEnabledProperty &&
				propDecl.Initializer?.Value is not null)
			{
				yield return propDecl.Initializer.Value;
			}
		}
	}

	static bool IsInitializedToTrue(ExpressionSyntax expression)
	{
		return expression.Kind() == SyntaxKind.TrueLiteralExpression;
	}
}
