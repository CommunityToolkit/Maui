using CommunityToolkit.Maui.SourceGenerators.Internal.Helpers;
using Microsoft.CodeAnalysis;

namespace CommunityToolkit.Maui.SourceGenerators.Internal.Models;

public record BindablePropertyModel(string PropertyName, ITypeSymbol ReturnType, ITypeSymbol DeclaringType, string DefaultValue, string DefaultBindingMode, string ValidateValueMethodName, string PropertyChangedMethodName, string PropertyChangingMethodName, string CoerceValueMethodName, string DefaultValueCreatorMethodName, string NewKeywordText, bool IsReadOnlyBindableProperty, string? SetterAccessibility, bool HasInitializer)
{
	// When both a DefaultValueCreatorMethodName and an initializer are provided, we implement the DefaultValueCreator method and the ignore the partial Property initializer
	public bool ShouldUsePropertyInitializer => HasInitializer && string.IsNullOrEmpty(DefaultValueCreatorMethodName);
	public string BindablePropertyName => $"{PropertyName}Property";
	public string BindablePropertyKeyName => $"{char.ToLower(PropertyName[0])}{PropertyName[1..]}PropertyKey";
	public string EffectiveDefaultValueCreatorMethodName => ShouldUsePropertyInitializer ? $"__createDefault{PropertyName}" : DefaultValueCreatorMethodName;
	public string InitializingPropertyName => $"__initializing{PropertyName}";

}

public record SemanticValues(ClassInformation ClassInformation, EquatableArray<BindablePropertyModel> BindableProperties);

public readonly record struct ClassInformation(string ClassName, string DeclaredAccessibility, string ContainingNamespace, string ContainingTypes = "", string GenericTypeParameters = "");