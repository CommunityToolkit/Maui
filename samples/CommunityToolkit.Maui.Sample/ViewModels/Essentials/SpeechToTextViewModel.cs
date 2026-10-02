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

	public SpeechToTextState? State => speechToText.CurrentState;

	[ObservableProperty]
	public partial Locale? CurrentLocale { get; set; }

	[ObservableProperty]
	public partial string? RecognitionText { get; set; } = "Welcome to .NET MAUI Community Toolkit!";

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(StartListenCommand))]
	public partial bool CanStartListenExecute { get; set; } = true;

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(StopListenCommand))]
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

	static async Task<bool> ArePermissionsGranted(ISpeechToText speechToText)
	{
		var microphonePermissionStatus = await Permissions.RequestAsync<Permissions.Microphone>();
		var isSpeechToTextPermissionsGranted = await speechToText.RequestPermissions(CancellationToken.None);

		return microphonePermissionStatus is PermissionStatus.Granted
			   && isSpeechToTextPermissionsGranted;
	}

	[RelayCommand]
	async Task SetLocales(CancellationToken token)
	{
		Locales.Clear();

		IReadOnlyList<Locale> locales = [.. await textToSpeech.GetLocalesAsync().WaitAsync(token)];

		foreach (var locale in locales.OrderBy(x => x.Language).ThenBy(x => x.Name))
		{
			Locales.Add(locale);
		}

		var currentLocale = locales.FirstOrDefault(l => l.Language.Equals(CultureInfo.CurrentUICulture.Name, StringComparison.OrdinalIgnoreCase))
							?? locales.FirstOrDefault(l => l.Language.Equals(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase));

		CurrentLocale = currentLocale ?? locales[0];
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
		CanStopListenExecute = true;

		var isGranted = await ArePermissionsGranted(speechToText);
		if (!isGranted)
		{
			await Toast.Make("Permission not granted").Show(cancellationToken);
			CanStartListenExecute = true;
			CanStopListenExecute = false;
			return;
		}

		if (Connectivity.NetworkAccess is not NetworkAccess.Internet)
		{
			await Toast.Make("Internet connection is required").Show(cancellationToken);
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
				Culture = CultureInfo.GetCultureInfo(CurrentLocale?.Language ?? defaultLanguage),
				AutoStopSilenceTimeout = TimeSpan.FromSeconds(5),
				ShouldReportPartialResults = true
			}, cancellationToken);

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
	Task StopListen(CancellationToken cancellationToken)
	{
		CanStartListenExecute = true;
		CanStopListenExecute = false;

		return speechToText.StopListenAsync(cancellationToken);
	}

	void HandleRecognitionResultUpdated(object? sender, SpeechToTextRecognitionResultUpdatedEventArgs e)
	{
		RecognitionText += $" {e.RecognitionResult}";
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