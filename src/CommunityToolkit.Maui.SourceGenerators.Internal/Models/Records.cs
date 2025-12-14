using CommunityToolkit.Maui.SourceGenerators.Internal.Helpers;
using Microsoft.CodeAnalysis;

namespace CommunityToolkit.Maui.SourceGenerators.Internal.Models;

public record BindablePropertyModel(string PropertyName, ITypeSymbol ReturnType, ITypeSymbol DeclaringType, string DefaultBindingMode, string ValidateValueMethodName, string PropertyChangedMethodName, string PropertyChangingMethodName, string CoerceValueMethodName, string DefaultValueCreatorMethodName, string NewKeywordText, bool IsReadOnlyBindableProperty, string? SetterAccessibility, bool HasInitializer)
{
	// When both a DefaultValueCreatorMethodName and an initializer are provided, we implement the DefaultValueCreator method and the ignore the partial Property initializer
	public bool ShouldUsePropertyInitializer => HasInitializer && DefaultValueCreatorMethodName is "null";
	public string BindablePropertyName => $"{PropertyName}Property";
	public string BindablePropertyKeyName => $"{char.ToLower(PropertyName[0])}{PropertyName[1..]}PropertyKey";
	public string EffectiveValidateValueMethodName => ValidateValueMethodName is "null" ? ValidateValueMethodName : $"(b,v) => (({DeclaringType})b).{ValidateValueMethodName}(v)";
	public string EffectivePropertyChangedMethodName => PropertyChangedMethodName is "null" ? PropertyChangedMethodName : $"(b,o,n) => (({DeclaringType})b).{PropertyChangedMethodName}(o,n)";
	public string EffectivePropertyChangingMethodName => PropertyChangingMethodName is "null" ? PropertyChangingMethodName : $"(b,o,n) => (({DeclaringType})b).{PropertyChangingMethodName}(o,n)";
	public string EffectiveCoerceValueMethodName => CoerceValueMethodName is "null" ? CoerceValueMethodName : $"(b,v) => (({DeclaringType})b).{CoerceValueMethodName}(v)";
	public string EffectiveDefaultValueCreatorMethodName
		=> ShouldUsePropertyInitializer
		? $"CreateDefault{PropertyName}"
		: (DefaultValueCreatorMethodName is "null" ? DefaultValueCreatorMethodName : $"(b) => (({DeclaringType})b).{DefaultValueCreatorMethodName}()");
	public string InitializingPropertyName => $"IsInitializing{PropertyName}";

}

public record SemanticValues(ClassInformation ClassInformation, EquatableArray<BindablePropertyModel> BindableProperties);

public readonly record struct ClassInformation(string ClassName, string DeclaredAccessibility, string ContainingNamespace, string ContainingTypes = "", string GenericTypeParameters = "");