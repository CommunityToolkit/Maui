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
	const string defaultLanguage_tizen= "en_US";

	readonly ITextToSpeech textToSpeech;
	readonly ISpeechToText speechToText;

	[ObservableProperty]
	Locale? locale;

	[ObservableProperty]
	string? recognitionText = "Welcome to .NET MAUI Community Toolkit!";

	public SpeechToTextViewModel(ITextToSpeech textToSpeech, ISpeechToText speechToText)
	{
		this.textToSpeech = textToSpeech;
		this.speechToText = speechToText;

		Locales.CollectionChanged += HandleLocalesCollectionChanged;
	}

	public ObservableCollection<Locale> Locales { get; } = new();

	[RelayCommand]
	async Task SetLocales()
	{
		Locales.Clear();

		var locales = await textToSpeech.GetLocalesAsync();

		foreach (var locale in locales.OrderBy(x => x.Language).ThenBy(x => x.Name))
		{
			Locales.Add(locale);
		}

		Locale = Locales.FirstOrDefault(x => x.Language is defaultLanguage or defaultLanguage_android or defaultLanguage_tizen) ?? Locales.FirstOrDefault();
	}

	[RelayCommand]
	async Task Play(CancellationToken cancellationToken)
	{
		await textToSpeech.SpeakAsync(RecognitionText ?? "Welcome to .NET MAUI Community Toolkit!", new()
		{
			Locale = Locale,
			Pitch = 2,
			Volume = 1
		}, cancellationToken);
	}

	[RelayCommand(IncludeCancelCommand = true)]
	async Task Listen(CancellationToken cancellationToken)
	{
		var isGranted = await speechToText.RequestPermissions(cancellationToken);
		if (!isGranted)
		{
			await Toast.Make("Permission not granted").Show(CancellationToken.None);
			return;
		}
		
		const string beginSpeakingPrompt = "Begin speaking...";

		RecognitionText = beginSpeakingPrompt;

		var recognitionResult = await speechToText.ListenAsync(
											CultureInfo.GetCultureInfo(Locale?.Language ?? defaultLanguage),
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

	void HandleLocalesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		OnPropertyChanged(nameof(Locale));
	}
}