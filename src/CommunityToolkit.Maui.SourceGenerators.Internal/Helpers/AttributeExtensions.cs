using System.Globalization;
using Microsoft.CodeAnalysis;

namespace CommunityToolkit.Maui.SourceGenerators.Internal.Helpers;

static class AttributeExtensions
{
	public static TypedConstant GetAttributeValueByName(this AttributeData attribute, string name)
	{
		var x = attribute.NamedArguments.SingleOrDefault(kvp => kvp.Key == name).Value;
		return x;
	}

	public static string GetNamedTypeArgumentsAttributeValueForDefaultBindingMode(this AttributeData attribute, string name, string placeholder = "null")
	{
		var data = attribute.NamedArguments.SingleOrDefault(kvp => kvp.Key == name).Value;

		return data.Value is null ? placeholder : $"({data.Type}){data.Value}";
	}

	public static string GetNamedMethodGroupArgumentsAttributeValueByNameAsString(this AttributeData attribute, string name, string placeholder = "null")
	{
		var data = attribute.NamedArguments.SingleOrDefault(kvp => kvp.Key == name).Value;

		return data.Value is null ? placeholder : data.Value.ToString();
	}
}