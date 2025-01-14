using System.Globalization;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Essentials;

public partial class OfflineSpeechToTextViewModel : BaseViewModel
{
	readonly ISpeechToText speechToText;

	public OfflineSpeechToTextViewModel()
	{
		// For demo purposes. You can resolve dependency from the DI container,
		speechToText = OfflineSpeechToText.Default;

		speechToText.StateChanged += HandleSpeechToTextStateChanged;
		speechToText.RecognitionResultCompleted += HandleRecognitionResultCompleted;
	}

	public SpeechToTextState? State => speechToText.CurrentState;

	[ObservableProperty]
	public partial string? RecognitionText { get; set; } = "Welcome to .NET MAUI Community Toolkit!";

	[RelayCommand]
	async Task StartListen()
	{
		var isGranted = await speechToText.RequestPermissions(CancellationToken.None);
		if (!isGranted)
		{
			await Toast.Make("Permission not granted").Show(CancellationToken.None);
			return;
		}

		const string beginSpeakingPrompt = "Begin speaking...";

		RecognitionText = beginSpeakingPrompt;

		speechToText.RecognitionResultUpdated += HandleRecognitionResultUpdated;

		await speechToText.StartListenAsync(new SpeechToTextOptions
		{
			Culture = CultureInfo.CurrentCulture,
			ShouldReportPartialResults = true
		}, CancellationToken.None);

		if (RecognitionText is beginSpeakingPrompt)
		{
			RecognitionText = string.Empty;
		}
	}

	[RelayCommand]
	Task StopListen()
	{
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
}