using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Dispatching;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public sealed partial class ByteArrayToImageSourceConverterViewModel : BaseViewModel, IDisposable
{
	readonly WeakEventManager imageDownloadFailedEventManager = new();
	readonly HttpClient client;

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(DownloadDotNetBotImageCommand))]
	bool isDownloadingImage;

	[ObservableProperty]
	byte[]? dotNetBotImageByteArray;

	[ObservableProperty]
	string labelText = "Tap the Download Image Button to download an Image as a byte[]";

	public ByteArrayToImageSourceConverterViewModel(HttpClient client)
	{
		this.client = client;
	}

	public event EventHandler<string> ImageDownloadFailed
	{
		add => imageDownloadFailedEventManager.AddEventHandler(value);
		remove => imageDownloadFailedEventManager.RemoveEventHandler(value);
	}

	public void Dispose()
	{
		DotNetBotImageByteArray = null;
	}

	bool CanDownloadDotNetBotImageComandExecute => !IsDownloadingImage && dotNetBotImageByteArray is null;

	[RelayCommand(CanExecute = nameof(CanDownloadDotNetBotImageComandExecute))]
	async Task DownloadDotNetBotImage()
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