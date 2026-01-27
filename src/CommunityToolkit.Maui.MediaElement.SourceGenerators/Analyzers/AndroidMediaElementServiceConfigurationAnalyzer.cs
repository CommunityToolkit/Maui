using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommunityToolkit.Maui.MediaElement.SourceGenerators;

/// <summary>
/// Analyzer that detects when Android Foreground Service is enabled for MediaElement
/// and reports missing AndroidManifest.xml configuration.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AndroidMediaElementServiceConfigurationAnalyzer : DiagnosticAnalyzer
{
	const string mediaElementOptionsClassName = "MediaElementOptions";
	const string isAndroidForegroundServiceEnabledProperty = "IsAndroidForegroundServiceEnabled";
	const string setDefaultAndroidForegroundServiceEnabled = "SetDefaultAndroidForegroundServiceEnabled";
	const string useMauiCommunityToolkitMediaElement = "UseMauiCommunityToolkitMediaElement";

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		ImmutableArray.Create(
			AndroidMediaElementServiceDiagnostics.MissingAndroidManifestConfigurationDescriptor,
			AndroidMediaElementServiceDiagnostics.AndroidServiceNotConfiguredDescriptor);

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
		var namedType = context.Symbol as INamedTypeSymbol;

		if (namedType?.Name != mediaElementOptionsClassName)
		{
			return;
		}

		// Check if the class has the IsAndroidForegroundServiceEnabled property
		var property = namedType.GetMembers(isAndroidForegroundServiceEnabledProperty)
			.FirstOrDefault() as IPropertySymbol;

		if (property is null)
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
				AndroidMediaElementServiceDiagnostics.AndroidServiceNotConfiguredDescriptor,
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

		// Check if true is being passed as argument
		if (methodName == setDefaultAndroidForegroundServiceEnabled &&
			invocation.ArgumentList.Arguments.Count > 0)
		{
			var firstArg = invocation.ArgumentList.Arguments[0].Expression;
			if (firstArg.Kind() == SyntaxKind.TrueLiteralExpression)
			{
				var diagnostic = Diagnostic.Create(
					AndroidMediaElementServiceDiagnostics.MissingAndroidManifestConfigurationDescriptor,
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
