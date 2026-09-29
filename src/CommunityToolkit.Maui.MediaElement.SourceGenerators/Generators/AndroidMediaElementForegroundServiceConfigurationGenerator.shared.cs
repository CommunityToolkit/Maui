using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CommunityToolkit.Maui.MediaElement.SourceGenerators;

/// <summary>
/// Source generator that detects when Android Foreground Service is enabled for MediaElement
/// and generates the required assembly-level permissions.
/// </summary>
[Generator]
public class AndroidMediaElementForegroundServiceConfigurationGenerator : IIncrementalGenerator
{
	const string mediaElementOptionsClassName = "MediaElementOptions";
	const string isAndroidForegroundServiceEnabledProperty = "IsAndroidForegroundServiceEnabled";
	const string setDefaultAndroidForegroundServiceEnabledMethod = "SetIsAndroidForegroundServiceEnabled";
	const string useMauiCommunityToolkitMediaElementMethod = "UseMauiCommunityToolkitMediaElement";

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// Detect MediaElementOptions to determine if Android Foreground Service is enabled
		var mediaElementProvider = context.SyntaxProvider
			.CreateSyntaxProvider(
				predicate: (syntax, _) => IsMediaElementOptionsClass(syntax),
				transform: (ctx, _) => GetConfigurationInfo(ctx))
			.Where(info => info is not null)
			.Collect();

		var isEnabledFromOptions = mediaElementProvider
			.Select((configs, _) => configs.Where(c => c is not null)
				.Cast<ConfigurationInfo>()
				.Any(c => c.IsForegroundServiceEnabled));

		var isEnabledFromInvocation = context.SyntaxProvider
			.CreateSyntaxProvider(
				predicate: (syntax, _) => IsSetDefaultAndroidForegroundServiceEnabledInvocation(syntax),
				transform: (ctx, _) => GetForegroundServiceEnabledFromInvocation(ctx))
			.Where(isEnabled => isEnabled)
			.Collect()
			.Select((results, _) => results.Any());

		var isEnabledFromMediaElementInvocation = context.SyntaxProvider
			.CreateSyntaxProvider(
				predicate: (syntax, _) => IsUseMauiCommunityToolkitMediaElementInvocation(syntax),
				transform: (ctx, _) => GetForegroundServiceEnabledFromMediaElementInvocation(ctx))
			.Where(isEnabled => isEnabled)
			.Collect()
			.Select((results, _) => results.Any());

		var isForegroundServiceEnabled = isEnabledFromOptions
			.Combine(isEnabledFromInvocation)
			.Combine(isEnabledFromMediaElementInvocation)
			.Select((pair, _) => pair.Left.Left || pair.Left.Right || pair.Right);

