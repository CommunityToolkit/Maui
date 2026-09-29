using System.ComponentModel;

namespace CommunityToolkit.Maui;

/// <summary>
/// Diagnostic IDs for BindableProperty source generator
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class BindablePropertyDiagnostic
{
	/// <summary>
	/// Represents the diagnostic ID for experimental usage of the BindablePropertyAttribute.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const string BindablePropertyAttributeExperimentalDiagnosticId = "MCTEXP001";
}