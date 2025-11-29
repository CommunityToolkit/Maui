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

	public static string GetNamedTypeArgumentsAttributeValueByNameAsCastedString(this AttributeData attribute, string name, string placeholder = "null")
	{
		var data = attribute.NamedArguments.SingleOrDefault(kvp => kvp.Key == name).Value;

		// true.ToString() => "True" and false.ToString() => "False", but we want "true" and "false"
		if (data.Kind is TypedConstantKind.Primitive && data.Type?.SpecialType is SpecialType.System_Boolean)
		{
			return data.Value is null ? placeholder : $"({data.Type}){data.Value.ToString().ToLowerInvariant()}";
		}

		if (data.Kind is TypedConstantKind.Enum && data is { Type: INamedTypeSymbol namedTypeSymbol, Value: not null })
		{
			var underlyingType = namedTypeSymbol.EnumUnderlyingType ?? throw new InvalidOperationException($"Unable to determine underlying type for enum");
			var members = data.Type.GetMembers();

			return underlyingType.SpecialType switch
			{
				SpecialType.System_Byte => $"({data.Type}){members[(byte)data.Value]}",
				SpecialType.System_SByte => $"({data.Type}){members[(sbyte)data.Value]}",
				SpecialType.System_Int16 => $"({data.Type}){members[(short)data.Value]}",
				SpecialType.System_UInt16 => $"({data.Type}){members[(ushort)data.Value]}",
				SpecialType.System_Int32 => $"({data.Type}){members[(int)data.Value]}",
				// SpecialType.System_UInt32 => $"({data.Type}){members[(uint)data.Value]}",
				// SpecialType.System_Int64 => $"({data.Type}){members[(long)data.Value]}",
				SpecialType.System_UInt64 => $"({data.Type}){members[(ushort)data.Value]}",
				_ => throw new NotSupportedException("The enum type is not yet supported")
			};
		}

		if (data.Type?.SpecialType is SpecialType.System_String)
		{
			return data.Value is null ? $"\"{placeholder}\"" : $"({data.Type})\"{data.Value}\"";
		}

		if (data.Type?.SpecialType is SpecialType.System_Char)
		{
			return data.Value is null ? $"\"{placeholder}\"" : $"({data.Type})\'{data.Value}\'";
		}

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
			var members = data.Type.GetMembers();

			return $"({data.Type}){members[(int)data.Value]}";
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