		context.RegisterSourceOutput(isForegroundServiceEnabled, (spc, isEnabled) =>
		{
			if (isEnabled)
			{
				GeneratePermissions(spc);
			}
		});
	}

	static bool IsMediaElementOptionsClass(SyntaxNode node)
	{
		return node is ClassDeclarationSyntax classDecl &&
			   classDecl.Identifier.Text.Contains(mediaElementOptionsClassName);
	}

	static bool IsSetDefaultAndroidForegroundServiceEnabledInvocation(SyntaxNode node)
	{
		return node is InvocationExpressionSyntax invocation &&
			GetInvocationMethodName(invocation) == setDefaultAndroidForegroundServiceEnabledMethod;
	}

	static bool IsUseMauiCommunityToolkitMediaElementInvocation(SyntaxNode node)
	{
		return node is InvocationExpressionSyntax invocation &&
			GetInvocationMethodName(invocation) == useMauiCommunityToolkitMediaElementMethod;
	}

	static bool GetForegroundServiceEnabledFromInvocation(GeneratorSyntaxContext context)
	{
		if (context.Node is not InvocationExpressionSyntax invocation)
		{
			return false;
		}

		var semanticModel = context.SemanticModel;
		var symbolInfo = semanticModel.GetSymbolInfo(invocation);
		var methodSymbol = symbolInfo.Symbol as IMethodSymbol ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();

		if (methodSymbol is null || methodSymbol.Name != setDefaultAndroidForegroundServiceEnabledMethod)
		{
			return false;
		}

		if (methodSymbol.ContainingType?.Name != mediaElementOptionsClassName)
		{
			return false;
		}

		if (invocation.ArgumentList.Arguments.Count == 0)
		{
			return false;
		}

		var firstArg = invocation.ArgumentList.Arguments[0].Expression;
		var constantValue = semanticModel.GetConstantValue(firstArg);

		return constantValue.HasValue && constantValue.Value is true;
	}

	static bool GetForegroundServiceEnabledFromMediaElementInvocation(GeneratorSyntaxContext context)
	{
		if (context.Node is not InvocationExpressionSyntax invocation)
		{
			return false;
		}

		var semanticModel = context.SemanticModel;
		var symbolInfo = semanticModel.GetSymbolInfo(invocation);
		var methodSymbol = symbolInfo.Symbol as IMethodSymbol ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();

		if (methodSymbol is null || methodSymbol.Name != useMauiCommunityToolkitMediaElementMethod)
		{
			return false;
		}

		// Verify this is an extension method
		if (!methodSymbol.IsExtensionMethod)
		{
			return false;
		}

		// Check if it has at least 2 parameters (this MauiAppBuilder, bool isAndroidForegroundServiceEnabled)
		if (methodSymbol.Parameters.Length < 2)
		{
			return false;
		}

		// Verify the second parameter is a boolean
		var secondParam = methodSymbol.Parameters[0];
		if (secondParam.Type.SpecialType != SpecialType.System_Boolean)
		{
			return false;
		}

		// Get the second argument (first argument is the builder via extension method call)
		if (invocation.ArgumentList.Arguments.Count < 1)
		{
			return false;
		}

		// In an extension method call like builder.UseMauiCommunityToolkitMediaElement(true, ...)
		// The first argument in ArgumentList corresponds to the second parameter in the method signature
		var firstArg = invocation.ArgumentList.Arguments[0].Expression;
		var constantValue = semanticModel.GetConstantValue(firstArg);

		return constantValue.HasValue && constantValue.Value is true;
	}

	static string? GetInvocationMethodName(InvocationExpressionSyntax invocation)
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

	static ConfigurationInfo? GetConfigurationInfo(GeneratorSyntaxContext context)
	{
		if (context.Node is not ClassDeclarationSyntax classDecl)
		{
			return null;
		}

		var className = classDecl.Identifier.Text;

		if (className.Contains(mediaElementOptionsClassName))
		{
			// Look for the IsAndroidForegroundServiceEnabled property
			var propertyDecl = classDecl.Members
				.OfType<PropertyDeclarationSyntax>()
				.FirstOrDefault(p => p.Identifier.Text == isAndroidForegroundServiceEnabledProperty);

			if (propertyDecl?.Initializer?.Value is null)
			{
				return null;
			}

			// Use semantic analysis to get the constant value
			var semanticModel = context.SemanticModel;
			var constantValue = semanticModel.GetConstantValue(propertyDecl.Initializer.Value);
			var isEnabled = constantValue.HasValue && constantValue.Value is true;

			var symbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;

			return new ConfigurationInfo
			{
				ClassName = className,
				Namespace = symbol?.ContainingNamespace.ToDisplayString() ?? "",
				IsForegroundServiceEnabled = isEnabled,
				ClassType = "MediaElementOptions"
			};
		}

		return null;
	}

	static void GeneratePermissions(SourceProductionContext context)
	{
		var sb = new StringBuilder();

		sb.AppendLine("// <auto-generated>");
		sb.AppendLine("// See: CommunityToolkit.Maui.MediaElement.SourceGenerators.AndroidMediaElementServiceConfigurationGenerator");
		sb.AppendLine("// This file provides assembly-level Android permissions and service attributes");
		sb.AppendLine("// when Android Foreground Service is enabled for MediaElement.");
		sb.AppendLine("");
		sb.AppendLine("#pragma warning disable");
		sb.AppendLine("#nullable enable");
		sb.AppendLine("");
		sb.AppendLine("#if ANDROID");
		sb.AppendLine("using Android.App;");
		sb.AppendLine("");
		sb.AppendLine("[assembly: UsesPermission(Android.Manifest.Permission.ForegroundServiceMediaPlayback)]");
		sb.AppendLine("[assembly: UsesPermission(Android.Manifest.Permission.ForegroundService)]");
		sb.AppendLine("[assembly: UsesPermission(Android.Manifest.Permission.MediaContentControl)]");
		sb.AppendLine("[assembly: UsesPermission(Android.Manifest.Permission.PostNotifications)]");
		sb.AppendLine("");
		sb.AppendLine("namespace CommunityToolkit.Maui.Android;");
		sb.AppendLine("");
		sb.AppendLine("/// <summary>");
		sb.AppendLine("/// Auto-generated configuration for Android MediaElement Service.");
		sb.AppendLine("/// </summary>");
		sb.AppendLine("/// <remarks>");
		sb.AppendLine("/// This file is auto-generated and provides assembly-level permissions");
		sb.AppendLine("/// for MediaElement when <see cref=\"CommunityToolkit.Maui.Core.MediaElementOptions.IsAndroidForegroundServiceEnabled\"/> is enabled");
		sb.AppendLine("/// or when <see cref=\"UseMauiCommunityToolkitMediaElement(Microsoft.Maui.MauiAppBuilder, bool, System.Action{CommunityToolkit.Maui.Core.MediaElementOptions})\"/>");
		sb.AppendLine("/// is called with <c>isAndroidForegroundServiceEnabled</c> set to <c>true</c>.");
		sb.AppendLine("/// </remarks>");
		sb.AppendLine("internal static class AndroidMediaElementServiceConfiguration");
		sb.AppendLine("{");
		sb.AppendLine("\t/// <summary>");
		sb.AppendLine("\t/// Indicates that Android MediaElement Service configuration is required.");
		sb.AppendLine("\t/// </summary>");
		sb.AppendLine("\tpublic const bool IsRequired = true;");
		sb.AppendLine("}");
		sb.Append("#endif");

		var source = sb.ToString();
		context.AddSource("AndroidMediaElementServiceConfiguration.g.cs", SourceText.From(source, Encoding.UTF8));
	}

	sealed class ConfigurationInfo
	{
		public string? ClassName { get; set; }
		public string? Namespace { get; set; }
		public bool IsForegroundServiceEnabled { get; set; }
		public string? ClassType { get; set; }
	}
}