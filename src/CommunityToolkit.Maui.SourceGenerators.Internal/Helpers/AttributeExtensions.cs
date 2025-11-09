using System.Reflection;
using Microsoft.CodeAnalysis;

namespace CommunityToolkit.Maui.SourceGenerators.Internal.Helpers;

static class AttributeExtensions
{
	public static TypedConstant GetAttributeValueByName(this AttributeData attribute, string name)
	{
		var x = attribute.NamedArguments.SingleOrDefault(kvp => kvp.Key == name).Value;
		return x;
	}

	public static string GetNamedTypeArgumentsAttributeValueByNameAsCastedString(this AttributeData attribute, string name, string placeholder = "null")
	{
		var data = attribute.NamedArguments.SingleOrDefault(kvp => kvp.Key == name).Value;

		// true.ToString() => "True" and false.ToString() => "False", but we want "true" and "false"
		if (data.Kind is TypedConstantKind.Primitive && data.Type?.SpecialType is SpecialType.System_Boolean)
		{
			return data.Value is null ? placeholder : $"({data.Type}){data.Value.ToString().ToLowerInvariant()}";
		}

		if (data.Kind is TypedConstantKind.Enum && data.Type is not null && data.Value is not null)
		{
			var members = data.Type.GetMembers();

			return $"({data.Type}){members[(int)data.Value]}";
		}

		return data.Value is null ? placeholder : $"({data.Type}){data.Value}";
	}

	public static string GetNamedMethodGroupArgumentsAttributeValueByNameAsString(this AttributeData attribute, string name, string placeholder = "null")
	{
		var data = attribute.NamedArguments.SingleOrDefault(kvp => kvp.Key == name).Value;

		return data.Value is null ? placeholder : data.Value.ToString();
	}

	public static string GetConstructorArgumentsAttributeValueByNameAsString(this AttributeData attribute, string placeholder)
	{
		if (attribute.ConstructorArguments.Length is 0)
		{
			return placeholder;
		}

		var data = attribute.ConstructorArguments[0];

		return data.Value is null ? placeholder : data.Value.ToString();
	}

}