using System.Globalization;

namespace CommunityToolkit.Maui.SpeechToText;

/// <inheritdoc cref="ISpeechToText"/>
public static class SpeechToText
{
	static Lazy<ISpeechToText> defaultImplementation = new(new SpeechToTextImplementation());

	/// <summary>
	/// Default implementation of <see cref="ISpeechToText"/>
	/// </summary>
	public static ISpeechToText Default => defaultImplementation.Value;

	/// <inheritdoc cref="ISpeechToText.Listen"/>
	public static Task<string> Listen(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken) =>
		Default.Listen(culture, recognitionResult, cancellationToken);

	internal static void SetDefault(ISpeechToText implementation) =>
		defaultImplementation = new(implementation);
}