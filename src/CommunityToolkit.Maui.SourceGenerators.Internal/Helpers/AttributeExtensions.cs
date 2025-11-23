using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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
			return data.Value is null ? placeholder : data.Value.ToString().ToLowerInvariant();
		}
		if (data.Kind is TypedConstantKind.Primitive && data.Type?.SpecialType is SpecialType.System_Int32)
		{
			return data.Value is null ? placeholder : ((int)data.Value).ToString("0", System.Globalization.CultureInfo.InvariantCulture);
		}
		if (data.Kind is TypedConstantKind.Primitive && data.Type?.SpecialType is SpecialType.System_Double)
		{
			return data.Value is null ? placeholder : ((double)data.Value).ToString("0.0###############", System.Globalization.CultureInfo.InvariantCulture);
		}
		if (data.Kind is TypedConstantKind.Primitive && data.Type?.Name == nameof(System.TimeSpan))
		{
			return data.Value is null ? placeholder : $"System.TimeSpan.FromTicks({((System.TimeSpan)data.Value).Ticks})";
		}
		if (data.Kind is TypedConstantKind.Enum && data.Type is not null && data.Value is not null)
		{
			var members = data.Type.GetMembers();

			return members[(int)data.Value].ToString();
		}

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

		if(data.Type?.SpecialType is SpecialType.System_String)
		{
			return data.Value is null ? $"\"{placeholder}\"": $"({data.Type})\"{data.Value}\"";
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

		// true.ToString() => "True" and false.ToString() => "False", but we want "true" and "false"
		if (data.Kind is TypedConstantKind.Primitive && data.Type?.SpecialType is SpecialType.System_Boolean)
		{
			return data.Value is null ? placeholder : data.Value.ToString().ToLowerInvariant();
		}
		if (data.Kind is TypedConstantKind.Primitive && data.Type?.SpecialType is SpecialType.System_Int32)
		{
			return data.Value is null ? placeholder : ((int)data.Value).ToString("0", System.Globalization.CultureInfo.InvariantCulture);
		}
		if (data.Kind is TypedConstantKind.Primitive && data.Type?.SpecialType is SpecialType.System_Double)
		{
			return data.Value is null ? placeholder : ((double)data.Value).ToString("0.0###############", System.Globalization.CultureInfo.InvariantCulture);
		}
		return data.Value is null ? placeholder : data.Value.ToString();
	}
}