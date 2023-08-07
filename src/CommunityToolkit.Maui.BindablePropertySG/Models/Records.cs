using CommunityToolkit.Maui.BindablePropertySG.Helpers;
using Microsoft.CodeAnalysis;

namespace CommunityToolkit.Maui.BindablePropertySG.Models;

record BPInfo
{
	public string PropertyName { get; set; } = string.Empty;
	public ITypeSymbol? ReturnType { get; set; }
	public string DeclaringType { get; set; } = string.Empty;
	public string DefaultValue { get; set; } = string.Empty;
	public string DefaultBindingMode { get; set; } = string.Empty;
	public string ValidateValueMethodName { get; set; } = string.Empty;
	public string PropertyChangedMethodName { get; set; } = string.Empty;
	public string PropertyChangingMethodName { get; set; } = string.Empty;
	public string CoerceValueMethodName { get; set; } = string.Empty;
	public string DefaultValueCreatorMethodName { get; set; } = string.Empty;
}

record SemanticValues(ClassInformation ClassInformation, EquatableArray<BPInfo> BPInfos);

readonly record struct ClassInformation(string ClassName, string DeclaredAccessibility, string ContainingNamespace);