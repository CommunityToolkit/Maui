using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace CommunityToolkit.Maui.MediaElement.SourceGenerators;

/// <summary>
/// Code fix provider that adds the required Android manifest configuration when the 
/// AndroidMediaElementServiceConfigurationAnalyzer detects missing configuration.
/// </summary>
/// <remarks>
/// This code fix provider is a placeholder for future implementation. It would:
/// 1. Detect when AndroidMediaElementServiceConfigurationAnalyzer reports a diagnostic
/// 2. Offer code actions to the developer
/// 3. Help add the required Android manifest configuration
///
/// The actual implementation requires the Microsoft.CodeAnalysis.CodeFixes package
/// which should be added to the project file dependencies.
/// </remarks>
public static class AndroidMediaElementServiceConfigurationCodeFixDocumentation
{
	/// <summary>
	/// Diagnostic IDs that this code fix provider would handle.
	/// </summary>
	public static ImmutableArray<string> HandledDiagnosticIds =>
	[
		AndroidMediaElementServiceDiagnostics.MissingAndroidManifestConfigurationId,
		AndroidMediaElementServiceDiagnostics.AndroidServiceNotConfiguredId,
	];

	/// <summary>
	/// Required Android manifest permissions.
	/// </summary>
	public const string RequiredPermissions = """
		<uses-permission android:name="android.permission.FOREGROUND_SERVICE"/>
		<uses-permission android:name="android.permission.POST_NOTIFICATIONS"/>
		<uses-permission android:name="android.permission.FOREGROUND_SERVICE_MEDIA_PLAYBACK"/>
		<uses-permission android:name="android.permission.MEDIA_CONTENT_CONTROL"/>
		""";
}
