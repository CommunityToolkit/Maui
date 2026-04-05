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
			return;
		}

		const string beginSpeakingPrompt = "Begin speaking...";

		RecognitionText = beginSpeakingPrompt;

		speechToText.RecognitionResultUpdated += HandleRecognitionResultUpdated;

		await speechToText.StartListenAsync(new SpeechToTextOptions
		{
			AutoStopSilenceTimeout =  TimeSpan.FromSeconds(5),
			Culture = CultureInfo.CurrentCulture,
			ShouldReportPartialResults = true
		}, token);

		if (RecognitionText is beginSpeakingPrompt)
		{
			RecognitionText = string.Empty;
		}
	}

	[RelayCommand(CanExecute = nameof(CanStopListenExecute))]
	Task StopListen(CancellationToken token)
	{
		CanStartListenExecute = true;
		CanStopListenExecute = false;
		
		speechToText.RecognitionResultUpdated -= HandleRecognitionResultUpdated;

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