using System.Globalization;
using Microsoft.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc cref="ISpeechToText"/>
public sealed partial class SpeechToTextImplementation : ISpeechToText
{
	readonly WeakEventManager recognitionResultUpdatedWeakEventManager = new();
	readonly WeakEventManager recognitionResultCompletedWeakEventManager = new();
	readonly WeakEventManager speechToTextStateChangedWeakEventManager = new();

	/// <inheritdoc />
	public event EventHandler<SpeechToTextRecognitionResultUpdatedEventArgs> RecognitionResultUpdated
	{
		add => recognitionResultUpdatedWeakEventManager.AddEventHandler(value);
		remove => recognitionResultUpdatedWeakEventManager.RemoveEventHandler(value);
	}

	/// <inheritdoc />
	public event EventHandler<SpeechToTextRecognitionResultCompletedEventArgs> RecognitionResultCompleted
	{
		add => recognitionResultCompletedWeakEventManager.AddEventHandler(value);
		remove => recognitionResultCompletedWeakEventManager.RemoveEventHandler(value);
	}

	/// <inheritdoc />
	public event EventHandler<SpeechToTextStateChangedEventArgs> StateChanged
	{
		add => speechToTextStateChangedWeakEventManager.AddEventHandler(value);
		remove => speechToTextStateChangedWeakEventManager.RemoveEventHandler(value);
	}


	/// <inheritdoc/>
	public async Task<SpeechToTextResult> ListenAsync(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken = default)
	{
		try
		{
			var isPermissionGranted = await IsSpeechPermissionAuthorized(cancellationToken).ConfigureAwait(false);
			if (!isPermissionGranted)
			{
				return new SpeechToTextResult(null, new Exception("Speech Recognizer Permission not granted"));
			}

			var finalResult = await InternalListenAsync(culture, recognitionResult, cancellationToken).ConfigureAwait(false);
			return new SpeechToTextResult(finalResult, null);
		}
		catch (Exception e)
		{
			return new SpeechToTextResult(null, e);
		}
	}

	/// <inheritdoc/>
	public async Task StartListenAsync(CultureInfo culture, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var isPermissionGranted = await IsSpeechPermissionAuthorized(cancellationToken).ConfigureAwait(false);
		if (!isPermissionGranted)
		{
			throw new PermissionException($"{nameof(Permissions)}.{nameof(Permissions.Microphone)} Not Granted");
		}

		await InternalStartListeningAsync(culture, cancellationToken).ConfigureAwait(false);
	}

	/// <inheritdoc/>
	public Task StopListenAsync(CancellationToken cancellationToken = default) => InternalStopListeningAsync(cancellationToken);

	void OnRecognitionResultUpdated(string recognitionResult)
	{
		recognitionResultUpdatedWeakEventManager.HandleEvent(this, new SpeechToTextRecognitionResultUpdatedEventArgs(recognitionResult), nameof(RecognitionResultUpdated));
	}

	void OnRecognitionResultCompleted(string recognitionResult)
	{
		recognitionResultCompletedWeakEventManager.HandleEvent(this, new SpeechToTextRecognitionResultCompletedEventArgs(recognitionResult), nameof(RecognitionResultCompleted));
	}

	void OnSpeechToTextStateChanged(SpeechToTextState speechToTextState)
	{
		speechToTextStateChangedWeakEventManager.HandleEvent(this, new SpeechToTextStateChangedEventArgs(speechToTextState), nameof(StateChanged));
	}

#if !MACCATALYST && !IOS
	/// <inheritdoc/>
	public async Task<bool> RequestPermissions(CancellationToken cancellationToken = default)
	{
		var status = await Permissions.RequestAsync<Permissions.Microphone>().WaitAsync(cancellationToken).ConfigureAwait(false);
		return status is PermissionStatus.Granted;
	}

	static async Task<bool> IsSpeechPermissionAuthorized(CancellationToken cancellationToken)
	{
		var status = await Permissions.CheckStatusAsync<Permissions.Microphone>().WaitAsync(cancellationToken).ConfigureAwait(false);
		return status is PermissionStatus.Granted;
	}
#endif
}