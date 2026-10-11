using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Essentials;

public partial class SpeechToTextViewModel : BaseViewModel, IAsyncDisposable
{
	const string defaultLanguage = "en-US";
	const string beginSpeakingPrompt = "Begin speaking...";

	readonly ITextToSpeech textToSpeech;
	readonly ISpeechToText speechToText;

	public SpeechToTextViewModel(ITextToSpeech textToSpeech, [FromKeyedServices("Online")] ISpeechToText speechToText)
	{
		this.textToSpeech = textToSpeech;
		this.speechToText = speechToText;

		Locales.CollectionChanged += HandleLocalesCollectionChanged;
		this.speechToText.StateChanged += HandleSpeechToTextStateChanged;
		this.speechToText.RecognitionResultUpdated += HandleRecognitionResultUpdated;
		this.speechToText.RecognitionResultCompleted += HandleRecognitionResultCompleted;
	}

	public ObservableCollection<Locale> Locales { get; } = [];

	// After StartListenAsync, CurrentState stays Stopped until speech begins on Android, and until the first result on iOS and Mac Catalyst:
	// until then the State label shows that the session waits for speech
	public string State => IsWaitingForSpeech ? "Waiting for speech" : speechToText.CurrentState.ToString();

	[ObservableProperty, NotifyPropertyChangedFor(nameof(State))]
	public partial bool IsWaitingForSpeech { get; set; }

	[ObservableProperty]
	public partial Locale? CurrentLocale { get; set; }

