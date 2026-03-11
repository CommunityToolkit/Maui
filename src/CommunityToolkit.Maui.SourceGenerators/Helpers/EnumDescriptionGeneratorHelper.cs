using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Globalization;

namespace CommunityToolkit.Maui.SourceGenerators.Helpers;

static class EnumDescriptionGeneratorHelper
{
	public static string? GetDescriptionFromDescriptionAttribute(IFieldSymbol member, string descriptionAttributeName)
	{
		AttributeData? descAttr = member.GetAttributes()
			.FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == descriptionAttributeName);

		if (descAttr != null && descAttr.ConstructorArguments.Length > 0)
		{
			if (descAttr.ConstructorArguments[0].Value is string description && !string.IsNullOrWhiteSpace(description))
			{
				return description;
			}
		}

		return null;
	}

	public static bool TryGetDisplayInfo(IFieldSymbol member, string displayAttributeName, out string? displayName, out INamedTypeSymbol? resourceType)
	{
		displayName = null;
		resourceType = null;

		AttributeData? displayAttr = member.GetAttributes()
			.FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == displayAttributeName);

		if (displayAttr is null)
		{
			return false;
		}

		foreach (KeyValuePair<string, TypedConstant> namedArg in displayAttr.NamedArguments)
		{
			if (namedArg.Key == "Name" && !namedArg.Value.IsNull && namedArg.Value.Value is string name)
			{
				displayName = name;
			}
			else if (namedArg.Key == "ResourceType" && !namedArg.Value.IsNull && namedArg.Value.Value is INamedTypeSymbol namedType)
			{
				resourceType = namedType;
			}
			else if (namedArg.Key == "ResourceType" && !namedArg.Value.IsNull && namedArg.Value.Value is ITypeSymbol typeSymbol && typeSymbol is INamedTypeSymbol resourceNamedType)
			{
				resourceType = resourceNamedType;
			}
		}

		return true;
	}

	public static bool TryGetLocalizedDisplayResolverExpression(INamedTypeSymbol resourceType, string resourceKey, IAssemblySymbol targetAssembly, out string expression)
	{
		expression = string.Empty;

		if (TryGetResourceManagerExpression(resourceType, resourceKey, targetAssembly, out var resourceManagerExpression))
		{
			expression = resourceManagerExpression;
			return true;
		}

		if (Microsoft.CodeAnalysis.CSharp.SyntaxFacts.IsValidIdentifier(resourceKey)
			&& TryGetResourceMemberExpression(resourceType, resourceKey, targetAssembly, out var resourceMemberExpression))
		{
			expression = resourceMemberExpression;
			return true;
		}

		return false;
	}

	static bool TryGetResourceManagerExpression(INamedTypeSymbol resourceType, string resourceKey, IAssemblySymbol targetAssembly, out string expression)
	{
		expression = string.Empty;

		IPropertySymbol? resourceManagerProperty = resourceType
			.GetMembers("ResourceManager")
			.OfType<IPropertySymbol>()
			.FirstOrDefault(static p => p.IsStatic && p.GetMethod is not null);

		if (resourceManagerProperty is null || !IsAccessibleToAssembly(resourceManagerProperty, targetAssembly))
		{
			return false;
		}

		if (resourceManagerProperty.Type.ToDisplayString() != "System.Resources.ResourceManager")
		{
			return false;
		}

		var resourceTypeName = resourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		expression = $"{resourceTypeName}.ResourceManager.GetString(\"{EscapeString(resourceKey)}\", culture ?? global::System.Globalization.CultureInfo.CurrentUICulture)";
		return true;
	}

	public static bool TryGetResourceMemberExpression(INamedTypeSymbol resourceType, string resourceKey, IAssemblySymbol targetAssembly, out string expression)
	{
		expression = string.Empty;

		IPropertySymbol? property = resourceType.GetMembers(resourceKey).OfType<IPropertySymbol>()
			.FirstOrDefault(static p => p.IsStatic && p.GetMethod is not null);

		if (property is not null)
		{
			if (property.Type.SpecialType == SpecialType.System_String && IsAccessibleToAssembly(property, targetAssembly))
			{
				var resourceTypeName = resourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
				expression = $"{resourceTypeName}.{resourceKey}";
				return true;
			}

			return false;
		}

		IFieldSymbol? field = resourceType.GetMembers(resourceKey).OfType<IFieldSymbol>()
			.FirstOrDefault(static f => f.IsStatic);

		if (field is not null && field.Type.SpecialType == SpecialType.System_String && IsAccessibleToAssembly(field, targetAssembly))
		{
			var resourceTypeName = resourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
			expression = $"{resourceTypeName}.{resourceKey}";
			return true;
		}

		return false;
	}

	public static bool IsAccessibleToAssembly(ISymbol symbol, IAssemblySymbol targetAssembly)
		=> symbol.DeclaredAccessibility switch
		{
			Accessibility.Public => true,
			Accessibility.Internal or Accessibility.ProtectedOrInternal => SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, targetAssembly),
			_ => false
		};

	public static string EscapeString(string input)
	{
		return input.Replace("\\", "\\\\")
				   .Replace("\"", "\\\"")
				   .Replace("\n", "\\n")
				   .Replace("\r", "\\r")
				   .Replace("\t", "\\t");
	}

	public static bool IsAccessibleFromNamespace(INamedTypeSymbol enumSymbol)
	{
		if (!IsTypeAccessible(enumSymbol))
		{
			return false;
		}

		INamedTypeSymbol? containingType = enumSymbol.ContainingType;
		while (containingType is not null)
		{
			if (!IsTypeAccessible(containingType))
			{
				return false;
			}

			containingType = containingType.ContainingType;
		}

		return true;
	}

	public static bool IsTypeAccessible(INamedTypeSymbol typeSymbol)
		=> typeSymbol.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal;
}
