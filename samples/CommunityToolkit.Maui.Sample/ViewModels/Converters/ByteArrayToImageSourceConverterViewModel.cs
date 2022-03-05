using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public sealed class ByteArrayToImageSourceConverterViewModel : BaseViewModel, IDisposable
{
	readonly WeakEventManager imageDownloadFailedEventManager = new();
	readonly HttpClient client;

	bool isDownloadingImage;
	byte[]? dotNetBotImageByteArray;
	string labelText = "Tap the Download Image Button to download an Image as a byte[]";

	public ByteArrayToImageSourceConverterViewModel(HttpClient httpClient)
	{
		client = httpClient;
		DownloadDotNetBotImageCommand = new AsyncRelayCommand(ExecuteDownloadDotNetBotImageCommand, () => !IsDownloadingImage && dotNetBotImageByteArray is null, false);
	}

	public event EventHandler<string> ImageDownloadFailed
	{
		add => imageDownloadFailedEventManager.AddEventHandler(value);
		remove => imageDownloadFailedEventManager.RemoveEventHandler(value);
	}

	public AsyncRelayCommand DownloadDotNetBotImageCommand { get; }

	public bool IsDownloadingImage
	{
		get => isDownloadingImage;
		set
		{
			SetProperty(ref isDownloadingImage, value);
			MainThread.BeginInvokeOnMainThread(DownloadDotNetBotImageCommand.NotifyCanExecuteChanged);
		}
	}

	public byte[]? DotNetBotImageByteArray
	{
		get => dotNetBotImageByteArray;
		set => SetProperty(ref dotNetBotImageByteArray, value);
	}

	public string LabelText
	{
		get => labelText;
		set => SetProperty(ref labelText, value);
	}

	public void Dispose()
	{
		DotNetBotImageByteArray = null;
	}

	async Task ExecuteDownloadDotNetBotImageCommand()
	{
		IsDownloadingImage = true;

		var maximumDownloadTime = TimeSpan.FromSeconds(5);
		var cancellationTokenSource = new CancellationTokenSource(maximumDownloadTime);

		// Ensure Activity Indicator appears on screen for a minumum of 1.5 seconds when the user taps the Download Button
		var minimumDownloadTime = TimeSpan.FromSeconds(1.5);
		var minimumDownloadTimeTask = Task.Delay(minimumDownloadTime, cancellationTokenSource.Token);

		try
		{
			DotNetBotImageByteArray = await client.GetByteArrayAsync("https://user-images.githubusercontent.com/13558917/137551073-ac8958bf-83e3-4ae3-8623-4db6dce49d02.png", cancellationTokenSource.Token).ConfigureAwait(false);

			await minimumDownloadTimeTask.ConfigureAwait(false);

			LabelText = "The above image was downloaded as a byte[]";
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			OnImageDownloadFailed(e.Message);
		}
		finally
		{
			IsDownloadingImage = false;
		}
	}

	void OnImageDownloadFailed(in string message) => imageDownloadFailedEventManager.HandleEvent(this, message, nameof(ImageDownloadFailed));
}

