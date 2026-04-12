using System.Globalization;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Essentials;

public partial class OfflineSpeechToTextViewModel : BaseViewModel
{
	readonly ISpeechToText speechToText;

	public OfflineSpeechToTextViewModel([FromKeyedServices("Offline")] ISpeechToText offlineTextToSpeech)
	{
		speechToText = offlineTextToSpeech;
		speechToText.StateChanged += HandleSpeechToTextStateChanged;
		speechToText.RecognitionResultUpdated += HandleRecognitionResultUpdated;
		speechToText.RecognitionResultCompleted += HandleRecognitionResultCompleted;
	}

	public SpeechToTextState? State => speechToText.CurrentState;

	[ObservableProperty]
	public partial string? RecognitionText { get; set; } = "Welcome to .NET MAUI Community Toolkit!";

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(StartListenCommand))]
	public partial bool CanStartListenExecute { get; set; } = true;

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(StopListenCommand))]
	public partial bool CanStopListenExecute { get; set; } = false;

	static async Task<bool> ArePermissionsGranted(ISpeechToText speechToText)
	{
		var microphonePermissionStatus = await Permissions.RequestAsync<Permissions.Microphone>();
		var isSpeechToTextRequestPermissionsGranted = await speechToText.RequestPermissions(CancellationToken.None);

		return microphonePermissionStatus is PermissionStatus.Granted
			   && isSpeechToTextRequestPermissionsGranted;
	}

	[RelayCommand(CanExecute = nameof(CanStartListenExecute))]
	async Task StartListen(CancellationToken token)
	{
		CanStartListenExecute = false;
		CanStopListenExecute = true;

		var isGranted = await ArePermissionsGranted(speechToText);
		if (!isGranted)
		{
			await Toast.Make("Permission not granted").Show(token);
			CanStartListenExecute = true;
			CanStopListenExecute = false;
			return;
		}

		const string beginSpeakingPrompt = "Begin speaking...";

		RecognitionText = beginSpeakingPrompt;

		try
		{
			await speechToText.StartListenAsync(new SpeechToTextOptions
			{
				AutoStopSilenceTimeout = TimeSpan.FromSeconds(5),
				Culture = CultureInfo.CurrentCulture,
				ShouldReportPartialResults = true
			}, token);

			if (RecognitionText is beginSpeakingPrompt)
			{
				RecognitionText = string.Empty;
			}
		}
		catch
		{
			CanStartListenExecute = true;
			CanStopListenExecute = false;

			throw;
		}
	}

	[RelayCommand(CanExecute = nameof(CanStopListenExecute))]
	Task StopListen(CancellationToken token)
	{
		CanStartListenExecute = true;
		CanStopListenExecute = false;

		return speechToText.StopListenAsync(token);
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