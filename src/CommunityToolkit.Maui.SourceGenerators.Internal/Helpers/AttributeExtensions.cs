using Microsoft.CodeAnalysis;

namespace CommunityToolkit.Maui.SourceGenerators.Internal.Helpers;

static class AttributeExtensions
{
	public static TypedConstant GetAttributeValueByName(this AttributeData attribute, string name)
	{
		var x = attribute.NamedArguments.SingleOrDefault(kvp => kvp.Key == name).Value;
		return x;
	}

	public static string GetEnumValueByNameAsString(this AttributeData attribute, string name, string placeholder)
	{
		var data = attribute.NamedArguments.SingleOrDefault(kvp => kvp.Key == name).Value;

		if (data.Value is null)
		{
			return placeholder;
		}

		var enumType = data.Type ?? throw new InvalidOperationException("Type cannot be null");

		var fullyQualifiedEnumType = enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		var members = enumType.GetMembers();

		return $"{fullyQualifiedEnumType}.{members[(int)data.Value]}";
	}

	public static string GetNamedArgumentsAttributeValueByNameAsString(this AttributeData attribute, string name, string placeholder = "null")
	{
		var data = attribute.NamedArguments.SingleOrDefault(kvp => kvp.Key == name).Value;

		// true.ToString() => "True" and false.ToString() => "False", but we want "true" and "false"
		if (data.Kind is TypedConstantKind.Primitive && data.Type?.SpecialType is SpecialType.System_Boolean)
		{
			return data.Value is null ? placeholder : data.Value.ToString().ToLowerInvariant();
		}

		return data.Value is null ? placeholder : data.Value.ToString();
	}

	public static string GetConstructorArgumentsAttributeValueByNameAsString(this AttributeData attribute, string placeholder)
	{
		if (attribute.ConstructorArguments.Length is 0)
		{
			return placeholder;
		}

		var data = attribute.ConstructorArguments[0];

		// true.ToString() => "True" and false.ToString() => "False", but we want "true" and "false"
		if (data.Kind is TypedConstantKind.Primitive && data.Type?.SpecialType is SpecialType.System_Boolean)
		{
			return data.Value is null ? placeholder : data.Value.ToString().ToLowerInvariant();
		}

		return data.Value is null ? placeholder : data.Value.ToString();
	}

}