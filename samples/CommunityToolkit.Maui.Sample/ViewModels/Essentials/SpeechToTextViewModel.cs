using System.Globalization;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Essentials;

public partial class SpeechToTextViewModel : BaseViewModel
{
	readonly ITextToSpeech textToSpeech;
	readonly ISpeechToText speechToText;
	[ObservableProperty]
	List<Locale>? locales;

	[ObservableProperty]
	Locale? locale;

	[ObservableProperty]
	string text;

	[ObservableProperty]
	string? recognitionText;

	public SpeechToTextViewModel(ITextToSpeech textToSpeech, ISpeechToText speechToText)
	{
		this.textToSpeech = textToSpeech;
		this.speechToText = speechToText;
		Locales = new();
		text = @"Welcome to .NET MAUI Community Toolkit!";
		SetLocalesCommand.Execute(null);
	}

	[RelayCommand]
	async Task SetLocales()
	{
		Locales = (await textToSpeech.GetLocalesAsync()).ToList();
		Locale = Locales.FirstOrDefault();
	}

	[RelayCommand]
	async Task Play(CancellationToken cancellationToken)
	{
		await textToSpeech.SpeakAsync(Text, new SpeechOptions()
		{
			Locale = Locale,
			Pitch = 2,
			Volume = 1
		}, cancellationToken);
	}

	[RelayCommand(IncludeCancelCommand = true)]
	async Task Listen(CancellationToken cancellationToken)
	{
		try
		{
			RecognitionText = await speechToText.ListenAsync(CultureInfo.GetCultureInfo(Locale?.Language ?? "en-us"), new Progress<string>(partialText =>
			{
				RecognitionText += partialText + " ";
			}), cancellationToken);
		}
		catch (Exception ex)
		{
			await Toast.Make(ex.Message).Show(CancellationToken.None);
		}
	}
}