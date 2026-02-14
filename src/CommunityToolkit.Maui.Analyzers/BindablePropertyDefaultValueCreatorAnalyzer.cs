using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommunityToolkit.Maui.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BindablePropertyDefaultValueCreatorAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "MCT003";
	const string category = "Usage";
	const string bindablePropertyAttributeName = "BindablePropertyAttribute";
	const string attachedBindablePropertyAttributeName = "AttachedBindablePropertyAttribute";
	const string defaultValueCreatorMethodNameProperty = "DefaultValueCreatorMethodName";

	static readonly LocalizableString title = new LocalizableResourceString(nameof(Resources.AttachedBindablePropertyDefaultValueCreatorErrorTitle), Resources.ResourceManager, typeof(Resources));
	static readonly LocalizableString messageFormat = new LocalizableResourceString(nameof(Resources.AttachedBindablePropertyDefaultValueCreatorMessageFormat), Resources.ResourceManager, typeof(Resources));
	static readonly LocalizableString description = new LocalizableResourceString(nameof(Resources.AttachedBindablePropertyDefaultValueCreatorErrorMessage), Resources.ResourceManager, typeof(Resources));
	static readonly DiagnosticDescriptor rule = new(DiagnosticId, title, messageFormat, category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: description);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [rule];

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
	}

	static void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is not AttributeSyntax attributeSyntax)
		{
			return;
		}

		var attributeSymbol = context.SemanticModel.GetSymbolInfo(attributeSyntax, context.CancellationToken).Symbol?.ContainingType;

		if (attributeSymbol is null)
		{
			return;
		}

		if (!IsBindablePropertyAttribute(attributeSymbol))
		{
			return;
		}

		var defaultValueCreatorMethodName = GetDefaultValueCreatorMethodName(attributeSyntax);
		if (defaultValueCreatorMethodName is null || string.IsNullOrWhiteSpace(defaultValueCreatorMethodName))
		{
			return;
		}

		var containingType = attributeSyntax.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();

		if (containingType is null)
		{
			return;
		}

		var methodDeclaration = FindMethodInType(containingType, defaultValueCreatorMethodName);

		if (methodDeclaration is null)
		{
			return;
		}

		if (ReturnsStaticReadOnlyFieldOrProperty(methodDeclaration, context.SemanticModel, context.CancellationToken))
		{
			var diagnostic = Diagnostic.Create(rule, methodDeclaration.GetLocation(), defaultValueCreatorMethodName);
			context.ReportDiagnostic(diagnostic);
		}
	}

	static bool IsBindablePropertyAttribute(INamedTypeSymbol attributeSymbol)
	{
		var currentType = attributeSymbol;

		while (currentType is not null)
		{
			if (currentType.Name.StartsWith(attachedBindablePropertyAttributeName, StringComparison.Ordinal)
				|| currentType.Name.StartsWith(bindablePropertyAttributeName, StringComparison.Ordinal))
			{
				return true;
			}

			currentType = currentType.BaseType;
		}

		return false;
	}

	static string? GetDefaultValueCreatorMethodName(AttributeSyntax attributeSyntax)
	{
		if (attributeSyntax.ArgumentList is null)
		{
			return null;
		}

		foreach (var argument in attributeSyntax.ArgumentList.Arguments)
		{
			if (argument.NameEquals?.Name.Identifier.ValueText != defaultValueCreatorMethodNameProperty)
			{
				continue;
			}

			// Handle string literal: DefaultValueCreatorMethodName = "MethodName"
			if (argument.Expression is LiteralExpressionSyntax literalExpression
				&& literalExpression.IsKind(SyntaxKind.StringLiteralExpression))
			{
				return literalExpression.Token.ValueText;
			}

			// Handle nameof expression: DefaultValueCreatorMethodName = nameof(MethodName)
			if (argument.Expression is InvocationExpressionSyntax invocationExpression
				&& invocationExpression.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" }
				&& invocationExpression.ArgumentList.Arguments.Count == 1)
			{
				var nameofArgument = invocationExpression.ArgumentList.Arguments[0];
				if (nameofArgument.Expression is IdentifierNameSyntax identifierName)
				{
					return identifierName.Identifier.ValueText;
				}
			}
		}

		return null;
	}

	static MethodDeclarationSyntax? FindMethodInType(TypeDeclarationSyntax typeDeclaration, string methodName)
	{
		return typeDeclaration
			.DescendantNodes()
			.OfType<MethodDeclarationSyntax>()
			.FirstOrDefault(method => method.Identifier.ValueText == methodName);
	}

	static bool ReturnsStaticReadOnlyFieldOrProperty(MethodDeclarationSyntax methodDeclaration, SemanticModel semanticModel, CancellationToken cancellationToken)
	{
		var returnStatements = methodDeclaration
			.DescendantNodes()
			.OfType<ReturnStatementSyntax>();

		foreach (var returnStatement in returnStatements)
		{
			if (returnStatement.Expression is null)
			{
				continue;
			}

			if (IsStaticReadOnlyMemberAccess(returnStatement.Expression, semanticModel, cancellationToken))
			{
				return true;
			}
		}

		if (methodDeclaration.ExpressionBody is not null)
		{
			return IsStaticReadOnlyMemberAccess(methodDeclaration.ExpressionBody.Expression, semanticModel, cancellationToken);
		}

		return false;
	}

	static bool IsStaticReadOnlyMemberAccess(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
	{
		// Handle conditional expressions (ternary operator)
		// e.g., condition ? DefaultStateViews : []
		if (expression is ConditionalExpressionSyntax conditionalExpression)
		{
			// Check if either branch returns a static readonly member
			return IsStaticReadOnlyMemberAccess(conditionalExpression.WhenTrue, semanticModel, cancellationToken)
				|| IsStaticReadOnlyMemberAccess(conditionalExpression.WhenFalse, semanticModel, cancellationToken);
		}

		// Handle binary expressions (e.g., null coalescing operator ??)
		// e.g., null ?? DefaultStateViews
		if (expression is BinaryExpressionSyntax binaryExpression)
		{
			// Check if either side returns a static readonly member
			return IsStaticReadOnlyMemberAccess(binaryExpression.Left, semanticModel, cancellationToken)
				|| IsStaticReadOnlyMemberAccess(binaryExpression.Right, semanticModel, cancellationToken);
		}

		// Handle switch expressions
		// e.g., bindable switch { null => DefaultStateViews, _ => new List<View>() }
		if (expression is SwitchExpressionSyntax switchExpression)
		{
			// Check if any arm returns a static readonly member
			foreach (var arm in switchExpression.Arms)
			{
				if (IsStaticReadOnlyMemberAccess(arm.Expression, semanticModel, cancellationToken))
				{
					return true;
				}
			}
			return false;
		}

		// Handle parenthesized expressions
		// e.g., (DefaultStateViews)
		if (expression is ParenthesizedExpressionSyntax parenthesizedExpression)
		{
			return IsStaticReadOnlyMemberAccess(parenthesizedExpression.Expression, semanticModel, cancellationToken);
		}

		// Handle cast expressions
		// e.g., (IList<View>)DefaultStateViews
		if (expression is CastExpressionSyntax castExpression)
		{
			return IsStaticReadOnlyMemberAccess(castExpression.Expression, semanticModel, cancellationToken);
		}

		// Check if the expression itself is a static readonly member
		var symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);

		if (symbolInfo.Symbol is null)
		{
			return false;
		}

		return symbolInfo.Symbol switch
		{
			IFieldSymbol fieldSymbol => fieldSymbol.IsStatic && fieldSymbol.IsReadOnly,
			IPropertySymbol propertySymbol => propertySymbol.IsStatic && propertySymbol.IsReadOnly,
			_ => false
		};
	}
}
