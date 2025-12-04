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

	public static string GetNamedTypeArgumentsAttributeValueByNameAsCastedString(this AttributeData attribute, string name, ITypeSymbol propertyType, string placeholder = "null")
	{
		var data = attribute.NamedArguments.SingleOrDefault(kvp => kvp.Key == name).Value;

		// true.ToString() => "True" and false.ToString() => "False", but we want "true" and "false"
		if (data.Kind is TypedConstantKind.Primitive && data.Type?.SpecialType is SpecialType.System_Boolean)
		{
			return data.Value is null ? placeholder : $"({data.Type}){data.Value.ToString().ToLowerInvariant()}";
		}

		if (data.Kind is TypedConstantKind.Enum && data.Type is not null && data.Value is not null)
		{
			return $"({data.Type}){data.Value}";
		}

		if (data.Type?.SpecialType is SpecialType.System_String)
		{
			// Special handling for TimeSpan string representations - only when property type is TimeSpan
			if (data.Value is string stringValue && IsTimeSpanType(propertyType) && TimeSpan.TryParse(stringValue, CultureInfo.InvariantCulture, out var timeSpanValue))
			{
				// Check if it's TimeSpan.Zero
				if (timeSpanValue == TimeSpan.Zero)
				{
					return "global::System.TimeSpan.Zero";
				}

				// For other TimeSpan values, use the ticks constructor
				return $"new global::System.TimeSpan({timeSpanValue.Ticks})";
			}

			return data.Value is null ? $"\"{placeholder}\"" : $"({data.Type})\"{data.Value}\"";
		}

		if (data.Type?.SpecialType is SpecialType.System_Char)
		{
			return data.Value is null ? $"\"{placeholder}\"" : $"({data.Type})\'{data.Value}\'";
		}

		return data.Value is null ? placeholder : $"({data.Type}){data.Value}";
	}

	public static string GetNamedMethodGroupArgumentsAttributeValueByNameAsString(this AttributeData attribute, string name, string placeholder = "null")
	{
		var data = attribute.NamedArguments.SingleOrDefault(kvp => kvp.Key == name).Value;

		return data.Value is null ? placeholder : data.Value.ToString();
	}

	static bool IsTimeSpanType(ITypeSymbol typeSymbol)
	{
		if (typeSymbol is null)
		{
			return false;
		}

		// Check if it's System.TimeSpan by comparing name
		return typeSymbol is { Name: "TimeSpan", ContainingNamespace: not null }
			   && typeSymbol.ContainingNamespace.ToDisplayString() == "System";
	}
}