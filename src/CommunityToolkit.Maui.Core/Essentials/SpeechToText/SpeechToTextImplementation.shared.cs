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
	public async Task<SpeechToTextResult> ListenAsync(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken)
	{
		try
		{
			cancellationToken.ThrowIfCancellationRequested();
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
	public async Task StartListenAsync(CultureInfo culture, CancellationToken cancellationToken)
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
	public Task StopListenAsync(CancellationToken cancellationToken) => InternalStopListeningAsync(cancellationToken);

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
	Lazy<SpeechToTextState> currentStateHolder = new(default(SpeechToTextState));

	/// <inheritdoc/>
	public SpeechToTextState CurrentState
	{
		get => currentStateHolder.Value;
		private set
		{
			if (currentStateHolder.Value != value)
			{
				currentStateHolder = new(value);
				OnSpeechToTextStateChanged(value);
			}
		}
	}

	/// <inheritdoc/>
	public async Task<bool> RequestPermissions(CancellationToken cancellationToken)
	{
		var status = await Permissions.RequestAsync<Permissions.Microphone>().ConfigureAwait(false);
		return status is PermissionStatus.Granted;
	}

	static async Task<bool> IsSpeechPermissionAuthorized(CancellationToken cancellationToken)
	{
		var status = await Permissions.CheckStatusAsync<Permissions.Microphone>().WaitAsync(cancellationToken).ConfigureAwait(false);
		return status is PermissionStatus.Granted;
	}
#endif
}

/// <summary>
/// <see cref="EventArgs"/> for <see cref="ISpeechToText.RecognitionResultUpdated"/>
/// </summary>
public class SpeechToTextRecognitionResultUpdatedEventArgs : EventArgs
{
	/// <summary>
	/// Speech recognition result
	/// </summary>
	public string RecognitionResult { get; }

	/// <summary>
	/// Initialize a new instance of <see cref="SpeechToTextRecognitionResultUpdatedEventArgs"/>
	/// </summary>
	public SpeechToTextRecognitionResultUpdatedEventArgs(string recognitionResult)
	{
		RecognitionResult = recognitionResult;
	}
}

/// <summary>
/// <see cref="EventArgs"/> for <see cref="ISpeechToText.RecognitionResultCompleted"/>
/// </summary>
public class SpeechToTextRecognitionResultCompletedEventArgs : EventArgs
{
	/// <summary>
	/// Speech recognition result
	/// </summary>
	public string RecognitionResult { get; }

	/// <summary>
	/// Initialize a new instance of <see cref="SpeechToTextRecognitionResultCompletedEventArgs"/>
	/// </summary>
	public SpeechToTextRecognitionResultCompletedEventArgs(string recognitionResult)
	{
		RecognitionResult = recognitionResult;
	}
}

/// <summary>
/// <see cref="EventArgs"/> for <see cref="ISpeechToText.StateChanged"/>
/// </summary>
public class SpeechToTextStateChangedEventArgs : EventArgs
{
	/// <summary>
	/// Speech To Text State
	/// </summary>
	public SpeechToTextState State { get; }

	/// <summary>
	/// Initialize a new instance of <see cref="SpeechToTextStateChangedEventArgs"/>
	/// </summary>
	public SpeechToTextStateChangedEventArgs(SpeechToTextState state)
	{
		State = state;
	}
}