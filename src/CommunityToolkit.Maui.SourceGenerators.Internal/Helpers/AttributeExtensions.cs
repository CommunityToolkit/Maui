using Microsoft.CodeAnalysis;

namespace CommunityToolkit.Maui.SourceGenerators.Internal.Helpers;

static class AttributeExtensions
{
	public static TypedConstant GetAttributeValueByName(this AttributeData attribute, string name)
	{
		var x = attribute.NamedArguments.SingleOrDefault(kvp => kvp.Key == name).Value;
		return x;
	}

	public static string GetNamedArgumentsAttributeValueByNameAsString(this AttributeData attribute, string name, string placeholder = "null")
	{
		var data = attribute.NamedArguments.SingleOrDefault(kvp => kvp.Key == name).Value;

		// true.ToString() => "True" and false.ToString() => "False", but we want "true" and "false"
		if (data.Kind is TypedConstantKind.Primitive && data.Type?.SpecialType is SpecialType.System_Boolean)
		{
			return data.Value is null ? placeholder : data.Value.ToString().ToLowerInvariant();
		}

		if (data.Kind is TypedConstantKind.Enum && data.Type is not null && data.Value is not null)
		{
			var members = data.Type.GetMembers();

			return members[(int)data.Value].ToString();
		}

		// Ensure floating point types include a decimal point in their string, otherwise runtime will interpret them as integers and throw a TypeException
		if (data.Type?.SpecialType is SpecialType.System_Double or SpecialType.System_Single or SpecialType.System_Decimal)
		{
			var text = data.Value?.ToString() ?? throw new InvalidOperationException("Value of a primitive type cannot be null");
			if (!text.Contains("."))
			{
				text += ".0";
			}
			return text;
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