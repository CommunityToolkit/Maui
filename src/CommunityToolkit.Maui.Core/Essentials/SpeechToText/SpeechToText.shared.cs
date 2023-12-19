using System.Globalization;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc cref="ISpeechToText"/>
public static class SpeechToText
{
	static Lazy<ISpeechToText> defaultImplementation = new(new SpeechToTextImplementation());

	/// <summary>
	/// Default implementation of <see cref="ISpeechToText"/>
	/// </summary>
	public static ISpeechToText Default => defaultImplementation.Value;

	/// <inheritdoc cref="ISpeechToText.ListenAsync"/>
	public static Task<SpeechToTextResult> ListenAsync(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken = default) =>
		Default.ListenAsync(culture, recognitionResult, cancellationToken);

	/// <inheritdoc cref="ISpeechToText.StartListenAsync"/>
	public static Task StartListenAsync(CultureInfo culture, CancellationToken cancellationToken = default) =>
		Default.StartListenAsync(culture, cancellationToken);

	/// <inheritdoc cref="ISpeechToText.StopListenAsync"/>
	public static Task StopListenAsync(CancellationToken cancellationToken = default) =>
		Default.StopListenAsync(cancellationToken);

	/// <inheritdoc cref="ISpeechToText.RequestPermissions"/>
	public static Task<bool> RequestPermissions(CancellationToken cancellationToken = default) =>
		Default.RequestPermissions(cancellationToken);

	internal static void SetDefault(ISpeechToText implementation) =>
		defaultImplementation = new(implementation);
}