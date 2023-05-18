using System.Globalization;
using Microsoft.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc cref="ISpeechToText"/>
public sealed partial class SpeechToTextImplementation : ISpeechToText
{
	/// <inheritdoc/>
	public async Task<SpeechToTextResult> ListenAsync(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken)
	{
		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			var isPermissionGranted = await IsSpeechPermissionAuthorized();
			if (!isPermissionGranted)
			{
				return new SpeechToTextResult(null, new Exception("Speech Recognizer Permission not granted"));
			}

			var finalResult = await InternalListenAsync(culture, recognitionResult, cancellationToken);
			return new SpeechToTextResult(finalResult, null);
		}
		catch (Exception e)
		{
			return new SpeechToTextResult(null, e);
		}
	}

#if !MACCATALYST && !IOS
	/// <inheritdoc/>
	public async Task<bool> RequestPermissions(CancellationToken cancellationToken)
	{
		var status = await Permissions.RequestAsync<Permissions.Microphone>();
		return status is PermissionStatus.Granted;
	}

	static async Task<bool> IsSpeechPermissionAuthorized()
	{
		var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
		return status is PermissionStatus.Granted;
	}
#endif
}