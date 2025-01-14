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

	public SpeechToTextViewModel(ITextToSpeech textToSpeech, [FromKeyedServices("Online")] ISpeechToText speechToText)
	{
		this.textToSpeech = textToSpeech;
		this.speechToText = speechToText;

		Locales.CollectionChanged += HandleLocalesCollectionChanged;
		this.speechToText.StateChanged += HandleSpeechToTextStateChanged;
		this.speechToText.RecognitionResultCompleted += HandleRecognitionResultCompleted;
	}

	public ObservableCollection<Locale> Locales { get; } = [];

	public SpeechToTextState? State => speechToText.CurrentState;

	[ObservableProperty]
	public partial Locale? CurrentLocale { get; set; }

	[ObservableProperty]
	public partial string? RecognitionText { get; set; } = "Welcome to .NET MAUI Community Toolkit!";

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(StartListenCommand))]
	public partial bool CanStartListenExecute { get; set; } = true;

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(StopListenCommand))]
	public partial bool CanStopListenExecute { get; set; } = false;

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

		await speechToText.StartListenAsync(new SpeechToTextOptions()
		{
			Culture = CultureInfo.GetCultureInfo(CurrentLocale?.Language ?? defaultLanguage),
			ShouldReportPartialResults = true
		}, CancellationToken.None);

		if (RecognitionText is beginSpeakingPrompt)
		{
			RecognitionText = string.Empty;
		}
	}

	[RelayCommand(CanExecute = nameof(CanStopListenExecute))]
	Task StopListen()
	{
		CanStartListenExecute = true;
		CanStopListenExecute = false;

		speechToText.RecognitionResultUpdated -= HandleRecognitionResultUpdated;

		return speechToText.StopListenAsync(CancellationToken.None);
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