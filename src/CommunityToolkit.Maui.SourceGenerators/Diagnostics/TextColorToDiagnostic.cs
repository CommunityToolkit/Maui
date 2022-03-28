using Microsoft.CodeAnalysis;

namespace CommunityToolkit.Maui.SourceGenerators;

static class TextColorToDiagnostic
{
	const string category = "TextColorTo";
	public static readonly DiagnosticDescriptor GlobalNamespace = new(
		   "MCT001",
		   "Global namespace is not support for this Source Generator",
		   "Please put '{0}' inside a valid namespace",
		   category,
		   DiagnosticSeverity.Warning,
		   true);

	public static readonly DiagnosticDescriptor MauiReferenceIsMissing = new(
		   "MCT002",
		   "Was not possible to find Microsoft.Maui.ITextStyle and or Microsoft.Maui.IAnimatable",
		   "Please make sure that your project is referencing Microsoft.Maui",
		   category,
		   DiagnosticSeverity.Error,
		   true);

	public static readonly DiagnosticDescriptor InvalidClassDeclarationSyntax = new(
		   "MCT003",
		   "Was not possible to get information from the Class",
		   "Please make sure that the code inside '{0}' has not error, the TextColorTo methods will not be generated for this file",
		   category,
		   DiagnosticSeverity.Info,
		   true);

	public static readonly DiagnosticDescriptor InvalidModifierAccess = new(
		   "MCT004",
		   "Class marked with invalid modifier access",
		   "TextColorTo only supports public and internal classes ineriting from ITextStyle, please fix '{0}'",
		   category,
		   DiagnosticSeverity.Info,
		   true);

}