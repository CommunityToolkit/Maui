using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace CommunityToolkit.Maui.MediaElement.Analyzers;

/// <summary>
/// Diagnostic definitions for Android MediaElement Service configuration.
/// </summary>
public static class AndroidMediaElementServiceDiagnostics
{
	const string diagnosticIdPrefix = "MCTME";

	/// <summary>
	/// Diagnostic ID for missing Android manifest configuration when service is enabled.
	/// </summary>
	public const string MissingAndroidManifestConfigurationId = $"{diagnosticIdPrefix}002";

	/// <summary>
	/// Gets the diagnostic descriptor for missing Android manifest configuration.
	/// </summary>
	public static readonly DiagnosticDescriptor MissingAndroidManifestConfigurationDescriptor =
		new(
			id: MissingAndroidManifestConfigurationId,
			title: "Missing Android Manifest Configuration for MediaElement Service",
			messageFormat: "Android service is enabled for MediaElement, but the required manifest configuration is missing. " +
						   "The Toolkit auto-generates the AndroidManifest entries for this service; ensure the generated manifest is included in your Android build output.",
			category: "CommunityToolkit.Maui.MediaElement",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "When IsAndroidForegroundServiceEnabled is set to true, the AndroidManifest.xml file must include " +
						  "the service declaration for communityToolkit.maui.media.services and the following permissions: " +
						  "FOREGROUND_SERVICE, POST_NOTIFICATIONS, FOREGROUND_SERVICE_MEDIA_PLAYBACK, and MEDIA_CONTENT_CONTROL.",
			helpLinkUri: "https://github.com/CommunityToolkit/Maui/wiki/MediaElement-Android-Configuration");


	/// <summary>
	/// Gets all diagnostic descriptors.
	/// </summary>
	public static ImmutableArray<DiagnosticDescriptor> GetAllDiagnosticDescriptors() =>
		[MissingAndroidManifestConfigurationDescriptor];
}
