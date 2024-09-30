using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Essentials;

public partial class SpeechToTextViewModel : BaseViewModel
{
	const string defaultLanguage = "en-US";
	const string defaultLanguage_android = "en";
	const string defaultLanguage_tizen = "en_US";

	readonly ITextToSpeech textToSpeech;
	readonly ISpeechToText speechToText;

	[ObservableProperty]
	Locale? currentLocale;

	public SpeechToTextState? State => speechToText.CurrentState;

	[ObservableProperty]
	string? recognitionText = "Welcome to .NET MAUI Community Toolkit!";
	
	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(StartListenCommand))]
	bool canStartListenExecute = true;

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(StopListenCommand))]
	bool canStopListenExecute = false;

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(StartOfflineListenCommand))]
	bool canStartOfflineListenExecute = true;

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(StopOfflineListenCommand))]
	bool canStopOfflineListenExecute = false;

	public SpeechToTextViewModel(ITextToSpeech textToSpeech, ISpeechToText speechToText)
	{
		this.textToSpeech = textToSpeech;
		this.speechToText = speechToText;

		Locales.CollectionChanged += HandleLocalesCollectionChanged;
		this.speechToText.StateChanged += HandleSpeechToTextStateChanged;
		this.speechToText.RecognitionResultCompleted += HandleRecognitionResultCompleted;
	}

	public ObservableCollection<Locale> Locales { get; } = [];

	[RelayCommand]
	async Task SetLocales(CancellationToken token)
	{
		Locales.Clear();

		var locales = await textToSpeech.GetLocalesAsync().WaitAsync(token);

		foreach (var locale in locales.OrderBy(x => x.Language).ThenBy(x => x.Name))
		{
			Locales.Add(locale);
		}

		CurrentLocale = Locales.FirstOrDefault(x => x.Language is defaultLanguage or defaultLanguage_android or defaultLanguage_tizen) ?? Locales.FirstOrDefault();
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
	async Task StartListen()
	{
		CanStartListenExecute = false;
		CanStartOfflineListenExecute = false;
		CanStopOfflineListenExecute = false;
		CanStopListenExecute = true;

		var isGranted = await speechToText.RequestPermissions(CancellationToken.None);
		if (!isGranted)
		{
			await Toast.Make("Permission not granted").Show(CancellationToken.None);
			return;
		}

		if (Connectivity.NetworkAccess != NetworkAccess.Internet)
		{
			await Toast.Make("Internet connection is required").Show(CancellationToken.None);
			return;
		}

		const string beginSpeakingPrompt = "Begin speaking...";

		RecognitionText = beginSpeakingPrompt;

		speechToText.RecognitionResultUpdated += HandleRecognitionResultUpdated;

		await speechToText.StartListenAsync(CultureInfo.GetCultureInfo(CurrentLocale?.Language ?? defaultLanguage), CancellationToken.None);
		
		if (RecognitionText is beginSpeakingPrompt)
		{
			RecognitionText = string.Empty;
		}
	}

	[RelayCommand(CanExecute = nameof(CanStopListenExecute))]
	Task StopListen()
	{
		CanStartListenExecute = true;
		CanStartOfflineListenExecute = true;
		CanStopOfflineListenExecute = false;
		CanStopListenExecute = false;

		speechToText.RecognitionResultUpdated -= HandleRecognitionResultUpdated;

		return speechToText.StopListenAsync(CancellationToken.None);
	}
	
	[RelayCommand(CanExecute = nameof(CanStartOfflineListenExecute))]
	async Task StartOfflineListen()
	{
		CanStartListenExecute = false;
		CanStopListenExecute = false;
		CanStartOfflineListenExecute = false;
		CanStopOfflineListenExecute = true;

		var isGranted = await speechToText.RequestPermissions(CancellationToken.None);
		if (!isGranted)
		{
			await Toast.Make("Permission not granted").Show(CancellationToken.None);
			return;
		}

		const string beginSpeakingPrompt = "Begin speaking...";

		RecognitionText = beginSpeakingPrompt;

		speechToText.RecognitionResultUpdated += HandleRecognitionResultUpdated;

		await speechToText.StartOfflineListenAsync(CultureInfo.GetCultureInfo(CurrentLocale?.Language ?? defaultLanguage), CancellationToken.None);
		
		if (RecognitionText is beginSpeakingPrompt)
		{
			RecognitionText = string.Empty;
		}
	}

	[RelayCommand(CanExecute = nameof(CanStopOfflineListenExecute))]
	Task StopOfflineListen()
	{
		CanStartOfflineListenExecute = true;
		CanStartListenExecute = true;
		CanStopListenExecute = false;
		CanStopOfflineListenExecute = false;

		speechToText.RecognitionResultUpdated -= HandleRecognitionResultUpdated;

		return speechToText.StopOfflineListenAsync(CancellationToken.None);
	}

	void HandleRecognitionResultUpdated(object? sender, SpeechToTextRecognitionResultUpdatedEventArgs e)
	{
		RecognitionText += e.RecognitionResult;
	}

	void HandleRecognitionResultCompleted(object? sender, SpeechToTextRecognitionResultCompletedEventArgs e)
	{
		RecognitionText = e.RecognitionResult.IsSuccessful ? e.RecognitionResult.Text : e.RecognitionResult.Exception.Message;
	}

	void HandleSpeechToTextStateChanged(object? sender, SpeechToTextStateChangedEventArgs e)
	{
		OnPropertyChanged(nameof(State));
	}

	void HandleLocalesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		OnPropertyChanged(nameof(CurrentLocale));
	}
}