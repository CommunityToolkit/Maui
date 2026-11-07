using System.Globalization;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Essentials;

public partial class OfflineSpeechToTextViewModel : BaseViewModel
{
	const string beginSpeakingPrompt = "Begin speaking...";

	readonly ISpeechToText speechToText;

	// A page can be left while a permission prompt is open: on Mac Catalyst the prompt is another process's window, so Back and the flyout stay usable
	bool isPageShown;

	public OfflineSpeechToTextViewModel([FromKeyedServices("Offline")] ISpeechToText offlineTextToSpeech)
	{
		speechToText = offlineTextToSpeech;
	}

	// After StartListenAsync, CurrentState stays Stopped until speech begins on Android, and until the first result on iOS and Mac Catalyst:
	// until then the State label shows that the session waits for speech
	public string State => IsWaitingForSpeech ? "Waiting for speech" : speechToText.CurrentState.ToString();

	[ObservableProperty, NotifyPropertyChangedFor(nameof(State))]
	public partial bool IsWaitingForSpeech { get; set; }

	[ObservableProperty]
	public partial string? RecognitionText { get; set; } = "Welcome to .NET MAUI Community Toolkit!";

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(StartListenCommand))]
	public partial bool CanStartListenExecute { get; set; } = true;

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(StopListenCommand))]
	public partial bool CanStopListenExecute { get; set; } = false;

	public void SubscribeToSpeechToText()
	{
		isPageShown = true;

		speechToText.StateChanged += HandleSpeechToTextStateChanged;
		speechToText.RecognitionResultUpdated += HandleRecognitionResultUpdated;
		speechToText.RecognitionResultCompleted += HandleRecognitionResultCompleted;

		OnPropertyChanged(nameof(State));
	}

	// The recognizer is a singleton that outlives the page: a session that is left running keeps the microphone open, and the next page would show its state.
	// The events are released first: Shell can show this page again, which subscribes it anew, before the stop completes
	public async Task StopListeningAndUnsubscribeFromSpeechToText()
	{
		isPageShown = false;

		speechToText.StateChanged -= HandleSpeechToTextStateChanged;
		speechToText.RecognitionResultUpdated -= HandleRecognitionResultUpdated;
		speechToText.RecognitionResultCompleted -= HandleRecognitionResultCompleted;

		if (CanStopListenExecute)
		{
			await StopListen(CancellationToken.None);
		}
	}

	// On iOS and Mac Catalyst, ISpeechToText.RequestPermissions requests the speech recognition permission; on Android and Windows it requests the microphone
	// permission again. It is requested only once the microphone permission is granted, so Android does not show its microphone dialog a second time after a denial
	static async Task<bool> ArePermissionsGranted(ISpeechToText speechToText)
	{
		var microphonePermissionStatus = await Permissions.RequestAsync<Permissions.Microphone>();

		return microphonePermissionStatus is PermissionStatus.Granted
			   && await speechToText.RequestPermissions(CancellationToken.None);
	}

	[RelayCommand(CanExecute = nameof(CanStartListenExecute))]
	async Task StartListen(CancellationToken token)
	{
		CanStartListenExecute = false;

		var isGranted = await ArePermissionsGranted(speechToText);

		// The teardown of a page that was left during the prompts found no session to stop and released the page's handlers, so the singleton
		// recognizer must not start: it would listen with no page to show its results or to stop it
		if (!isPageShown)
		{
			CanStartListenExecute = true;
			return;
		}

		if (!isGranted)
		{
			await Toast.Make("Permission not granted").Show(token);
			CanStartListenExecute = true;
			return;
		}

		// StartListenAsync returns once the session has started: the prompt stays until the first result replaces it, and the session ends with the
		// RecognitionResultCompleted event (also when it stops by itself, after AutoStopSilenceTimeout or an error) or with StopListenAsync
		CanStopListenExecute = true;
		RecognitionText = beginSpeakingPrompt;
		IsWaitingForSpeech = true;

		try
		{
			await speechToText.StartListenAsync(new SpeechToTextOptions
			{
				AutoStopSilenceTimeout = TimeSpan.FromSeconds(5),
				Culture = CultureInfo.CurrentCulture,
				ShouldReportPartialResults = true
			}, token);
		}
		catch (Exception e)
		{
			// An exception that escapes the command ends the app: for example a FeatureNotSupportedException on Android 13 and earlier, or on a device
			// without on-device speech recognition, a NotSupportedException on Android when the on-device recognizer does not support the culture, or an
			// exception of the audio engine on the iOS simulator when it gets no audio input.
			IsWaitingForSpeech = false;
			CanStartListenExecute = true;
			CanStopListenExecute = false;

			// The message of the audio engine's ObjCException continues with the native stack trace: the page shows its first line
			var message = e.Message.Split('\n', 2)[0].TrimEnd();
			RecognitionText = message;
			await Toast.Make($"{e.GetType().Name}: {message}").Show(token);
		}
	}

	[RelayCommand(CanExecute = nameof(CanStopListenExecute))]
	async Task StopListen(CancellationToken token)
	{
		IsWaitingForSpeech = false;
		CanStopListenExecute = false;

		await speechToText.StopListenAsync(token);

		// StartListenAsync is enabled again once the stop has completed, so an outcome that the stop reports cannot end the next session
		CanStartListenExecute = true;

		// A session that was stopped before its first result does not keep showing the prompt: Windows reports no RecognitionResultCompleted after StopListenAsync
		if (RecognitionText is beginSpeakingPrompt)
		{
			RecognitionText = string.Empty;
		}

		// Windows unsubscribes from StateChanged before it releases the recognizer, so no event refreshes the State label
		OnPropertyChanged(nameof(State));
	}

	// The speech-to-text events can come on a background thread (Windows): the view model handles them on the main thread, where the buttons are updated
	void HandleRecognitionResultUpdated(object? sender, SpeechToTextRecognitionResultUpdatedEventArgs e) => MainThread.BeginInvokeOnMainThread(() =>
	{
		IsWaitingForSpeech = false;
		RecognitionText = RecognitionText is beginSpeakingPrompt ? e.RecognitionResult : RecognitionText + e.RecognitionResult;
	});

	void HandleRecognitionResultCompleted(object? sender, SpeechToTextRecognitionResultCompletedEventArgs e) => MainThread.BeginInvokeOnMainThread(() =>
	{
		// The session ended by itself (after AutoStopSilenceTimeout or an error, for example 'NoMatch' on Android when nothing was said): a new session can start.
		// The outcome of a session that StopListenAsync stopped can also come later, after the buttons were reset or while a new start requests the permissions
		if (CanStopListenExecute)
		{
			IsWaitingForSpeech = false;
			CanStartListenExecute = true;
			CanStopListenExecute = false;
		}

		RecognitionText = e.RecognitionResult.IsSuccessful ? e.RecognitionResult.Text : e.RecognitionResult.Exception.Message;
	});

	void HandleSpeechToTextStateChanged(object? sender, SpeechToTextStateChangedEventArgs e) => MainThread.BeginInvokeOnMainThread(() =>
	{
		// Listening or Silence: the recognizer reports that it listens, so the State label shows CurrentState again
		if (e.State is not SpeechToTextState.Stopped)
		{
			IsWaitingForSpeech = false;
		}

		OnPropertyChanged(nameof(State));
	});
}