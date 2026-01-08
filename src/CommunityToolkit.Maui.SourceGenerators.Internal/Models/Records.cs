using CommunityToolkit.Maui.SourceGenerators.Internal.Helpers;
using Microsoft.CodeAnalysis;

namespace CommunityToolkit.Maui.SourceGenerators.Internal.Models;

public record BindablePropertyModel(string PropertyName, ITypeSymbol ReturnType, ITypeSymbol DeclaringType, string DefaultBindingMode, string ValidateValueMethodName, string PropertyChangedMethodName, string PropertyChangingMethodName, string CoerceValueMethodName, string DefaultValueCreatorMethodName, string NewKeywordText, bool IsReadOnlyBindableProperty, string? SetterAccessibility, bool HasInitializer, string? PropertyAccessibility)
{
	// When both a DefaultValueCreatorMethodName and an initializer are provided, we implement the DefaultValueCreator method and the ignore the partial Property initializer
	public bool ShouldUsePropertyInitializer => HasInitializer && DefaultValueCreatorMethodName is "null";
	public string BindablePropertyName => $"{PropertyName}Property";
	public string BindablePropertyKeyName => $"{char.ToLower(PropertyName[0])}{PropertyName[1..]}PropertyKey";
	public string EffectiveDefaultValueCreatorMethodName => ShouldUsePropertyInitializer ? $"CreateDefault{PropertyName}" : DefaultValueCreatorMethodName;
	public string InitializingPropertyName => $"IsInitializing{PropertyName}";

}

public record AttachedBindablePropertyModel(string PropertyName, ITypeSymbol ReturnType, ITypeSymbol DeclaringType, string DefaultValue, string DefaultBindingMode, string ValidateValueMethodName, string PropertyChangedMethodName, string PropertyChangingMethodName, string CoerceValueMethodName, string DefaultValueCreatorMethodName, string? GetterAccessibility, string? SetterAccessibility, string BindablePropertyAccessibility, bool isDeclaringTypeNullable, string? BindablePropertyXMLDocumentation, string? GetterMethodXMLDocumentation, string? SetterMethodXMLDocumentation)
{
	public string BindablePropertyName => $"{PropertyName}Property";
	public bool ShouldPostpendNullable => isDeclaringTypeNullable && ReturnType.OriginalDefinition.SpecialType is not SpecialType.System_Nullable_T;
}

public record BindablePropertySemanticValues(ClassInformation ClassInformation, EquatableArray<BindablePropertyModel> BindableProperties);

public record AttachedBindablePropertySemanticValues(ClassInformation ClassInformation, EquatableArray<AttachedBindablePropertyModel> BindableProperties);

public readonly record struct ClassInformation(string ClassName, string DeclaredAccessibility, string ContainingNamespace, string ContainingTypes = "", string GenericTypeParameters = "");