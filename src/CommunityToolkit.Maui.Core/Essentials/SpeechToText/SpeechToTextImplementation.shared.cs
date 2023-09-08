using System.Globalization;
using Microsoft.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc cref="ISpeechToText"/>
public sealed partial class SpeechToTextImplementation : ISpeechToText
{
	readonly WeakEventManager onRecognitionResultUpdatedWeakEventManager = new();
	readonly WeakEventManager onRecognitionResultCompletedWeakEventManager = new();

	/// <inheritdoc />
	public event EventHandler<OnSpeechToTextRecognitionResultUpdated> RecognitionResultUpdated
	{
		add => onRecognitionResultUpdatedWeakEventManager.AddEventHandler(value);
		remove => onRecognitionResultUpdatedWeakEventManager.RemoveEventHandler(value);
	}

	/// <inheritdoc />
	public event EventHandler<OnSpeechToTextRecognitionResultCompleted> RecognitionResultCompleted
	{
		add => onRecognitionResultCompletedWeakEventManager.AddEventHandler(value);
		remove => onRecognitionResultCompletedWeakEventManager.RemoveEventHandler(value);
	}

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

	/// <inheritdoc/>
	public async Task StartListeningAsync(CultureInfo culture, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var isPermissionGranted = await IsSpeechPermissionAuthorized();
		if (isPermissionGranted)
		{
			await InternalStartListeningAsync(culture);
		}

	}
	/// <inheritdoc/>
	public async Task StopListeningAsync(CancellationToken cancellationToken)
	{
		await InternalStopListeningAsync();
	}

	void OnRecognitionResultUpdated(string recognitionResult)
	{
		onRecognitionResultUpdatedWeakEventManager.HandleEvent(this, new OnSpeechToTextRecognitionResultUpdated(recognitionResult), nameof(RecognitionResultUpdated));
	}

	void OnRecognitionResultCompleted(string recognitionResult)
	{
		onRecognitionResultCompletedWeakEventManager.HandleEvent(this, new OnSpeechToTextRecognitionResultCompleted(recognitionResult), nameof(RecognitionResultCompleted));
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

/// <summary>
/// Event occurred on SpeechToText Recognition Result Updated
/// </summary>
public class OnSpeechToTextRecognitionResultUpdated : EventArgs
{
	/// <summary>
	/// Speech recognition result
	/// </summary>
	public string RecognitionResult { get; }

	/// <summary>
	/// Initialize a new instance of <see cref="OnSpeechToTextRecognitionResultUpdated"/>
	/// </summary>
	public OnSpeechToTextRecognitionResultUpdated(string recognitionResult)
	{
		RecognitionResult = recognitionResult;
	}
}

/// <summary>
/// Event occurred on SpeechToText Recognition Result Completed
/// </summary>
public class OnSpeechToTextRecognitionResultCompleted : EventArgs
{
	/// <summary>
	/// Speech recognition result
	/// </summary>
	public string RecognitionResult { get; }

	/// <summary>
	/// Initialize a new instance of <see cref="OnSpeechToTextRecognitionResultCompleted"/>
	/// </summary>
	public OnSpeechToTextRecognitionResultCompleted(string recognitionResult)
	{
		RecognitionResult = recognitionResult;
	}
}