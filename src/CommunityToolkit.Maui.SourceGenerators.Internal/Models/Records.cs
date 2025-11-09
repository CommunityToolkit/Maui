using CommunityToolkit.Maui.SourceGenerators.Internal.Helpers;
using Microsoft.CodeAnalysis;

namespace CommunityToolkit.Maui.SourceGenerators.Internal.Models;

record BindablePropertyModel(string PropertyName, ITypeSymbol ReturnType, ITypeSymbol DeclaringType, string DefaultValue, string DefaultBindingMode, string ValidateValueMethodName, string PropertyChangedMethodName, string PropertyChangingMethodName, string CoerceValueMethodName, string DefaultValueCreatorMethodName, string NewKeywordText)
{
	public string BindablePropertyName => $"{PropertyName}Property";
}

record SemanticValues(ClassInformation ClassInformation, EquatableArray<BindablePropertyModel> BindableProperties);

readonly record struct ClassInformation(string ClassName, string DeclaredAccessibility, string ContainingNamespace);