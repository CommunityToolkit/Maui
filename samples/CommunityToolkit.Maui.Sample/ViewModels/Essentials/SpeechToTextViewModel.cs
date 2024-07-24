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

	[ObservableProperty]
	string? recognitionText = "Welcome to .NET MAUI Community Toolkit!";

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(ListenCommand))]
	bool canListenExecute = true;

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(StartListenCommand))]
	bool canStartListenExecute = true;

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(StopListenCommand))]
	bool canStopListenExecute = false;

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

	[RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanListenExecute))]
	async Task Listen(CancellationToken cancellationToken)
	{
		CanStartListenExecute = false;

		try
		{
			var isGranted = await speechToText.RequestPermissions(cancellationToken);
			if (!isGranted)
			{
				await Toast.Make("Permission not granted").Show(cancellationToken);
				return;
			}

			const string beginSpeakingPrompt = "Begin speaking...";

			RecognitionText = beginSpeakingPrompt;

			var recognitionResult = await speechToText.ListenAsync(
				CultureInfo.GetCultureInfo(CurrentLocale?.Language ?? defaultLanguage),
				new Progress<string>(partialText =>
				{
					if (RecognitionText is beginSpeakingPrompt)
					{
						RecognitionText = string.Empty;
					}

					RecognitionText += partialText + " ";
				}), cancellationToken);

			if (recognitionResult.IsSuccessful)
			{
				RecognitionText = recognitionResult.Text;
			}
			else
			{
				await Toast.Make(recognitionResult.Exception?.Message ?? "Unable to recognize speech").Show(CancellationToken.None);
			}

			if (RecognitionText is beginSpeakingPrompt)
			{
				RecognitionText = string.Empty;
			}
		}
		finally
		{
			CanStartListenExecute = true;
		}
	}

	[RelayCommand(CanExecute = nameof(CanStartListenExecute))]
	async Task StartListen(CancellationToken cancellationToken)
	{
		CanListenExecute = false;
		CanStartListenExecute = false;
		CanStopListenExecute = true;

		var isGranted = await speechToText.RequestPermissions(cancellationToken);
		if (!isGranted)
		{
			await Toast.Make("Permission not granted").Show(cancellationToken);
			return;
		}

		const string beginSpeakingPrompt = "Begin speaking...";

		RecognitionText = beginSpeakingPrompt;

		await speechToText.StartListenAsync(CultureInfo.GetCultureInfo(CurrentLocale?.Language ?? defaultLanguage), cancellationToken);

		speechToText.RecognitionResultUpdated += HandleRecognitionResultUpdated;

		if (RecognitionText is beginSpeakingPrompt)
		{
			RecognitionText = string.Empty;
		}
	}

	[RelayCommand(CanExecute = nameof(CanStopListenExecute))]
	Task StopListen(CancellationToken cancellationToken)
	{
		CanListenExecute = true;
		CanStartListenExecute = true;
		CanStopListenExecute = false;

		speechToText.RecognitionResultUpdated -= HandleRecognitionResultUpdated;

		return speechToText.StopListenAsync(cancellationToken);
	}

	void HandleRecognitionResultUpdated(object? sender, SpeechToTextRecognitionResultUpdatedEventArgs e)
	{
		RecognitionText += e.RecognitionResult;
	}

	void HandleRecognitionResultCompleted(object? sender, SpeechToTextRecognitionResultCompletedEventArgs e)
	{
		RecognitionText = e.RecognitionResult;
	}

	async void HandleSpeechToTextStateChanged(object? sender, SpeechToTextStateChangedEventArgs e)
	{
		await Toast.Make($"State Changed: {e.State}").Show(CancellationToken.None);
	}

	void HandleLocalesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		OnPropertyChanged(nameof(CurrentLocale));
	}
}