	[ObservableProperty]
	public partial string? RecognitionText { get; set; } = "Welcome to .NET MAUI Community Toolkit!";

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(StartListenCommand))]
	public partial bool CanStartListenExecute { get; set; } = true;

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(StopListenCommand)), NotifyCanExecuteChangedFor(nameof(StopListenWithCanceledTokenCommand))]
	public partial bool CanStopListenExecute { get; set; } = false;

	public async ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);

		Locales.CollectionChanged -= HandleLocalesCollectionChanged;
		this.speechToText.StateChanged -= HandleSpeechToTextStateChanged;
		this.speechToText.RecognitionResultUpdated -= HandleRecognitionResultUpdated;
		this.speechToText.RecognitionResultCompleted -= HandleRecognitionResultCompleted;
		await speechToText.DisposeAsync();
	}

	// On iOS and Mac Catalyst, ISpeechToText.RequestPermissions requests the speech recognition permission; on Android and Windows it requests the microphone
	// permission again. It is requested only once the microphone permission is granted, so Android does not show its microphone dialog a second time after a denial
	static async Task<bool> ArePermissionsGranted(ISpeechToText speechToText)
	{
		var microphonePermissionStatus = await Permissions.RequestAsync<Permissions.Microphone>();

		return microphonePermissionStatus is PermissionStatus.Granted
			   && await speechToText.RequestPermissions(CancellationToken.None);
	}

	// The culture of a text-to-speech locale: Android names its language and region in Language and Country ("en", "US"), iOS, Mac Catalyst and Windows in Language ("en-US")
	static string GetCultureName(Locale locale) => string.IsNullOrEmpty(locale.Country) ? locale.Language : $"{locale.Language}-{locale.Country}";

	[RelayCommand]
	async Task SetLocales(CancellationToken token)
	{
		Locales.Clear();

		// Android returns a locale for each voice, and the voices of a language and region have the same Language, Country and Name: the Picker lists them once
		IReadOnlyList<Locale> locales = [.. (await textToSpeech.GetLocalesAsync().WaitAsync(token)).DistinctBy(static x => (x.Language, x.Country, x.Name))];

		foreach (var locale in locales.OrderBy(static x => x.Language).ThenBy(static x => x.Name))
		{
			Locales.Add(locale);
		}

		// The locale of the device's language and region, else one of its language. It is looked up in the order of GetLocalesAsync, not in the Picker's
		// alphabetical order, which starts with novelty voices on iOS ('en-US Albert' instead of 'en-US Samantha'): Play speaks with the picked voice
		var currentCulture = CultureInfo.CurrentUICulture;
		CurrentLocale = locales.FirstOrDefault(l => GetCultureName(l).Equals(currentCulture.Name, StringComparison.OrdinalIgnoreCase))
						?? locales.FirstOrDefault(l => GetCultureName(l).Split('-')[0].Equals(currentCulture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase))
						?? locales.FirstOrDefault();
	}

	[RelayCommand]
	async Task Play(CancellationToken cancellationToken)
	{
		var timeoutCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

		try
		{
			await textToSpeech.SpeakAsync(RecognitionText ?? "Welcome to .NET MAUI Community Toolkit!", new()
			{
				Locale = CurrentLocale,
				Pitch = 1,
				Volume = 1
			}, cancellationToken).WaitAsync(timeoutCancellationTokenSource.Token);
		}
		catch (TaskCanceledException)
		{
			await Toast.Make("Playback automatically stopped after 5 seconds").Show(cancellationToken);
#if IOS
			await Toast.Make("If you did not hear playback, test again on a physical iOS device").Show(cancellationToken);
#endif
		}
	}

	[RelayCommand(CanExecute = nameof(CanStartListenExecute))]
	async Task StartListen(CancellationToken cancellationToken)
	{
		CanStartListenExecute = false;

		var isGranted = await ArePermissionsGranted(speechToText);
		if (!isGranted)
		{
			await Toast.Make("Permission not granted").Show(cancellationToken);
			CanStartListenExecute = true;
			return;
		}

		if (Connectivity.NetworkAccess is not NetworkAccess.Internet)
		{
			await Toast.Make("Internet connection is required").Show(cancellationToken);
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
				Culture = CultureInfo.GetCultureInfo(CurrentLocale is null ? defaultLanguage : GetCultureName(CurrentLocale)),
				AutoStopSilenceTimeout = TimeSpan.FromSeconds(5),
				ShouldReportPartialResults = true
			}, cancellationToken);
		}
		catch (Exception e)
		{
			// An exception that escapes the command ends the app, and the permission requests above do not cover every failure: for example a
			// PermissionException on Windows when "Online speech recognition" is switched off in the privacy settings, a FeatureNotSupportedException on
			// Android when the device has no speech recognition service, or an exception of the audio engine on the iOS simulator when it gets no audio input.
			IsWaitingForSpeech = false;
			CanStartListenExecute = true;
			CanStopListenExecute = false;

			// The message of the audio engine's ObjCException continues with the native stack trace: the page shows its first line
			var message = e.Message.Split('\n', 2)[0].TrimEnd();
			RecognitionText = message;
			await Toast.Make($"{e.GetType().Name}: {message}").Show(cancellationToken);
		}
	}

	[RelayCommand(CanExecute = nameof(CanStopListenExecute))]
	async Task StopListen(CancellationToken cancellationToken)
	{
		IsWaitingForSpeech = false;
		CanStopListenExecute = false;

		await speechToText.StopListenAsync(cancellationToken);

		// StartListenAsync is enabled again once the stop has completed, so an outcome that the stop reports cannot end the next session
		CanStartListenExecute = true;
		ClearPromptAfterStop();

		// Windows unsubscribes from StateChanged before it releases the recognizer, so no event refreshes the State label
		OnPropertyChanged(nameof(State));
	}

	// Windows ignores the canceled token, so the recognizer is still released. Android, iOS and Mac Catalyst throw an OperationCanceledException
	// before they stop anything, so the recognizer keeps listening until StopListenAsync is called again.
	[RelayCommand(CanExecute = nameof(CanStopListenExecute))]
	async Task StopListenWithCanceledToken(CancellationToken cancellationToken)
	{
		using var canceledTokenSource = new CancellationTokenSource();
		await canceledTokenSource.CancelAsync();

		string outcome;
		try
		{
			await speechToText.StopListenAsync(canceledTokenSource.Token);

			IsWaitingForSpeech = false;
			CanStartListenExecute = true;
			CanStopListenExecute = false;

			ClearPromptAfterStop();

			outcome = "returned without an exception";
		}
		catch (OperationCanceledException)
		{
			outcome = $"threw {nameof(OperationCanceledException)}";
		}

		// Windows unsubscribes from StateChanged before it releases the recognizer, so no event refreshes the State label.
		OnPropertyChanged(nameof(State));

		await Toast.Make($"StopListenAsync with a canceled token {outcome}. State: {State}").Show(cancellationToken);
	}

	// A session that was stopped before its first result does not keep showing the prompt, also when no RecognitionResultCompleted event follows the stop
	void ClearPromptAfterStop()
	{
		if (RecognitionText is beginSpeakingPrompt)
		{
			RecognitionText = string.Empty;
		}
	}

	// The speech-to-text events can come on a background thread (Windows): the view model handles them on the main thread, where the buttons are updated
	void HandleRecognitionResultUpdated(object? sender, SpeechToTextRecognitionResultUpdatedEventArgs e) => MainThread.BeginInvokeOnMainThread(() =>
	{
		// The recognizer heard speech; its first result replaces the prompt
		IsWaitingForSpeech = false;
		RecognitionText = RecognitionText is beginSpeakingPrompt ? e.RecognitionResult : $"{RecognitionText} {e.RecognitionResult}";
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

	void HandleLocalesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		OnPropertyChanged(nameof(CurrentLocale));
	}
}